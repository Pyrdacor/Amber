using Amber.Assets.Common;
using Amber.Common;
using Amber.Serialization;

namespace Amberstar.GameData.Legacy;

/// <summary>
/// The outro program (unpacked): EXTRO.68K of the Atari ST versions and EXTRO.UDO of the Amiga versions.
/// The code of each platform is the same in all languages, only the addresses of the data differ. So the data
/// addresses are read from the instructions at fixed code offsets.
///
/// The Amiga outro was rewritten for the Amiga hardware. The sprites, texts and the script are the same, but the
/// pictures are stored in the Amiga format, the palettes have 4 bits per color channel and fade in 15 steps, and
/// the copper changes color 5 every few lines (a gradient). The pixels of color 5 in these lines use the colors
/// from 16 of the palettes instead, so each part of a picture still needs only one palette.
/// </summary>
internal class OutroData : IOutroData
{
	/// <summary>
	/// The code offsets of the address operands (lea abs.l or move.l #imm) and of other values.
	/// </summary>
	sealed record CodeOffsets(int ThroneRoom, int ThroneRoomPalette, int Balcony, int SkyPalette, int LandscapePalette,
		int RedMoonPalette, int Moon, int MoonX, int MoonWidth, int PulsingMoon, int DragonYOffsets, int Dragon,
		int EagleYOffsets, int Eagle, int BalconyPeopleRects, int[] BalconyPeople, int ChancellorTexts, int BalconyTexts,
		int BalconyScript, int LeftTorch, int RightTorch, int Chancellor, int PartyFigurePositions, int PartyFigureYOffsets,
		int PartyFigures, int FontMap, int Glyphs, int Music, int ThroneRoomCopper, int BalconyCopper);

	static readonly CodeOffsets AtariOffsets = new(ThroneRoom: 0x1a8, ThroneRoomPalette: 0x1f8, Balcony: 0x274,
		SkyPalette: 0x2c8, LandscapePalette: 0x2da, RedMoonPalette: 0x5a4, Moon: 0x5f8, MoonX: 0x5ef, MoonWidth: 0x5f3,
		PulsingMoon: 0x670, DragonYOffsets: 0x6be, Dragon: 0x6ca, EagleYOffsets: 0x71c, Eagle: 0x728, BalconyPeopleRects: 0x76a,
		BalconyPeople: [0x792, 0x7b2, 0x7d2], ChancellorTexts: 0x7ea, BalconyTexts: 0x4a0, BalconyScript: 0x130,
		LeftTorch: 0x982, RightTorch: 0x99a, Chancellor: 0x9be, PartyFigurePositions: 0x9d8, PartyFigureYOffsets: 0x9ee,
		PartyFigures: 0xa00, FontMap: 0x11f6, Glyphs: 0x1214, Music: 0x226, ThroneRoomCopper: 0, BalconyCopper: 0);

	static readonly CodeOffsets AmigaOffsets = new(ThroneRoom: 0x144, ThroneRoomPalette: 0x180, Balcony: 0x224,
		SkyPalette: 0x270, LandscapePalette: 0x28c, RedMoonPalette: 0x442, Moon: 0x4a0, MoonX: 0x497, MoonWidth: 0x49b,
		PulsingMoon: 0x552, DragonYOffsets: 0x5a0, Dragon: 0x5ac, EagleYOffsets: 0x5fe, Eagle: 0x60a, BalconyPeopleRects: 0x64c,
		BalconyPeople: [0x674, 0x694, 0x6b4], ChancellorTexts: 0x6cc, BalconyTexts: 0x336, BalconyScript: 0xa8,
		LeftTorch: 0x8b2, RightTorch: 0x8ca, Chancellor: 0x8ee, PartyFigurePositions: 0x908, PartyFigureYOffsets: 0x91e,
		PartyFigures: 0x930, FontMap: 0x1676, Glyphs: 0x1694, Music: 0, ThroneRoomCopper: 0x40, BalconyCopper: 0x1ea);

