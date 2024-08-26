using Amber.Common;

namespace Amber.Assets.Common;

public class Graphic : IGraphic
{
	readonly byte[] data = [];

	public Graphic()
	{

	}

	public Graphic(int width, int height, GraphicFormat graphicFormat)
	{
		Width = width;
		Height = height;
		Format = graphicFormat;
		UsesPalette = graphicFormat.UsesPalette();
		BytesPerPixel = Format.BytesPerPixel();
		this.data = new byte[width * height * BytesPerPixel];
	}

	public Graphic(int width, int height, byte[] data, GraphicFormat graphicFormat)
	{
		Width = width;
		Height = height;
		Format = graphicFormat;
		UsesPalette = graphicFormat.UsesPalette();
		BytesPerPixel = Format.BytesPerPixel();

		if (data.Length != width * height * BytesPerPixel)
			throw new AmberException(ExceptionScope.Data, "Unexpected graphic data size.");

		this.data = data;
	}

	private protected static byte[] ReadBitPlanes(int width, int height, byte[] data, int planes, int frameCount = 1)
	{
		if (width <= 8 && planes == 1 && frameCount == 1) // font
		{
			byte[] glyphlData = new byte[width * height];

			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					int mask = 1 << (7 - x);

					if ((data[y] & mask) != 0)
						glyphlData[y * width + x] = 1;
				}
			}

			return glyphlData;
		}

		if (planes < 1 || planes > 8)
			throw new AmberException(ExceptionScope.Application, "Bit planes must be between 1 and 8.");

		if (data.Length != frameCount * width * height * planes / 8)
			throw new AmberException(ExceptionScope.Application, $"Invalid data length for {planes}-bit graphic.");

		byte[] pixelData = new byte[frameCount * width * height];
		int wordsPerLine = (width + 15) / 16;
		int index = 0;
		int targetIndex = 0;
		int[] plane = new int[planes];

		for (int y = 0; y < frameCount * height; y++)
		{
			for (int w = 0; w < wordsPerLine; w++)
			{
				for (int p = 0; p < planes; p++)
					plane[p] = (data[index++] << 8) | data[index++];

				for (int x = 0; x < 16; x++)
				{
					int pixel = 0;
					int mask = 1 << (15 - x);

					for (int p = 0; p < planes; p++)
					{
						if ((plane[p] & mask) != 0)
							pixel |= 1 << p;
					}

					pixelData[targetIndex++] = (byte)pixel;
				}
			}
		}

		return pixelData;
	}

	public static Graphic FromBitPlanes(int width, int height, byte[] data, int planes, int frameCount = 1)
	{
		var pixelData = ReadBitPlanes(width, height, data, planes, frameCount);

		return new Graphic(frameCount * width, height, pixelData, GraphicFormat.PaletteIndices);
	}

	public static Graphic FromAlpha(int width, int height, byte[] data)
	{
		return new Graphic(width, height, data, GraphicFormat.Alpha);
	}

	public static Graphic FromRGBA(int width, int height, byte[] data)
	{
		return new Graphic(width, height, data, GraphicFormat.RGBA);
	}

	/// <summary>
	/// Note: This expects 16 bit colors in XR GB format where each
	/// color component uses the full 4-bit nibble. X is ignored and
	/// all colors will be treated as fully opaque (alpha = 100%).
	/// </summary>
	public static Graphic FromPalette(byte[] data)
	{
		var pixelData = new byte[data.Length * 2];
		int sourceIndex = 0;
		int targetIndex = 0;

		for (int i = 0; i < data.Length / 2; i++)
		{
			int r = data[sourceIndex++] & 0x0f;
			int gb = data[sourceIndex++];
			int g = gb >> 4;
			int b = gb & 0xf;

			pixelData[targetIndex++] = (byte)(r | (r << 4));
			pixelData[targetIndex++] = (byte)(g | (g << 4));
			pixelData[targetIndex++] = (byte)(b | (b << 4));
			pixelData[targetIndex++] = 0xff; // a
		}

		return new Graphic(data.Length / 2, 1, pixelData, GraphicFormat.RGBA);
	}

	public int Width { get; }

	public int Height { get; }

	public GraphicFormat Format { get; }

	public bool UsesPalette { get; }

	public int BytesPerPixel { get; }

	public byte[] GetData() => data;

	public Color GetColorAt(int x, int y)
	{
		if (x < 0 || y < 0 || x >= Width || y >= Height)
			return Color.Transparent;

		switch (Format)
		{
			case GraphicFormat.Alpha:
			{
				byte alpha = data[y * Width + x];
				return new Color(alpha, alpha, alpha, alpha);
			}
			case GraphicFormat.RGBA:
			{
				int index = (y * Width + x) * 4;
				return new Color(data[index++], data[index++], data[index++], data[index++]);
			}
			default:
				throw new NotSupportedException($"Color format {Format} does not supporting getting a color at a position.");
		}
	}

	public Graphic GetPart(int x, int y, int width, int height)
	{
		if (x < 0 || x + width > Width || y < 0 || y + height > Height)
			throw new AmberException(ExceptionScope.Application, "Part is out of bounds.");

		if (x == 0 && y == 0 && width == Width && height == Height)
			return new Graphic(Width, Height, (byte[])data.Clone(), Format);

		byte[] partPixelData = new byte[width * height * BytesPerPixel];
		int sourceRowSize = Width * BytesPerPixel;
		int targetRowSize = width * BytesPerPixel;
		int sourceIndex = y * sourceRowSize + x * BytesPerPixel;
		int targetIndex = 0;

		for (int i = 0; i < height; i++)
		{
			Buffer.BlockCopy(data, sourceIndex, partPixelData, targetIndex, targetRowSize);
			sourceIndex += sourceRowSize;
			targetIndex += targetRowSize;
		}

		return new Graphic(width, height, partPixelData, Format);
	}	

	/// <summary>
	/// Adds an overlay to the graphic.
	/// 
	/// Note that only palette images can be overlayed on palette images
	/// and non-palette images on non-palette images.
	/// </summary>
	/// <param name="x">X position at which to place the overlay</param>
	/// <param name="y">X position at which to place the overlay</param>
	/// <param name="overlay">The overlay</param>
	/// <param name="blend">If true, the alpha of the overlay is respected (for palette images, index 0 is treated as fully transparent). If false, transparent pixels will be placed in the target graphic.</param>
	public void AddOverlay(int x, int y, IGraphic overlay, bool blend = false)
	{
		if (UsesPalette != overlay.Format.UsesPalette())
			throw new AmberException(ExceptionScope.Application, "Cannot overlay graphics with different palette usage.");

		if (x < 0 || x + overlay.Width > Width || y < 0 || y + overlay.Height > Height)
			throw new AmberException(ExceptionScope.Application, "Overlay is out of bounds.");

		var overlayPixelData = overlay.GetData();
		Func<int, bool> IsTransparent = UsesPalette
			? (int index) => overlayPixelData[index] == 0
			: (int index) => overlayPixelData[index + 3] == 0;
		int sourceRowSize = overlay.Width * BytesPerPixel;
		int targetRowSize = Width * BytesPerPixel;
		int sourceIndex = 0;
		int targetIndex = y * targetRowSize + x * BytesPerPixel;
		Action<int> CopyPixel = UsesPalette
			? (int sourceIndex) => data[targetIndex++] = overlayPixelData[sourceIndex]
			: (int sourceIndex) => { Buffer.BlockCopy(overlayPixelData, sourceIndex, data, targetIndex, BytesPerPixel); targetIndex += BytesPerPixel; };

		for (int i = 0; i < overlay.Height; i++)
		{
			if (!blend)
			{
				Buffer.BlockCopy(overlayPixelData, sourceIndex, data, targetIndex, sourceRowSize);
				targetIndex += targetRowSize;
			}
			else
			{
				int nextTargetIndex = targetIndex + targetRowSize;

				for (int j = 0; j < overlay.Width; j++)
				{
					int index = sourceIndex + j * BytesPerPixel;

					if (!IsTransparent(index))
						CopyPixel(index);
					else
						targetIndex += BytesPerPixel;
				}

				targetIndex = nextTargetIndex;
			}

			sourceIndex += sourceRowSize;
		}
	}

	public void ApplyBitMaskedPlanarValues(int x, int y, word[] masks, word[] values, int planes)
	{
		if (!UsesPalette)
			throw new AmberException(ExceptionScope.Application, "Bit masks can only be applied to palette graphics.");

		if (x < 0 || x + 16 > Width || y < 0 || y >= Height)
			throw new AmberException(ExceptionScope.Application, "Mask position is out of bounds.");

		var pixels = new ReadOnlySpan<byte>(data, y * Width + x, 16);
		word[] results = new word[planes];

		for (int p = 0; p < planes; p++)
		{
			word plane = 0;
			int checkMask = 1 << p;

			for (int i = 0; i < 16; i++)
			{
				if ((pixels[i] & checkMask) != 0)
					plane |= (word)(1 << (15 - i));
			}

			plane &= (word)~masks[p];
			plane |= values[p];
			results[p] = plane;
		}

		for (int i = 0; i < 16; i++)
		{
			byte pixelValue = 0;

			for (int p = 0; p < planes; p++)
			{
				if ((results[p] & (word)(1 << (15 - i))) != 0)
					pixelValue |= (byte)(1 << p);
			}

			data[y * Width + x + i] = pixelValue;
		}
	}
}

