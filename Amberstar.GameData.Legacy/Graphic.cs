using Amber.Assets.Common;
using Amber.Common;

namespace Amberstar.GameData.Legacy;

public class Graphic : IGraphic
{
	readonly byte[] pixelData = [];

	public Graphic()
	{

	}

	public Graphic(int width, int height, bool usePalette)
	{
		Width = width;
		Height = height;
		this.pixelData = new byte[width * height * BytesPerPixel];
		UsePalette = usePalette;
	}

	public Graphic(int width, int height, byte[] pixelData, bool usePalette)
	{
		Width = width;
		Height = height;
		this.pixelData = pixelData;
		UsePalette = usePalette;
	}

	public static Graphic From4BitPlanes(int width, int height, byte[] data)
	{
		if (data.Length != width * height / 2)
			throw new AmberException(ExceptionScope.Application, "Invalid data length for 4-bit graphic.");

		byte[] pixelData = new byte[width * height];
		int wordsPerLine = (width + 15) / 16;
		int index = 0;
		int targetIndex = 0;

		for (int y = 0; y < height; y++)
		{
			for (int w = 0; w < wordsPerLine; w++)
			{
				int plane0 = (data[index++] << 8) | data[index++];
				int plane1 = (data[index++] << 8) | data[index++];
				int plane2 = (data[index++] << 8) | data[index++];
				int plane3 = (data[index++] << 8) | data[index++];

				for (int x = 0; x < 16; x++)
				{
					int pixel = 0;
					int mask = 1 << (15 - x);

					if ((plane0 & mask) != 0)
						pixel |= 0x1;

					if ((plane1 & mask) != 0)
						pixel |= 0x2;

					if ((plane2 & mask) != 0)
						pixel |= 0x4;

					if ((plane3 & mask) != 0)
						pixel |= 0x8;

					pixelData[targetIndex++] = (byte)pixel;
				}
			}
		}

		return new Graphic(width, height, pixelData, true);
	}

	public int Width { get; private init; } = 0;

	public int Height { get; private init; } = 0;

	public bool UsePalette { get; private init; } = false;

	public int BytesPerPixel => UsePalette ? 1 : 4;

	public byte[] GetPixelData()
	{
		return pixelData;
	}

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
		if (UsePalette != overlay.UsePalette)
			throw new AmberException(ExceptionScope.Application, "Cannot overlay graphics with different palette usage.");

		if (x < 0 || x + overlay.Width > Width || y < 0 || y + overlay.Height > Height)
			throw new AmberException(ExceptionScope.Application, "Overlay is out of bounds.");

		var overlayPixelData = overlay.GetPixelData();
		Func<int, bool> IsTransparent = UsePalette
			? (int index) => overlayPixelData[index] == 0
			: (int index) => overlayPixelData[index + 3] == 0;
		int sourceRowSize = overlay.Width * BytesPerPixel;
		int targetRowSize = Width * BytesPerPixel;
		int sourceIndex = 0;
		int targetIndex = y * targetRowSize + x * BytesPerPixel;
		Action<int> CopyPixel = UsePalette
			? (int sourceIndex) => pixelData[targetIndex++] = overlayPixelData[sourceIndex]
			: (int sourceIndex) => { Buffer.BlockCopy(overlayPixelData, sourceIndex, pixelData, targetIndex, BytesPerPixel); targetIndex += BytesPerPixel; };

		for (int i = 0; i < overlay.Height; i++)
		{
			if (!blend)
			{
				Buffer.BlockCopy(overlayPixelData, sourceIndex, pixelData, targetIndex, sourceRowSize);
				targetIndex += targetRowSize;
			}
			else
			{
				for (int j = 0; j < overlay.Width; j++)
				{
					int index = sourceIndex + j * BytesPerPixel;

					if (!IsTransparent(index))
						CopyPixel(index);
					else
						targetIndex += BytesPerPixel;
				}
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

		var pixels = new ReadOnlySpan<byte>(pixelData, y * Width + x, 16);
		word[] results = new word[planes];

		for (int p = 0; p < planes; p++)
		{
			word pixelValue = 0;
			int checkMask = 1 << p;

			for (int i = 0; i < 16; i++)
			{
				if ((pixels[i] & checkMask) != 0)
					pixelValue |= (word)(1 << (15 - i));
			}

			pixelValue &= masks[p];
			pixelValue |= values[p];
			results[p] = pixelValue;
		}

		for (int i = 0; i < 16; i++)
		{
			byte pixelValue = 0;

			for (int p = 0; p < planes; p++)
			{
				if ((results[p] & (word)(1 << (15 - i))) != 0)
					pixelValue |= (byte)(1 << p);
			}

			pixelData[y * Width + x + i] = pixelValue;
		}
	}
}