	const int PictureSize = IOutroData.PictureWidth * IOutroData.PictureHeight / 2;
	const int PictureLineSize = IOutroData.PictureWidth / 2;
	const int PlaneLineSize = IOutroData.PictureWidth / 8;
	// the Amiga outro: the first line of the display (DIWSTRT $3081), the copper lists start with 8 bit plane pointers
	// and 16 colors, then the gradient of color 5 follows (WAIT and MOVE, L976 and L9a6 in the extro)
	const int AmigaFirstLine = 0x30;
	const int CopperGradientOffset = 8 * 4 + 16 * 4;
	const int GradientSteps = 9;
	const int GradientColorIndex = 5;
	const int FirstGradientColor = 16;
	// the landscape palette of the Amiga outro: 16 colors, the gradient of the sky and color 5 of the lower lines
	const int AmigaLandscapeLowerColor5 = 16 + GradientSteps;
	const byte FontColor = 15;
	// the special characters of the texts
	static readonly Dictionary<byte, char> SpecialCharacters = new()
	{
		{ 0x8e, 'Ä' }, { 0x99, 'Ö' }, { 0x9a, 'Ü' }, { 0x9e, 'ß' }, { 0x84, 'ä' }, { 0x94, 'ö' }, { 0x81, 'ü' }
	};

	readonly byte[] data;
	readonly bool amiga;
	readonly CodeOffsets offsets;
	readonly Dictionary<char, IGraphic?> glyphs = [];
	// the colors (Atari ST or Amiga format) of each palette
	readonly Dictionary<OutroPalette, int[]> palettes = [];

	public OutroData(byte[] data, LegacyPlatform platform)
	{
		this.data = data;
		amiga = platform == LegacyPlatform.Amiga;
		offsets = amiga ? AmigaOffsets : AtariOffsets;
		FadeSteps = amiga ? 15 : 7;
		// fading in takes 5 vertical blanks per step on the Atari ST (La36 in the extro), 3 on the Amiga (L186)
		FadeInVerticalBlanks = amiga ? 3 : 5;
		FadeOutVerticalBlanks = 3;

		if (amiga)
			LoadAmigaPictures();
		else
			LoadAtariPictures();

		LeftTorch = Frames(Address(offsets.LeftTorch), 3, 1, 46);
		RightTorch = Frames(Address(offsets.RightTorch), 3, 1, 46);
		Chancellor = Frames(Address(offsets.Chancellor), 7, 2, 37);
		PulsingMoon = Frames(Address(offsets.PulsingMoon), 4, 1, 14);
		// The moon of the German versions is wider (moveq #x,d0 and moveq #width,d5).
		Moon = Sprite(Address(offsets.Moon), data[offsets.MoonWidth], 32);
		MoonPosition = new(data[offsets.MoonX], 20);

		// The party figures have word offsets, the dragons long offsets to their frames (with width and height).
		int partyFigures = Address(offsets.PartyFigures);
		PartyFigures = [.. Enumerable.Range(0, 3).Select(i => SpriteWithSize(partyFigures + Word(partyFigures + i * 2)))];
		PartyFigureYOffsets = Words(Address(offsets.PartyFigureYOffsets), 3);
		var partyPositions = Words(Address(offsets.PartyFigurePositions), 12);
		PartyFigurePositions = [.. Enumerable.Range(0, 6).Select(i => new Position(partyPositions[i * 2], partyPositions[i * 2 + 1]))];
		int dragon = Address(offsets.Dragon);
		Dragon = [.. Enumerable.Range(0, 12).Select(i => SpriteWithSize(dragon + Long(dragon + i * 4)))];
		DragonYOffsets = Words(Address(offsets.DragonYOffsets), 12);
		int eagle = Address(offsets.Eagle);
		Eagle = [.. Enumerable.Range(0, 8).Select(i => SpriteWithSize(eagle + Long(eagle + i * 4)))];
		EagleYOffsets = Words(Address(offsets.EagleYOffsets), 8);

		// The rectangles which restore the background behind the people give their positions and sizes.
		int rects = Address(offsets.BalconyPeopleRects);
		var people = new List<IReadOnlyList<IGraphic>>();
		var peoplePositions = new List<Position>();

		for (int i = 0; i < 3; i++)
		{
			var rect = Words(rects + i * 8, 4);
			int width = rect[2] + 1;
			int height = rect[3];
			peoplePositions.Add(new(rect[0], rect[1]));
			people.Add(Frames(Address(offsets.BalconyPeople[i]), 4, width, height));
		}

		BalconyPeople = people;
		BalconyPeoplePositions = peoplePositions;

		int script = Address(offsets.BalconyScript);
		int scriptEnd = script;

		// commands with a parameter until 0xff
		while (data[scriptEnd] != 0xff)
			scriptEnd += data[scriptEnd] is 4 or 5 ? 1 : 2;

		BalconyScript = data[script..(scriptEnd + 1)];
		ChancellorTexts = Texts(Address(offsets.ChancellorTexts));
		BalconyTexts = Texts(Address(offsets.BalconyTexts));

		Music = amiga ? null : LoadMusic(Address(offsets.Music));
	}

