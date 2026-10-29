using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;

namespace Amberstar.Game.Screens;

/// <summary>
/// The intro (INTRO.68K in the original, a separate program which was disassembled). The title picture fades in and
/// stays until the space key is pressed. Then the stone background fades in, the music starts and the logo is drawn
/// line by line. Then the credits are shown page by page (each for 5 seconds, with one of 2 transitions) until the
/// space key is pressed. Like in the original everything is timed by vertical blanks (50 per second).
/// </summary>
internal sealed class IntroScreen : Screen
{
	enum Phase
	{
		TitleFadeIn,
		Title,
		TitleFadeOut,
		BackgroundFadeIn,
		Logo,
		Transition,
		Page,
		FadeOut,
		Finished
	}

	const int VerticalBlanksPerSecond = 50;
	// the title picture changes its fading step every 4 vertical blanks (24 in total)
	const int TitleFadeVerticalBlanks = 24;
	const int TitleVerticalBlanksPerStep = TitleFadeVerticalBlanks / (IIntroData.TitleFadeSteps - 1);
	const int VerticalBlanksPerFadeStep = 3;
	// each page is shown for 251 vertical blanks after its transition
	const int PageVerticalBlanks = 251;
	const byte PictureDisplayLayer = 200;
	const byte LogoDisplayLayer = 202;
	const byte PageDisplayLayer = 204;
	const byte NewPageDisplayLayer = 206;
	// transition 1: 5 bands of 22 lines, one more line of each band per vertical blank (L372 in the intro)
	const int BandCount = 5;
	const int BandHeight = 22;
	// transition 2: rows of 4 lines, the upper 2 lines are shown from the left, the lower 2 lines from the right,
	// 16 more pixels per vertical blank (L3a0 in the intro)
	const int RowCount = 28;
	const int RowHeight = 4;
	const int BlockWidth = 16;
	const int BlockCount = IIntroData.PictureWidth / BlockWidth;

	IIntroData intro = null!;
	Phase phase = Phase.TitleFadeIn;
	long verticalBlankTicks = 0;
	int verticalBlanks = 0;
	int fadeStep = 0;
	bool spacePressed = false;
	int pageIndex = 0;
	bool blockTransition = false;
	// the random numbers of the intro ($528)
	int randomSeed = 0x1be1;
	Image? title;
	Image? background;
	Image? logo;
	Image? page;
	readonly List<Image> newPageParts = [];

	public override ScreenType Type { get; } = ScreenType.Intro;

	public override ScreenFadeType FadeType { get; } = ScreenFadeType.None;
	public override bool CloseOnEscape { get; } = false;
	public override bool AllowInventoryAccess { get; } = false;
	public override bool AllowCharacterSelection { get; } = false;
	public override bool UpdateWhilePaused { get; } = true;

	/// <summary>
	/// The texts of the pages which were shown (used by scenario scripts).
	/// </summary>
	internal List<string> ShownTexts { get; } = [];

	public override void Open(Action? closeAction)
	{
		base.Open(closeAction);

		intro = Game.AssetProvider.IntroData!;
		Game.Cursor.Visible = false;
		phase = Phase.TitleFadeIn;
		verticalBlanks = 0;
		title = new Image(Game, new Position(0, 0), new Size(IIntroData.PictureWidth, IIntroData.PictureHeight),
			Game.GraphicIndexProvider.GetIntroGraphicIndex(IntroGraphic.TitlePicture, 0), PictureDisplayLayer, Layer.TrueColor, 0, true);
	}

	public override void Close()
	{
		title?.Destroy();
		background?.Destroy();
		logo?.Destroy();
		page?.Destroy();
		ClearNewPage();
		title = background = logo = page = null;
		Game.Cursor.Visible = true;

		base.Close();
	}

	/// <summary>
	/// The space key (or a click) continues after the title picture and ends the intro while the credits are shown.
	/// </summary>
	internal void Continue() => spacePressed = true;

	public override bool KeyDown(Key key, KeyModifiers keyModifiers)
	{
		if (key == Key.Space)
			Continue();

		return true;
	}

