using Amber.Assets.Common;
using Amber.Common;
using Amber.Serialization;

namespace Amberstar.GameData.Legacy;

/// <summary>
/// The outro program EXTRO.68K (unpacked). The code is the same in all versions, only the addresses of the data
/// differ. So the data addresses are read from the instructions at fixed code offsets.
/// </summary>
internal class OutroData : IOutroData
{
	// code offsets of the address operands (lea abs.l or move.l #imm)
	const int ThroneRoomOffset = 0x1a8;
	const int ThroneRoomPaletteOffset = 0x1f8;
	const int BalconyOffset = 0x274;
	const int SkyPaletteOffset = 0x2c8;
	const int LandscapePaletteOffset = 0x2da;
	const int RedMoonPaletteOffset = 0x5a4;
	const int MoonOffset = 0x5f8;
	const int EaglesOffset = 0x670;
	const int DragonYOffsetsOffset = 0x6be;
	const int DragonOffset = 0x6ca;
	const int FlyerYOffsetsOffset = 0x71c;
	const int FlyerOffset = 0x728;
	const int BalconyPeopleRectsOffset = 0x76a;
	static readonly int[] BalconyPeopleOffsets = [0x792, 0x7b2, 0x7d2];
	const int ChancellorTextsOffset = 0x7ea;
	const int BalconyTextsOffset = 0x4a0;
	const int BalconyScriptOffset = 0x130;
	const int LeftTorchOffset = 0x982;
	const int RightTorchOffset = 0x99a;
	const int ChancellorOffset = 0x9be;
	const int PartyFigurePositionsOffset = 0x9d8;
	const int PartyFigureYOffsetsOffset = 0x9ee;
	const int PartyFiguresOffset = 0xa00;
	const int FontMapOffset = 0x11f6;
	const int GlyphsOffset = 0x1214;
	const int MusicOffset = 0x226;
	const int PictureSize = IOutroData.PictureWidth * IOutroData.PictureHeight / 2;
	const byte FontColor = 15;
	// the special characters of the texts
	static readonly Dictionary<byte, char> SpecialCharacters = new()
	{
		{ 0x8e, 'Ä' }, { 0x99, 'Ö' }, { 0x9a, 'Ü' }, { 0x9e, 'ß' }, { 0x84, 'ä' }, { 0x94, 'ö' }, { 0x81, 'ü' }
	};

	readonly byte[] data;
	readonly Dictionary<char, IGraphic?> glyphs = [];

	public OutroData(byte[] data, LegacyPlatform platform)
	{
		this.data = data;

		ThroneRoom = Picture(ThroneRoomOffset);
		Balcony = Picture(BalconyOffset);
		var throneRoomPalette = Address(ThroneRoomPaletteOffset);
		ThroneRoomPalette = Palette(throneRoomPalette, null);
		ThroneRoomLowerPalette = Palette(throneRoomPalette, Word(throneRoomPalette + 32));
		var landscapePalette = Address(LandscapePaletteOffset);
		LandscapePalette = Palette(landscapePalette, null);
		LandscapeLowerPalette = Palette(landscapePalette, Word(landscapePalette + 32));
		SkyPalette = Palette(Address(SkyPaletteOffset), null);
		RedMoonPalette = Palette(Address(RedMoonPaletteOffset), null);

		LeftTorch = Frames(Address(LeftTorchOffset), 3, 1, 46);
		RightTorch = Frames(Address(RightTorchOffset), 3, 1, 46);
		Chancellor = Frames(Address(ChancellorOffset), 7, 2, 37);
		Eagles = Frames(Address(EaglesOffset), 4, 1, 14);
		Moon = Sprite(Address(MoonOffset), 9, 32);

		// The party figures have word offsets, the dragons long offsets to their frames (with width and height).
		int partyFigures = Address(PartyFiguresOffset);
		PartyFigures = [.. Enumerable.Range(0, 3).Select(i => SpriteWithSize(partyFigures + Word(partyFigures + i * 2)))];
		PartyFigureYOffsets = Words(Address(PartyFigureYOffsetsOffset), 3);
		var partyPositions = Words(Address(PartyFigurePositionsOffset), 12);
		PartyFigurePositions = [.. Enumerable.Range(0, 6).Select(i => new Position(partyPositions[i * 2], partyPositions[i * 2 + 1]))];
		int dragon = Address(DragonOffset);
		Dragon = [.. Enumerable.Range(0, 12).Select(i => SpriteWithSize(dragon + Long(dragon + i * 4)))];
		DragonYOffsets = Words(Address(DragonYOffsetsOffset), 12);
		int flyer = Address(FlyerOffset);
		Flyer = [.. Enumerable.Range(0, 8).Select(i => SpriteWithSize(flyer + Long(flyer + i * 4)))];
		FlyerYOffsets = Words(Address(FlyerYOffsetsOffset), 8);

		// The rectangles which restore the background behind the people give their positions and sizes.
		int rects = Address(BalconyPeopleRectsOffset);
		var people = new List<IReadOnlyList<IGraphic>>();
		var peoplePositions = new List<Position>();

		for (int i = 0; i < 3; i++)
		{
			var rect = Words(rects + i * 8, 4);
			int width = rect[2] + 1;
			int height = rect[3];
			peoplePositions.Add(new(rect[0], rect[1]));
			people.Add(Frames(Address(BalconyPeopleOffsets[i]), 4, width, height));
		}

		BalconyPeople = people;
		BalconyPeoplePositions = peoplePositions;

		int script = Address(BalconyScriptOffset);
		int scriptEnd = script;

		// commands with a parameter until 0xff
		while (data[scriptEnd] != 0xff)
			scriptEnd += data[scriptEnd] is 4 or 5 ? 1 : 2;

		BalconyScript = data[script..(scriptEnd + 1)];
		ChancellorTexts = Texts(Address(ChancellorTextsOffset));
		BalconyTexts = Texts(Address(BalconyTextsOffset));

		Music = LoadMusic(Address(MusicOffset), platform);
	}

