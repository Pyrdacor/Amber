using Amber.Common;

namespace Amber.Assets.Common;

public class Graphic : IGraphic
{
	readonly byte[] data = [];

	public Graphic()
	{

	}

	public Graphic(int width, int height, bool usePalette)
	{
		Width = width;
		Height = height;
		Format = usePalette
		    ? GraphicFormat.PaletteIndices
			: GraphicFormat.RGBA;
		UsePalette = usePalette;
		BytesPerPixel = Format.BytesPerPixel();
		this.pixelData = new byte[width * height * BytesPerPixel];		
	}

	public Graphic(int width, int height, byte[] data, bool usePalette)
	{
		Width = width;
		Height = height;
		Format = usePalette
		    ? GraphicFormat.PaletteIndices
			: GraphicFormat.RGBA;
		UsePalette = usePalette;
		BytesPerPixel = Format.BytesPerPixel();
		this.data = data;
	}

	public static Graphic FromBitPlanes(int width, int height, byte[] data, int planes, int frameCount = 1)
	{
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

		return new Graphic(frameCount * width, height, pixelData, true);
	}

	public int Width { get; }

	public int Height { get; }

	public GraphicFormat Format { get; }

	public bool UsesPalette { get; }

	public int BytesPerPixel { get; }

	public byte[] GetData() => data;

	public Graphic GetPart(int x, int y, int width, int height)
	{
		if (x < 0 || x + width > Width || y < 0 || y + height > Height)
			throw new AmberException(ExceptionScope.Application, "Part is out of bounds.");

		if (x == 0 && y == 0 && width == Width && height == Height)
			return new Graphic(Width, Height, (byte[])pixelData.Clone(), UsePalette);

		byte[] partPixelData = new byte[width * height * BytesPerPixel];
		int sourceRowSize = Width * BytesPerPixel;
		int targetRowSize = width * BytesPerPixel;
		int sourceIndex = y * sourceRowSize + x * BytesPerPixel;
		int targetIndex = 0;

		for (int i = 0; i < height; i++)
		{
			Buffer.BlockCopy(pixelData, sourceIndex, partPixelData, targetIndex, targetRowSize);
			sourceIndex += sourceRowSize;
			targetIndex += targetRowSize;
		}

		return new Graphic(width, height, partPixelData, UsePalette);
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
		if (UsePalette != overlay.Format.UsePalette())
			throw new AmberException(ExceptionScope.Application, "Cannot overlay graphics with different palette usage.");

		if (x < 0 || x + overlay.Width > Width || y < 0 || y + overlay.Height > Height)
			throw new AmberException(ExceptionScope.Application, "Overlay is out of bounds.");

		var overlayPixelData = overlay.GetData();
		Func<int, bool> IsTransparent = UsePalette
			? (int index) => overlayPixelData[index] == 0
			: (int index) => overlayPixelData[index + 3] == 0;
		int sourceRowSize = overlay.Width * BytesPerPixel;
		int targetRowSize = Width * BytesPerPixel;
		int sourceIndex = 0;
		int targetIndex = y * targetRowSize + x * BytesPerPixel;
		Action<int> CopyPixel = UsePalette
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
		if (!UsePalette)
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