	/// <summary>
	/// The pictures of the Atari ST are 4 interleaved bit planes. Some palettes have a 17th color which replaces color 5
	/// below a line.
	/// </summary>
	void LoadAtariPictures()
	{
		ThroneRoom = Picture(Address(offsets.ThroneRoom));
		Balcony = Picture(Address(offsets.Balcony));
		var throneRoomPalette = Address(offsets.ThroneRoomPalette);
		var landscapePalette = Address(offsets.LandscapePalette);
		palettes.Add(OutroPalette.ThroneRoom, PaletteColors(throneRoomPalette, null));
		palettes.Add(OutroPalette.ThroneRoomLower, PaletteColors(throneRoomPalette, Word(throneRoomPalette + 32)));
		palettes.Add(OutroPalette.Sky, PaletteColors(Address(offsets.SkyPalette), null));
		palettes.Add(OutroPalette.RedMoon, PaletteColors(Address(offsets.RedMoonPalette), null));
		palettes.Add(OutroPalette.Landscape, PaletteColors(landscapePalette, null));
		palettes.Add(OutroPalette.LandscapeLower, PaletteColors(landscapePalette, Word(landscapePalette + 32)));
	}

	/// <summary>
	/// The pictures of the Amiga have the 4 bit planes of each line after another. The copper lists change color 5
	/// at some lines: in the throne room from line 46 to 106, in the sky of the balcony from line 2 to 64 (L976 and
	/// L9a6 in the extro). The throne room palette has these 9 colors after its 16 colors, the landscape palette
	/// has the colors of the sky and then color 5 of the lower lines.
	/// </summary>
	void LoadAmigaPictures()
	{
		var throneRoomLines = GradientLines(Address(offsets.ThroneRoomCopper));
		var skyLines = GradientLines(Address(offsets.BalconyCopper));
		ThroneRoom = AmigaPicture(Address(offsets.ThroneRoom), throneRoomLines, IOutroData.PictureHeight);
		Balcony = AmigaPicture(Address(offsets.Balcony), skyLines, IOutroData.LandscapeLine);

		var throneRoomPalette = Words(Address(offsets.ThroneRoomPalette), 16 + GradientSteps);
		var landscapePalette = Words(Address(offsets.LandscapePalette), 16 + GradientSteps + 1);
		var skyGradient = landscapePalette[16..(16 + GradientSteps)];
		palettes.Add(OutroPalette.ThroneRoom, throneRoomPalette);
		// the party figures and the text box are below the gradient
		var throneRoomLower = throneRoomPalette.ToArray();
		throneRoomLower[GradientColorIndex] = throneRoomPalette[^1];
		palettes.Add(OutroPalette.ThroneRoomLower, throneRoomLower);
		palettes.Add(OutroPalette.Sky, [.. Words(Address(offsets.SkyPalette), 16), .. skyGradient]);
		palettes.Add(OutroPalette.RedMoon, [.. Words(Address(offsets.RedMoonPalette), 16), .. skyGradient]);
		palettes.Add(OutroPalette.Landscape, landscapePalette[0..16]);
		var landscapeLower = landscapePalette[0..16];
		landscapeLower[GradientColorIndex] = landscapePalette[AmigaLandscapeLowerColor5];
		palettes.Add(OutroPalette.LandscapeLower, landscapeLower);
	}

	/// <summary>
	/// The lines of the WAIT instructions which change color 5 in a copper list.
	/// </summary>
	int[] GradientLines(int copperList) =>
		[.. Enumerable.Range(0, GradientSteps).Select(i => data[copperList + CopperGradientOffset + i * 8] - AmigaFirstLine)];

	/// <summary>
	/// The pixels of color 5 from a gradient line (until the end line) use the colors from 16 of the palette.
	/// </summary>
	IGraphic AmigaPicture(int offset, int[] gradientLines, int gradientEndLine)
	{
		var pixels = new byte[IOutroData.PictureWidth * IOutroData.PictureHeight];
		int gradientStep = -1;

		for (int y = 0; y < IOutroData.PictureHeight; y++)
		{
			while (gradientStep + 1 < gradientLines.Length && y >= gradientLines[gradientStep + 1])
				gradientStep++;

			int line = offset + y * PictureLineSize;

			for (int x = 0; x < IOutroData.PictureWidth; x++)
			{
				int color = 0;

				for (int plane = 0; plane < 4; plane++)
				{
					if ((data[line + plane * PlaneLineSize + x / 8] & (0x80 >> (x % 8))) != 0)
						color |= 1 << plane;
				}

				if (color == GradientColorIndex && gradientStep >= 0 && y < gradientEndLine)
					color = FirstGradientColor + gradientStep;

				pixels[y * IOutroData.PictureWidth + x] = (byte)color;
			}
		}

		return new Graphic(IOutroData.PictureWidth, IOutroData.PictureHeight, pixels, GraphicFormat.PaletteIndices);
	}