public class PaletteGraphic : Graphic, IPaletteGraphic
{
	public PaletteGraphic(int width, int height, IGraphic palette)
		: base(width, height, GraphicFormat.PaletteIndices)
	{
		Palette = palette;
	}

	public PaletteGraphic(int width, int height, byte[] data, IGraphic palette)
		: base(width, height, data, GraphicFormat.PaletteIndices)
	{
		Palette = palette;
	}

	public static PaletteGraphic FromBitPlanes(int width, int height, byte[] data, int planes, IGraphic palette, int frameCount = 1)
	{
		var pixelData = ReadBitPlanes(width, height, data, planes, frameCount);

		return new PaletteGraphic(frameCount * width, height, pixelData, palette);
	}


	public IGraphic Palette { get; }
}

public static class GraphicExtensions
{
	public static Graphic ToGraphic(this IEnumerable<IGraphic> frames)
	{
		int width = frames.Sum(frame => frame.Width);
		int height = frames.Max(frame => frame.Height);
		var format = frames.First().Format;

		if (frames.Any(frame => frame.Format != format))
			throw new AmberException(ExceptionScope.Data, "Frames have different format.");

		var graphic = new Graphic(width, height, format);
		int x = 0;

		foreach (var frame in frames)
		{
			graphic.AddOverlay(x, 0, frame);
			x += frame.Width;
		}

		return graphic;
	}
}