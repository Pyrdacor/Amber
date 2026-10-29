using Amber.Assets.Common;
using Amber.Serialization;

namespace Amberstar.GameData.Legacy;

/// <summary>
/// The intro program INTRO.68K (unpacked). The code is the same in all versions, only the addresses of the
/// variables differ. The data addresses are read from the instructions at fixed code offsets.
/// </summary>
internal class IntroData : IIntroData
{
	// code offsets of the address operands (lea abs.l or move.l #imm)
	const int TitlePictureOffset = 0xe8;
	const int BackgroundOffset = 0x188;
	const int BackgroundPaletteOffset = 0x19e;
	const int LogoOffset = 0x206;
	const int TextsOffset = 0x2fa;
	const int FontOffset = 0x822;
	const int MusicPlayerOffset = 0x1e4;
	const int PictureSize = IIntroData.PictureWidth * IIntroData.PictureHeight / 2;
	// the title picture: 199 lines with 3 palettes of 16 colors (the first line is black)
	const int TitlePaletteLines = 199;
	const int ColorsPerLine = 48;
	const int LogoHeight = 36;
	// the font (16x20 pixels, from character 0x25), the text starts at (32,75) and the lines have 22 pixels
	const int GlyphWidth = 16;
	const int GlyphHeight = 20;
	const int FirstGlyphCharacter = 0x25;
	const int TextX = 32;
	const int LineHeight = 22;
	// the special characters of the texts
	static readonly Dictionary<byte, char> SpecialCharacters = new()
	{
		{ 0x8e, 'Ä' }, { 0x99, 'Ö' }, { 0x9a, 'Ü' }, { 0x9e, 'ß' }, { 0x84, 'ä' }, { 0x94, 'ö' }, { 0x81, 'ü' }
	};

	readonly byte[] data;
	readonly int[] backgroundPalette;

	public IntroData(byte[] data)
	{
		this.data = data;

		int title = Address(TitlePictureOffset);
		TitlePictures = LoadTitlePictures(title);
		var background = Graphic.FromBitPlanes(IIntroData.PictureWidth, IIntroData.PictureHeight,
			data[Address(BackgroundOffset)..(Address(BackgroundOffset) + PictureSize)], 4);
		Background = background;
		backgroundPalette = [.. Enumerable.Range(0, 16).Select(i => Word(Address(BackgroundPaletteOffset) + i * 2))];
		int logo = Address(LogoOffset);
		Logo = Graphic.FromBitPlanes(IIntroData.PictureWidth, LogoHeight, data[logo..(logo + IIntroData.PictureWidth / 2 * LogoHeight)], 4);

		// The pages end with 0, the list ends with an empty page.
		var pages = new List<byte[]>();
		int offset = Address(TextsOffset);

		while (data[offset] != 0)
		{
			int end = Array.IndexOf(data, (byte)0, offset);
			pages.Add(data[offset..end]);
			offset = end + 1;
		}

		Texts = [.. pages.Select(page => new string([.. page.Select(ToChar)]))];
		TextPages = [.. pages.Select(page => TextPage(background, page))];
		Music = LoadMusic(Address(MusicPlayerOffset));
	}

	public IReadOnlyList<IGraphic> TitlePictures { get; }
	public IGraphic Background { get; }
	public IGraphic Logo { get; }
	public IReadOnlyList<string> Texts { get; }
	public IReadOnlyList<IGraphic> TextPages { get; }
	public ISong? Music { get; }

	int Word(int offset) => (data[offset] << 8) | data[offset + 1];

	int Address(int codeOffset) => (Word(codeOffset) << 16) | Word(codeOffset + 2);

	static char ToChar(byte value) => value == 0x0d ? '\n' : SpecialCharacters.TryGetValue(value, out var ch) ? ch : (char)value;

	/// <summary>
	/// The colors have 3 bits per channel (like the palettes of the game).
	/// </summary>
	static (byte R, byte G, byte B) ToRgb(int color)
	{
		static byte Channel(int value) => (byte)(((value & 0x7) << 1) * 17);

		return (Channel(color >> 8), Channel(color >> 4), Channel(color));
	}

