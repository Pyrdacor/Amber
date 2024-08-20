using Amber.Assets.Common;
using Amber.Common;
using Amber.Serialization;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy;

internal class PaletteLoader(Amber.Assets.Common.IAssetProvider assetProvider, IGraphic uiPalette) : IPaletteLoader
{
	private readonly Dictionary<int, IGraphic> palettes = [];

	// Atari ST only had 3 bit per color channel (0-7).
	// So we have to map it to normal colors. 0 should map to 0 and 7 to 255.
	// A good approach is to multiply the value by 32 and add 16.
	// Also palettes store alpha as 0 while the color is still opaque.
	// Amiga seems to also use the same palette format.

	public static IGraphic LoadWidePalette(IDataReader dataReader)
	{
		int size = dataReader.ReadWord();
		var data = dataReader.ReadBytes(size * 4);
		int index = 0;

		byte ConvertColorComponent(byte c)
		{
			c *= 32;

			if (c == 224)
				c = 255;
			else if (c != 0)
				c += 16;

			return c;
		}

		for (int i = 0; i < size; i++)
		{
			data[index] = ConvertColorComponent(data[++index]); // R
			data[index] = ConvertColorComponent(data[++index]); // G
			data[index] = ConvertColorComponent(data[++index]); // B
			data[index++] = 0xff; // A
		}

		return Graphic.FromRGBA(size, 1, data);
	}

	public static IGraphic LoadPalette(IDataReader dataReader)
	{
		var data = dataReader.ReadBytes(16 * 2);

		// For compact palettes each color component is stored in a 4-bit nibble.
		// But still only 3 bits are used on the Atari. So we need to map it.
		// We achieve this by multiplying by 2 or left shifting by 1.
		// The XR nibble can be directly shifted. The GB nibble needs special care.
		for (int i = 0; i < 16; i++)
		{
			var r = data[i * 2] & 0x7;
			r <<= 1;
			if (r == 14)
				r = 15;
			data[i * 2] = (byte)r;
			var gb = data[i * 2 + 1] & 0x77;
			var g = gb >> 4;
			var b = gb & 0xf;
			g <<= 1;
			if (g == 14)
				g = 15;
			b <<= 1;
			if (b == 14)
				b = 15;
			data[i * 2 + 1] = (byte)((g << 4) | b);
		}

		// Note: Graphic.FromPalette will handle alpha so we don't need to care about it here.
		return Graphic.FromPalette(data);
	}

	public IGraphic LoadPalette(int index)
	{
		if (palettes.TryGetValue(index, out var palette))
			return palette;

		var asset = assetProvider.GetAsset(new AssetIdentifier(AssetType.Palette, index));

		if (asset == null)
			throw new AmberException(ExceptionScope.Data, $"Palette {index} not found.");

		palette = LoadWidePalette(asset.GetReader());

		palettes.Add(index, palette);

		return palette;
	}

	public IGraphic LoadUIPalette() => uiPalette;
}