	public IGraphic ThroneRoom { get; }
	public IGraphic ThroneRoomPalette { get; }
	public IGraphic ThroneRoomLowerPalette { get; }
	public IGraphic Balcony { get; }
	public IGraphic SkyPalette { get; }
	public IGraphic RedMoonPalette { get; }
	public IGraphic LandscapePalette { get; }
	public IGraphic LandscapeLowerPalette { get; }
	public IReadOnlyList<IGraphic> LeftTorch { get; }
	public IReadOnlyList<IGraphic> RightTorch { get; }
	public IReadOnlyList<IGraphic> Chancellor { get; }
	public IReadOnlyList<IGraphic> PartyFigures { get; }
	public IReadOnlyList<int> PartyFigureYOffsets { get; }
	public IReadOnlyList<Position> PartyFigurePositions { get; }
	public IReadOnlyList<IGraphic> Eagles { get; }
	public IReadOnlyList<IGraphic> Dragon { get; }
	public IReadOnlyList<int> DragonYOffsets { get; }
	public IReadOnlyList<IGraphic> Flyer { get; }
	public IReadOnlyList<int> FlyerYOffsets { get; }
	public IGraphic Moon { get; }
	public IReadOnlyList<IReadOnlyList<IGraphic>> BalconyPeople { get; }
	public IReadOnlyList<Position> BalconyPeoplePositions { get; }
	public IReadOnlyList<byte> BalconyScript { get; }
	public IReadOnlyList<string> ChancellorTexts { get; }
	public IReadOnlyList<string> BalconyTexts { get; }
	public ISong? Music { get; }

	int Word(int offset) => (data[offset] << 8) | data[offset + 1];

	int SignedWord(int offset) => (short)Word(offset);

	int Long(int offset) => (Word(offset) << 16) | Word(offset + 2);

	int Address(int codeOffset) => Long(codeOffset);

	int[] Words(int offset, int count) => [.. Enumerable.Range(0, count).Select(i => SignedWord(offset + i * 2))];

	IGraphic Picture(int codeOffset)
	{
		int offset = Address(codeOffset);

		return Graphic.FromBitPlanes(IOutroData.PictureWidth, IOutroData.PictureHeight, data[offset..(offset + PictureSize)], 4);
	}

	/// <summary>
	/// The palettes have 16 colors. Some have a 17th color which replaces color 5 below a line.
	/// </summary>
	IGraphic Palette(int offset, int? color5)
	{
		var palette = data[offset..(offset + 32)];

		if (color5 != null)
		{
			palette[10] = (byte)(color5 >> 8);
			palette[11] = (byte)color5;
		}

		return PaletteLoader.LoadPalette(new DataReader(palette));
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
			int glyphIndex = (sbyte)data[Address(FontMapOffset) + index];

			if (glyphIndex >= 0)
			{
				int offset = Address(GlyphsOffset) + glyphIndex * IOutroData.GlyphHeight;
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
	/// The music player contains a COSO module. It is null if it can't be loaded.
	/// </summary>
	ISong? LoadMusic(int playerOffset, LegacyPlatform platform)
	{
		int offset = playerOffset;

		while (offset + 0x20 <= data.Length && !(data[offset] == 'C' && data[offset + 1] == 'O' && data[offset + 2] == 'S' && data[offset + 3] == 'O'))
			offset += 2;

		if (offset + 0x20 > data.Length)
			return null;

		// TODO: The module is a newer TFMX variant (with samples and timbre transposes in the divisions)
		// which the player doesn't support yet.
		try
		{
			var songs = HippelCosoLoader.Load(new DataReader(data[offset..]), platform);

			return songs.Count == 0 ? null : songs[0];
		}
		catch
		{
			return null;
		}
	}
}
