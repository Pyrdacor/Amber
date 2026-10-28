using Amber.Common;
using Amber.Renderer.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;

namespace Amberstar.Game.Screens;

/// <summary>
/// The outro (EXTRO.68K in the original, a separate program which was disassembled). The chancellor speaks to the
/// party in the throne room. Then the people watch the dragon and an eagle from the balcony until the space key
/// is pressed. Like in the original everything is timed by vertical blanks (50 per second): a step every 6 vertical
/// blanks, the palettes fade in steps of 5 (in) and 3 (out) vertical blanks (3 and 3 on the Amiga).
/// </summary>
internal sealed class OutroScreen : Screen
{
	enum Phase
	{
		ThroneRoomFadeIn,
		ThroneRoom,
		ThroneRoomFadeOut,
		BalconyFadeIn,
		Balcony,
		BalconyFadeOut,
		Finished
	}

	const int VerticalBlanksPerSecond = 50;
	const int VerticalBlanksPerStep = 6;
	const byte PictureDisplayLayer = 220;
	const byte FigureDisplayLayer = 222;
	const byte SpriteDisplayLayer = 224;
	const byte TextBoxDisplayLayer = 230;
	const byte TextDisplayLayer = 232;
	// If the music of the outro can't be loaded, a song of the game is played.
	const int FallbackSong = 6;
	// throne room
	static readonly Position LeftTorchPosition = new(115, 59);
	static readonly Position RightTorchPosition = new(188, 59);
	static readonly Position ChancellorPosition = new(146, 66);
	const int PartyFigureOffsetX = -8;
	const int PartyFigureOffsetY = 104;
	const int ChancellorStartWait = 90;
	const int ChancellorLastFrame = 6;
	const int ChancellorEndWait = -80;
	const int TextWait = -100;
	// balcony
	static readonly Position PulsingMoonPosition = new(226, 22);
	static readonly int[] PulsingMoonFrames = [0, 1, 2, 3, 2, 1];
	const int DragonStartX = 350;
	const int DragonEndX = -128;
	const int DragonOffsetY = -9;
	const int EagleStartX = 620;
	const int EagleEndX = -80;
	const int EagleOffsetY = 2;
	const int PulsingMoonStartWait = -70;
	const int MoonSteps = 2;
	// text box (L928 in the extro)
	static readonly Rect TextBoxArea = new(20, 150, 281, 43);
	static readonly Rect TextBoxInnerArea = new(23, 153, 275, 37);
	static readonly Position TextPosition = new(25, 155);
	const int TextBoxColorIndex = 15;
	const int TextBoxInnerColorIndex = 3;

	IOutroData outro = null!;
	Phase phase = Phase.ThroneRoomFadeIn;
	long verticalBlankTicks = 0;
	int verticalBlanks = 0;
	int fadeStep = 0;
	bool redMoon = false;
	// images with the palette they use
	readonly List<(Image Image, OutroPalette Palette)> images = [];
	readonly List<(IColoredRect Rect, OutroPalette Palette, int ColorIndex)> rects = [];
	readonly List<(Image Image, OutroPalette Palette)> textBox = [];
	// throne room
	int torchFrame = 0;
	int chancellorFrame = 0;
	int chancellorWait = ChancellorStartWait;
	int chancellorTextIndex = 0;
	bool chancellorTextsShown = false;
	Image? leftTorch;
	Image? rightTorch;
	Image? chancellor;
	// balcony
	int scriptPosition = 0;
	int scriptWait = 0;
	int balconyTextIndex = 0;
	readonly int[] personFrames = [0, 1, 1];
	readonly Image?[] persons = new Image?[3];
	int dragonX = DragonStartX;
	int dragonFrame = 0;
	int eagleX = EagleStartX;
	int eagleFrame = 3;
	int pulsingMoonPhase = PulsingMoonStartWait;
	int moonSteps = 0;
	bool canLeave = false;
	readonly List<(Image Image, OutroPalette Palette)> flyingSprites = [];