	public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		if (buttons == MouseButtons.Left)
			Continue();

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
			case Phase.TitleFadeIn:
				SetTitleStep(verticalBlanks / TitleVerticalBlanksPerStep);
				if (verticalBlanks == TitleFadeVerticalBlanks)
					StartPhase(Phase.Title);
				break;
			case Phase.Title:
				if (spacePressed)
					StartPhase(Phase.TitleFadeOut);
				break;
			case Phase.TitleFadeOut:
				SetTitleStep((TitleFadeVerticalBlanks - verticalBlanks) / TitleVerticalBlanksPerStep);
				// After the fading the screen is black for a vertical blank.
				if (verticalBlanks == TitleFadeVerticalBlanks + 1)
					StartPhase(Phase.BackgroundFadeIn);
				break;
			case Phase.BackgroundFadeIn:
				if (verticalBlanks % VerticalBlanksPerFadeStep == 0)
				{
					// from black (-7) to the steps 1 to 7
					SetFadeStep(Math.Max(1, fadeStep + 1));

					if (fadeStep == IIntroData.FadeSteps)
						StartPhase(Phase.Logo);
				}
				break;
			case Phase.Logo:
				ShowLogo(verticalBlanks);
				if (verticalBlanks == intro.Logo.Height)
					StartPage();
				break;
			case Phase.Transition:
				if (blockTransition ? UpdateBlockTransition() : UpdateBandTransition())
				{
					page?.Destroy();
					page = AddPagePart(pageIndex, 0, 0, IIntroData.PictureWidth, IIntroData.TextAreaHeight, PageDisplayLayer);
					ClearNewPage();
					StartPhase(spacePressed ? Phase.FadeOut : Phase.Page);
				}
				break;
			case Phase.Page:
				if (spacePressed)
					StartPhase(Phase.FadeOut);
				else if (verticalBlanks == PageVerticalBlanks)
				{
					pageIndex = (pageIndex + 1) % intro.TextPages.Count;
					StartPage();
				}
				break;
			case Phase.FadeOut:
				if (verticalBlanks % VerticalBlanksPerFadeStep == 0)
				{
					SetFadeStep(Math.Min(-1, fadeStep - 1));

					if (fadeStep == -IIntroData.FadeSteps)
						StartPhase(Phase.Finished);
				}
				break;
		}
	}

	void StartPhase(Phase newPhase)
	{
		phase = newPhase;
		verticalBlanks = 0;
		spacePressed = false;

		switch (phase)
		{
			case Phase.BackgroundFadeIn:
				title?.Destroy();
				title = null;
				fadeStep = -IIntroData.FadeSteps;
				background = new Image(Game, new Position(0, 0), new Size(IIntroData.PictureWidth, IIntroData.PictureHeight),
					Game.GraphicIndexProvider.GetIntroGraphicIndex(IntroGraphic.Background), PictureDisplayLayer, Layer.Cutscene, PaletteIndex, true);
				break;
			case Phase.Logo:
				SetFadeStep(0);
				if (intro.Music != null)
					Game.PlaySong(Game.IntroSong);
				break;
			case Phase.Finished:
				Game.PlaySong(0);
				Game.IntroFinished();
				break;
		}
	}

	byte PaletteIndex => Game.PaletteIndexProvider.GetIntroPaletteIndex(fadeStep);

	void SetTitleStep(int step)
	{
		title?.SetTextureIndex(Game.GraphicIndexProvider.GetIntroGraphicIndex(IntroGraphic.TitlePicture,
			Math.Clamp(step, 0, IIntroData.TitleFadeSteps - 1)));
	}

	void SetFadeStep(int step)
	{
		fadeStep = step;

		foreach (var image in new[] { background, logo, page }.Concat(newPageParts))
		{
			if (image != null)
				image.PaletteIndex = PaletteIndex;
		}
	}

	/// <summary>
	/// The logo is drawn line by line (one line per vertical blank).
	/// </summary>
	void ShowLogo(int lines)
	{
		logo?.Destroy();
		logo = new Image(Game, new Position(0, IIntroData.LogoY), new Size(IIntroData.PictureWidth, lines),
			Game.GraphicIndexProvider.GetIntroGraphicIndex(IntroGraphic.Logo), LogoDisplayLayer, Layer.Cutscene, PaletteIndex);
	}

	/// <summary>
	/// The text of the next page is drawn into the background, then one of 2 transitions shows it (chosen randomly).
	/// </summary>
	void StartPage()
	{
		blockTransition = (NextRandom() & 0x20) != 0;
		ShownTexts.Add(intro.Texts[pageIndex]);
		Game.ScriptTextShown(intro.Texts[pageIndex].Replace('\n', ' '));
		StartPhase(Phase.Transition);
	}

	int NextRandom()
	{
		long value = (long)(ushort)randomSeed * 0xe62d + 1;
		randomSeed = (int)(value & 0xffff);

		return (int)(value >> 8) & 0x7fff;
	}

	Image AddPagePart(int index, int x, int y, int width, int height, byte displayLayer)
	{
		var image = new Image(Game, new Position(x, IIntroData.TextAreaY + y), new Size(width, height),
			Game.GraphicIndexProvider.GetIntroGraphicIndex(IntroGraphic.TextPage, index), displayLayer, Layer.Cutscene, PaletteIndex, true);
		image.SetTextureIndex(Game.GraphicIndexProvider.GetIntroGraphicIndex(IntroGraphic.TextPage, index), new Position(x, y));

		return image;
	}

	void ClearNewPage()
	{
		newPageParts.ForEach(part => part.Destroy());
		newPageParts.Clear();
	}

	/// <summary>
	/// Each band shows one more line (22 steps). Returns true when the page is complete.
	/// </summary>
	bool UpdateBandTransition()
	{
		ClearNewPage();

		for (int band = 0; band < BandCount; band++)
			newPageParts.Add(AddPagePart(pageIndex, 0, band * BandHeight, IIntroData.PictureWidth, verticalBlanks, NewPageDisplayLayer));

		return verticalBlanks == BandHeight;
	}

	/// <summary>
	/// Each row shows 16 more pixels (20 steps). The space key ends it. Returns true when the page is complete.
	/// </summary>
	bool UpdateBlockTransition()
	{
		ClearNewPage();
		int width = verticalBlanks * BlockWidth;

		for (int row = 0; row < RowCount; row++)
		{
			newPageParts.Add(AddPagePart(pageIndex, 0, row * RowHeight, width, RowHeight / 2, NewPageDisplayLayer));
			newPageParts.Add(AddPagePart(pageIndex, IIntroData.PictureWidth - width, row * RowHeight + RowHeight / 2, width,
				RowHeight / 2, NewPageDisplayLayer));
		}

		return verticalBlanks == BlockCount || spacePressed;
	}
}