	/// <summary>
	/// The palettes of the lines of the title picture follow the picture. The VBL routine writes the 16 colors
	/// 3 times while a line is drawn (like Spectrum 512), so the color of a pixel also depends on its x position.
	/// The fading steps decrease each channel by 1 per step (L492 in the intro).
	/// </summary>
	List<IGraphic> LoadTitlePictures(int offset)
	{
		var pixels = Graphic.FromBitPlanes(IIntroData.PictureWidth, IIntroData.PictureHeight, data[offset..(offset + PictureSize)], 4).GetData();
		int palettes = offset + PictureSize;
		var pictures = new List<IGraphic>();

		for (int step = 0; step < IIntroData.TitleFadeSteps; step++)
		{
			int darker = IIntroData.TitleFadeSteps - 1 - step;
			var rgba = new byte[IIntroData.PictureWidth * IIntroData.PictureHeight * 4];

			for (int y = 0; y < IIntroData.PictureHeight; y++)
			{
				for (int x = 0; x < IIntroData.PictureWidth; x++)
				{
					int target = (y * IIntroData.PictureWidth + x) * 4;
					rgba[target + 3] = 0xff;

					// the first line is black
					if (y == 0)
						continue;

					int color = pixels[y * IIntroData.PictureWidth + x];
					int value = Word(palettes + ((y - 1) * ColorsPerLine + SpectrumColorIndex(x, color)) * 2);
					int faded = 0;

					for (int shift = 0; shift <= 8; shift += 4)
						faded |= Math.Max(0, ((value >> shift) & 0x7) - darker) << shift;

					(rgba[target], rgba[target + 1], rgba[target + 2]) = ToRgb(faded);
				}
			}

			pictures.Add(Graphic.FromRGBA(IIntroData.PictureWidth, IIntroData.PictureHeight, rgba));
		}

		return pictures;
	}

	/// <summary>
	/// Which of the 48 colors of a line is shown at a position (the palette is written while the line is drawn).
	/// </summary>
	static int SpectrumColorIndex(int x, int color)
	{
		int x1 = 10 * color + ((color & 1) != 0 ? -5 : 1);

		if (x >= x1 + 160)
			return color + 32;

		return x >= x1 ? color + 16 : color;
	}

	public IGraphic GetBackgroundPalette(int fadeStep = 0)
	{
		var bytes = new byte[32];

		for (int i = 0; i < 16; i++)
		{
			int color = 0;

			for (int shift = 0; shift <= 8; shift += 4)
			{
				int channel = (backgroundPalette[i] >> shift) & 0x7;

				if (fadeStep > 0)
					channel = Math.Min(channel, fadeStep);
				else if (fadeStep < 0)
					channel = Math.Max(0, channel + fadeStep);

				color |= channel << shift;
			}

			bytes[i * 2] = (byte)(color >> 8);
			bytes[i * 2 + 1] = (byte)color;
		}

		return PaletteLoader.LoadPalette(new DataReader(bytes));
	}

	/// <summary>
	/// The text is drawn into the background with the font (masked, $7f6 in the intro). A page shows the
	/// part of the background below the logo.
	/// </summary>
	IGraphic TextPage(IGraphic background, byte[] text)
	{
		var pixels = background.GetData()[(IIntroData.TextAreaY * IIntroData.PictureWidth)..((IIntroData.TextAreaY + IIntroData.TextAreaHeight) * IIntroData.PictureWidth)];
		int font = Address(FontOffset);
		int x = TextX;
		int y = 0;

		foreach (byte ch in text)
		{
			if (ch == 0x0d)
			{
				x = TextX;
				y += LineHeight;
				continue;
			}

			int glyphOffset = font + Math.Max(0, ch - FirstGlyphCharacter) * GlyphWidth / 2 * GlyphHeight;
			var glyph = Graphic.FromBitPlanes(GlyphWidth, GlyphHeight, data[glyphOffset..(glyphOffset + GlyphWidth / 2 * GlyphHeight)], 4).GetData();

			for (int gy = 0; gy < GlyphHeight && y + gy < IIntroData.TextAreaHeight; gy++)
			{
				for (int gx = 0; gx < GlyphWidth && x + gx < IIntroData.PictureWidth; gx++)
				{
					byte color = glyph[gy * GlyphWidth + gx];

					if (color != 0)
						pixels[(y + gy) * IIntroData.PictureWidth + x + gx] = color;
				}
			}

			x += GlyphWidth;
		}

		return new Graphic(IIntroData.PictureWidth, IIntroData.TextAreaHeight, pixels, GraphicFormat.PaletteIndices);
	}

	/// <summary>
	/// The music player emulates the 4 sample voices of the Amiga like the one of the outro and plays a COSO module
	/// with samples which follows the player code.
	/// </summary>
	ISong? LoadMusic(int playerOffset)
	{
		int offset = playerOffset;

		while (offset + 0x20 <= data.Length && !data.AsSpan(offset, 4).SequenceEqual("COSO"u8))
			offset += 2;

		if (offset + 0x20 > data.Length)
			return null;

		return new HippelPaulaSong(data[offset..]);
	}
}