	public override ScreenType Type { get; } = ScreenType.Outro;

	public override bool CloseOnEscape { get; } = false;
	public override bool AllowInventoryAccess { get; } = false;
	public override bool AllowCharacterSelection { get; } = false;
	public override bool UpdateWhilePaused { get; } = true;

	/// <summary>
	/// The texts which were shown (used by scenario scripts).
	/// </summary>
	internal List<string> ShownTexts { get; } = [];

	internal bool CanLeave => canLeave;

	public override void Open(Action? closeAction)
	{
		base.Open(closeAction);

		outro = Game.AssetProvider.OutroData!;
		Game.Cursor.CursorType = CursorType.Sword;
		Game.PlaySong(outro.Music != null ? Game.OutroSong : FallbackSong);

		// The palettes fade in from black.
		phase = Phase.ThroneRoomFadeIn;
		fadeStep = -outro.FadeSteps;
		ShowThroneRoom();
	}

	public override void Close()
	{
		ClearImages();

		base.Close();
	}

	/// <summary>
	/// The space key (or a click) ends the outro after the last text.
	/// </summary>
	void Leave()
	{
		if (canLeave && phase == Phase.Balcony)
			StartPhase(Phase.BalconyFadeOut);
	}

	public override bool KeyDown(Key key, KeyModifiers keyModifiers)
	{
		if (key == Key.Space)
			Leave();

		return true;
	}

	public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		if (buttons == MouseButtons.Left)
			Leave();