	public IGraphic ThroneRoom { get; private set; } = null!;
	public IGraphic Balcony { get; private set; } = null!;
	public int FadeSteps { get; }
	public int FadeInVerticalBlanks { get; }
	public int FadeOutVerticalBlanks { get; }
	public Position MoonPosition { get; }
	public IReadOnlyList<IGraphic> LeftTorch { get; }
	public IReadOnlyList<IGraphic> RightTorch { get; }
	public IReadOnlyList<IGraphic> Chancellor { get; }
	public IReadOnlyList<IGraphic> PartyFigures { get; }
	public IReadOnlyList<int> PartyFigureYOffsets { get; }
	public IReadOnlyList<Position> PartyFigurePositions { get; }
	public IReadOnlyList<IGraphic> PulsingMoon { get; }
	public IReadOnlyList<IGraphic> Dragon { get; }
	public IReadOnlyList<int> DragonYOffsets { get; }
	public IReadOnlyList<IGraphic> Eagle { get; }
	public IReadOnlyList<int> EagleYOffsets { get; }
	public IGraphic Moon { get; }
	public IReadOnlyList<IReadOnlyList<IGraphic>> BalconyPeople { get; }
	public IReadOnlyList<Position> BalconyPeoplePositions { get; }
	public IReadOnlyList<byte> BalconyScript { get; }
	public IReadOnlyList<string> ChancellorTexts { get; }
	public IReadOnlyList<string> BalconyTexts { get; }
	public ISong? Music { get; }

	public IReadOnlyList<IGraphic> GetGraphics(OutroGraphic graphic) => graphic switch
	{
		OutroGraphic.ThroneRoomTop => [PicturePart(ThroneRoom, 0, IOutroData.ThroneRoomLowerLine)],
		OutroGraphic.ThroneRoomBottom => [PicturePart(ThroneRoom, IOutroData.ThroneRoomLowerLine, IOutroData.PictureHeight)],
		OutroGraphic.BalconySky => [PicturePart(Balcony, 0, IOutroData.LandscapeLine)],
		OutroGraphic.BalconyLandscape => [PicturePart(Balcony, IOutroData.LandscapeLine, IOutroData.LandscapeLowerLine)],
		OutroGraphic.BalconyLower => [PicturePart(Balcony, IOutroData.LandscapeLowerLine, IOutroData.PictureHeight)],
		OutroGraphic.LeftTorch => LeftTorch,
		OutroGraphic.RightTorch => RightTorch,
		OutroGraphic.Chancellor => Chancellor,
		OutroGraphic.PartyFigure => PartyFigures,
		OutroGraphic.PulsingMoon => PulsingMoon,
		OutroGraphic.Dragon => Dragon,
		OutroGraphic.Eagle => Eagle,
		OutroGraphic.Moon => [Moon],
		OutroGraphic.BalconyPerson1 => BalconyPeople[0],
		OutroGraphic.BalconyPerson2 => BalconyPeople[1],
		OutroGraphic.BalconyPerson3 => BalconyPeople[2],
		_ => []
	};

	static IGraphic PicturePart(IGraphic picture, int startLine, int endLine)
	{
		int width = picture.Width;

		return new Graphic(width, endLine - startLine, picture.GetData()[(startLine * width)..(endLine * width)], GraphicFormat.PaletteIndices);
	}

	int Word(int offset) => (data[offset] << 8) | data[offset + 1];

	int SignedWord(int offset) => (short)Word(offset);

	int Long(int offset) => (Word(offset) << 16) | Word(offset + 2);

	int Address(int codeOffset) => Long(codeOffset);

	int[] Words(int offset, int count) => [.. Enumerable.Range(0, count).Select(i => SignedWord(offset + i * 2))];

	IGraphic Picture(int offset)
	{
		return Graphic.FromBitPlanes(IOutroData.PictureWidth, IOutroData.PictureHeight, data[offset..(offset + PictureSize)], 4);
	}

	/// <summary>
	/// The palettes have 16 colors. Some have a 17th color which replaces color 5 below a line.
	/// </summary>
	int[] PaletteColors(int offset, int? color5)
	{
		var colors = Words(offset, 16);

		if (color5 != null)
			colors[5] = color5.Value;

		return colors;
	}