		return true;
	}

	public override void Update(long elapsedTicks)
	{
		base.Update(elapsedTicks);

		verticalBlankTicks += elapsedTicks * VerticalBlanksPerSecond;
		int newVerticalBlanks = (int)(verticalBlankTicks / Game.TicksPerSecond);
		verticalBlankTicks %= Game.TicksPerSecond;

		for (int i = 0; i < newVerticalBlanks && phase != Phase.Finished; i++)
			VerticalBlank();
	}

	void VerticalBlank()
	{
		verticalBlanks++;

		switch (phase)
		{
			case Phase.ThroneRoomFadeIn:
			case Phase.BalconyFadeIn:
				if (verticalBlanks % outro.FadeInVerticalBlanks == 0)
				{
					// from black (-7) to the steps 1 to 7 (15 on the Amiga)
					SetFadeStep(Math.Max(1, fadeStep + 1));

					if (fadeStep == outro.FadeSteps)
						StartPhase(phase + 1);
				}
				break;
			case Phase.ThroneRoomFadeOut:
			case Phase.BalconyFadeOut:
				if (verticalBlanks % outro.FadeOutVerticalBlanks == 0)
				{
					SetFadeStep(fadeStep - 1);

					if (fadeStep == -outro.FadeSteps)
						StartPhase(phase + 1);
				}
				break;
			case Phase.ThroneRoom:
				if (verticalBlanks % VerticalBlanksPerStep == 0)
					ThroneRoomStep();
				break;
			case Phase.Balcony:
				if (verticalBlanks % VerticalBlanksPerStep == 0)
					BalconyStep();
				break;
		}
	}

	void StartPhase(Phase newPhase)
	{
		phase = newPhase;
		verticalBlanks = 0;

		switch (phase)
		{
			case Phase.ThroneRoom:
			case Phase.Balcony:
				SetFadeStep(0);
				break;
			case Phase.BalconyFadeIn:
				fadeStep = -outro.FadeSteps;
				ShowBalcony();
				break;
			case Phase.Finished:
				// The restart closes all screens (also the outro). Popping the outro before would fade and
				// show the map after it was closed.
				Game.RestartGame();
				break;
		}
	}

	#region Drawing

	void ClearImages()
	{
		images.ForEach(entry => entry.Image.Destroy());
		images.Clear();
		rects.ForEach(entry => Game.Destroy(entry.Rect));
		rects.Clear();
		ClearTextBox();
		flyingSprites.Clear();
		leftTorch = rightTorch = chancellor = null;
		Array.Fill(persons, null);
	}

	/// <summary>
	/// While fading in, the step counts from 1 to 7, while fading out from -1 to -7 (15 on the Amiga).
	/// 0 is the normal palette.
	/// </summary>
	void SetFadeStep(int step)
	{
		fadeStep = step;

		foreach (var (image, palette) in images.Concat(textBox))
			image.PaletteIndex = PaletteIndex(palette);

		foreach (var (rect, palette, colorIndex) in rects)
			rect.Color = Color(palette, colorIndex);
	}

	OutroPalette ActualPalette(OutroPalette palette) => palette == OutroPalette.Sky && redMoon ? OutroPalette.RedMoon : palette;

	byte PaletteIndex(OutroPalette palette) => Game.PaletteIndexProvider.GetOutroPaletteIndex(ActualPalette(palette), fadeStep);

	Color Color(OutroPalette palette, int colorIndex) => outro.GetPalette(ActualPalette(palette), fadeStep).GetColorAt(colorIndex, 0);

	/// <summary>
	/// Adds an image. Images are clipped at the bottom of the screen and at the top (negative y).
	/// </summary>
	Image? AddImage(OutroGraphic graphic, int frame, Position position, OutroPalette palette, byte displayLayer, bool opaque = false,
		List<(Image Image, OutroPalette Palette)>? list = null)
	{
		var source = outro.GetGraphics(graphic)[frame];
		int width = source.Width;
		int height = Math.Min(source.Height, IOutroData.PictureHeight - position.Y);
		int skip = Math.Max(0, -position.Y);

		if (height - skip <= 0)
			return null;

		var image = new Image(Game, new Position(position.X, position.Y + skip), new Size(width, height - skip),
			Game.GraphicIndexProvider.GetOutroGraphicIndex(graphic, frame), displayLayer, Layer.Cutscene, PaletteIndex(palette), opaque);

		// Images which start above the screen show their lower part.
		if (skip != 0)
			image.SetTextureIndex(Game.GraphicIndexProvider.GetOutroGraphicIndex(graphic, frame), new Position(0, skip));

		(list ?? images).Add((image, palette));

		return image;
	}

	void RemoveImage(Image? image)
	{
		if (image == null)
			return;

		image.Destroy();
		images.RemoveAll(entry => entry.Image == image);
	}

	void AddRect(Rect area, OutroPalette palette, int colorIndex, byte displayLayer)
	{
		var rect = Game.CreateColoredRect(Layer.Cutscene, area.Position, area.Size, Color(palette, colorIndex));

		if (rect != null)
		{
			rect.DisplayLayer = displayLayer;
			rects.Add((rect, palette, colorIndex));
		}
	}

	void ClearTextBox()
	{
		textBox.ForEach(entry => entry.Image.Destroy());
		textBox.Clear();
	}

	/// <summary>
	/// The text box is drawn into the picture and stays until the next text (L928 and L11d4 in the extro).
	/// </summary>
	void ShowText(string text, OutroPalette palette)
	{
		ClearTextBox();

		if (rects.Count == 0)
		{
			AddRect(TextBoxArea, palette, TextBoxColorIndex, TextBoxDisplayLayer);
			AddRect(TextBoxInnerArea, palette, TextBoxInnerColorIndex, TextBoxDisplayLayer + 1);
		}

		int x = TextPosition.X;
		int y = TextPosition.Y;

		foreach (char ch in text)
		{
			if (ch == '\n')
			{
				x = TextPosition.X;
				y += IOutroData.LineHeight;
				continue;
			}

			int? glyph = Game.GraphicIndexProvider.GetOutroGlyphIndex(ch);

			if (glyph != null)
			{
				var image = new Image(Game, new Position(x, y), new Size(IOutroData.GlyphWidth, IOutroData.GlyphHeight), glyph.Value,
					TextDisplayLayer, Layer.Cutscene, PaletteIndex(palette));
				textBox.Add((image, palette));
			}

			x += IOutroData.CharacterWidth;
		}

		ShownTexts.Add(text);
		Game.ScriptTextShown(text.Replace('\n', ' '));
	}

	#endregion

	#region Throne room

	/// <summary>
	/// The figure of a party member depends on its race (the outro event at $a262 in the game).
	/// </summary>
	static int GetPartyFigure(IPartyMember partyMember) => partyMember.Race switch
	{
		Race.Dwarf => 1,
		Race.Gnome => 2,
		_ => 0
	};

	void ShowThroneRoom()
	{
		ClearImages();
		AddImage(OutroGraphic.ThroneRoomTop, 0, new(0, 0), OutroPalette.ThroneRoom, PictureDisplayLayer, true);
		AddImage(OutroGraphic.ThroneRoomBottom, 0, new(0, IOutroData.ThroneRoomLowerLine), OutroPalette.ThroneRoomLower, PictureDisplayLayer, true);

		foreach (var (slotIndex, partyMember) in Game.State.PartyMembersWithSlot)
		{
			if (partyMember == null)
				continue;

			int figure = GetPartyFigure(partyMember);
			var position = outro.PartyFigurePositions[slotIndex];
			AddImage(OutroGraphic.PartyFigure, figure, new(position.X + PartyFigureOffsetX,
				position.Y + outro.PartyFigureYOffsets[figure] + PartyFigureOffsetY), OutroPalette.ThroneRoomLower, FigureDisplayLayer);
		}

		UpdateThroneRoomSprites();
	}

	void UpdateThroneRoomSprites()
	{
		RemoveImage(leftTorch);
		RemoveImage(rightTorch);
		RemoveImage(chancellor);
		leftTorch = AddImage(OutroGraphic.LeftTorch, torchFrame, LeftTorchPosition, OutroPalette.ThroneRoom, SpriteDisplayLayer);
		rightTorch = AddImage(OutroGraphic.RightTorch, torchFrame, RightTorchPosition, OutroPalette.ThroneRoom, SpriteDisplayLayer);
		chancellor = AddImage(OutroGraphic.Chancellor, chancellorFrame, ChancellorPosition, OutroPalette.ThroneRoom, SpriteDisplayLayer);
	}

	/// <summary>
	/// The torches flicker. The chancellor waits, stands up and then shows his texts one after another (L7e8 in the extro).
	/// </summary>
	void ThroneRoomStep()
	{
		torchFrame = (torchFrame + 1) % outro.LeftTorch.Count;

		if (--chancellorWait == 0)
		{
			chancellorFrame++;
			chancellorWait = 1;

			if (chancellorFrame == ChancellorLastFrame)
				chancellorWait = ChancellorEndWait;
		}

		UpdateThroneRoomSprites();

		if (!chancellorTextsShown && chancellorWait == TextWait)
		{
			chancellorWait = 0;
			ShowText(outro.ChancellorTexts[chancellorTextIndex++], OutroPalette.ThroneRoomLower);
			chancellorTextsShown = chancellorTextIndex == outro.ChancellorTexts.Count;
		}
		else if (chancellorTextsShown && chancellorWait == TextWait)
		{
			StartPhase(Phase.ThroneRoomFadeOut);
		}
	}

	#endregion

	#region Balcony

	void ShowBalcony()
	{
		ClearImages();
		AddImage(OutroGraphic.BalconySky, 0, new(0, 0), OutroPalette.Sky, PictureDisplayLayer, true);
		AddImage(OutroGraphic.BalconyLandscape, 0, new(0, IOutroData.LandscapeLine), OutroPalette.Landscape, PictureDisplayLayer, true);
		AddImage(OutroGraphic.BalconyLower, 0, new(0, IOutroData.LandscapeLowerLine), OutroPalette.LandscapeLower, PictureDisplayLayer, true);
		UpdatePersons();
	}

	void UpdatePersons()
	{
		for (int i = 0; i < persons.Length; i++)
		{
			RemoveImage(persons[i]);
			persons[i] = AddImage(OutroGraphic.BalconyPerson1 + i, personFrames[i], outro.BalconyPeoplePositions[i], OutroPalette.LandscapeLower,
				SpriteDisplayLayer);
		}
	}

	/// <summary>
	/// The script moves the people and shows the texts. The dragon and the eagle fly to the left, later the
	/// third moon pulses (L49e in the extro).
	/// </summary>
	void BalconyStep()
	{
		if (pulsingMoonPhase < 0)
		{
			eagleX = Math.Max(EagleEndX, eagleX - 2);

			if (--dragonX <= DragonEndX)
			{
				dragonX = DragonEndX;
				pulsingMoonPhase++;
			}

			eagleFrame = (eagleFrame + 1) % outro.Eagle.Count;
			dragonFrame = (dragonFrame + 1) % outro.Dragon.Count;
		}

		RunScript();

		flyingSprites.ForEach(entry => RemoveImage(entry.Image));
		flyingSprites.Clear();

		// the pulsing third moon
		if (pulsingMoonPhase >= 0)
		{
			pulsingMoonPhase = (pulsingMoonPhase + 1) % PulsingMoonFrames.Length;
			AddFlyingSprite(OutroGraphic.PulsingMoon, PulsingMoonFrames[pulsingMoonPhase], PulsingMoonPosition, SpriteDisplayLayer);
		}

		// The dragon frames are shown backwards.
		int dragonIndex = outro.Dragon.Count - 1 - dragonFrame;
		// The eagle is drawn in front of the dragon.
		AddFlyingSprite(OutroGraphic.Dragon, dragonIndex, new(dragonX, DragonOffsetY + outro.DragonYOffsets[dragonIndex]), SpriteDisplayLayer);
		AddFlyingSprite(OutroGraphic.Eagle, eagleFrame, new(eagleX, EagleOffsetY + outro.EagleYOffsets[eagleFrame]), SpriteDisplayLayer + 1);

		UpdatePersons();

		// The moon is drawn into the picture and stays.
		if (moonSteps > 0)
		{
			if (moonSteps == MoonSteps)
				AddImage(OutroGraphic.Moon, 0, outro.MoonPosition, OutroPalette.Sky, FigureDisplayLayer);

			moonSteps--;
			canLeave = true;
		}
	}

	void AddFlyingSprite(OutroGraphic graphic, int frame, Position position, int displayLayer)
	{
		var image = AddImage(graphic, frame, position, OutroPalette.Sky, (byte)displayLayer);

		if (image != null)
			flyingSprites.Add((image, OutroPalette.Sky));
	}

	/// <summary>
	/// 0 n = wait n steps, 1-3 n = frame of a person, 4 = next text, 5 = red moon, 0xff = end (L554 in the extro).
	/// </summary>
	void RunScript()
	{
		if (--scriptWait > 0)
			return;

		scriptWait = 0;
		var script = outro.BalconyScript;

		while (scriptPosition < script.Count)
		{
			int command = script[scriptPosition];

			if (command >= 0x80)
				return;

			scriptPosition++;

			switch (command)
			{
				case 0:
					scriptWait = script[scriptPosition++];
					return;
				case 4:
					if (balconyTextIndex < outro.BalconyTexts.Count)
						ShowText(outro.BalconyTexts[balconyTextIndex++], OutroPalette.LandscapeLower);
					break;
				case 5:
					redMoon = true;
					moonSteps = MoonSteps;
					SetFadeStep(fadeStep);
					break;
				default:
					personFrames[command - 1] = script[scriptPosition++];
					break;
			}
		}
	}

	#endregion
}