	/// <summary>
	/// Fading in increases each color channel by 1 per step up to its value (La36 in the extro, Lb24 on the Amiga),
	/// fading out decreases each channel by 1 per step (La96 in the extro, Lb7c on the Amiga). The channels have
	/// 3 bits on the Atari ST and 4 bits on the Amiga.
	/// </summary>
	public IGraphic GetPalette(OutroPalette palette, int fadeStep = 0)
	{
		var colors = palettes[palette];
		var bytes = new byte[colors.Length * 2];
		int channelMask = amiga ? 0xf : 0x7;

		for (int i = 0; i < colors.Length; i++)
		{
			int color = 0;

			for (int shift = 0; shift <= 8; shift += 4)
			{
				int channel = (colors[i] >> shift) & channelMask;

				if (fadeStep > 0)
					channel = Math.Min(channel, fadeStep);
				else if (fadeStep < 0)
					channel = Math.Max(0, channel + fadeStep);

				color |= channel << shift;
			}

			bytes[i * 2] = (byte)(color >> 8);
			bytes[i * 2 + 1] = (byte)color;
		}

		// The Atari ST colors are converted to 4 bits per channel.
		return Graphic.FromPalette(amiga ? bytes : PaletteLoader.LoadPaletteColors(new DataReader(bytes), colors.Length));
	}

	/// <summary>
	/// Sprites have 4 interleaved bit planes for each block of 16 pixels. Color 0 is transparent.
	/// </summary>
	IGraphic Sprite(int offset, int widthInBlocks, int height) =>
		Graphic.FromBitPlanes(widthInBlocks * 16, height, data[offset..(offset + widthInBlocks * height * 8)], 4);

	IGraphic SpriteWithSize(int offset) => Sprite(offset + 4, Word(offset), Word(offset + 2));

	List<IGraphic> Frames(int offset, int count, int widthInBlocks, int height) =>
		[.. Enumerable.Range(0, count).Select(i => Sprite(offset + i * widthInBlocks * height * 8, widthInBlocks, height))];

	char ToChar(byte value) => value == 0x0d ? '\n' : SpecialCharacters.TryGetValue(value, out var ch) ? ch : (char)value;

	byte FromChar(char ch) => SpecialCharacters.FirstOrDefault(c => c.Value == ch, new((byte)ch, ch)).Key;

	/// <summary>
	/// The texts end with 0 and the list with 0xff.
	/// </summary>
	List<string> Texts(int offset)
	{
		var texts = new List<string>();

		while (data[offset] != 0xff)
		{
			int end = Array.IndexOf(data, (byte)0, offset);
			texts.Add(new string([.. data[offset..end].Select(ToChar)]));
			offset = end + 1;
		}

		return texts;
	}

	/// <summary>
	/// The font has a map from the characters (from space) to the glyphs (5 bytes each, L11d4 in the extro).
	/// </summary>
	public IGraphic? GetGlyph(char ch)
	{
		if (glyphs.TryGetValue(ch, out var glyph))
			return glyph;

		int index = FromChar(ch) - 32;
		glyph = null;

		if (index >= 0 && index < 0xe0)
		{
			int glyphIndex = (sbyte)data[Address(offsets.FontMap) + index];

			if (glyphIndex >= 0)
			{
				int offset = Address(offsets.Glyphs) + glyphIndex * IOutroData.GlyphHeight;
				var pixels = new byte[IOutroData.GlyphWidth * IOutroData.GlyphHeight];

				for (int y = 0; y < IOutroData.GlyphHeight; y++)
				{
					for (int x = 0; x < IOutroData.GlyphWidth; x++)
					{
						if ((data[offset + y] & (0x80 >> x)) != 0)
							pixels[y * IOutroData.GlyphWidth + x] = FontColor;
					}
				}

				glyph = new Graphic(IOutroData.GlyphWidth, IOutroData.GlyphHeight, pixels, GraphicFormat.PaletteIndices);
			}
		}

		glyphs.Add(ch, glyph);

		return glyph;
	}

	/// <summary>
	/// The music player of the outro emulates the 4 sample voices of the Amiga (mixed by software) and plays a COSO module
	/// with samples (TFMX variant, 4 voices with 3 bytes per division) which follows the player code.
	/// </summary>
	ISong? LoadMusic(int playerOffset)
	{
		int offset = playerOffset;

		while (offset + 0x20 <= data.Length && !(data[offset] == 'C' && data[offset + 1] == 'O' && data[offset + 2] == 'S' && data[offset + 3] == 'O'))
			offset += 2;

		if (offset + 0x20 > data.Length)
			return null;

		// The samples follow the module.
		return new HippelCosoPaulaSong(data[offset..]);
	}
}
