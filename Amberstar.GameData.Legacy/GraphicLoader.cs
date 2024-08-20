using Amber.Assets.Common;
using Amber.Common;
using Amber.Serialization;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy;

internal class GraphicLoader(Amber.Assets.Common.IAssetProvider assetProvider) : IGraphicLoader
{
	private readonly Dictionary<Image80x80, IPaletteGraphic> graphics80x80 = [];
	private readonly Dictionary<ItemGraphic, IGraphic> itemGraphics = [];

	public static byte[] LoadGraphicDataWithHeader(IDataReader dataReader, out int width, out int height, out int planes)
	{
		width = dataReader.ReadWord() + 1;
		height = dataReader.ReadWord() + 1;
		planes = dataReader.ReadWord();

		if (planes != 4)
			throw new AmberException(ExceptionScope.Data, "Unexpected plane count for legacy graphic data.");

		return dataReader.ReadBytes(width * height * planes / 8);
	}

	public static Graphic LoadGraphicWithHeader(IDataReader dataReader)
	{
		var data = LoadGraphicDataWithHeader(dataReader, out int width, out int height, out int planes);

		return Graphic.FromBitPlanes(width, height, data, planes);
	}

	public static PaletteGraphic LoadGraphicWithHeader(IDataReader dataReader, IGraphic palette)
	{
		var data = LoadGraphicDataWithHeader(dataReader, out int width, out int height, out int planes);

		return PaletteGraphic.FromBitPlanes(width, height, data, planes, palette);
	}

	public IPaletteGraphic Load80x80Graphic(Image80x80 index)
	{
		if (graphics80x80.TryGetValue(index, out var graphic))
			return graphic;

		// Each even file is the next image.
		// Each odd file is the palette for the last image.	

		var asset = assetProvider.GetAsset(new AssetIdentifier(AssetType.Graphics80x80, (int)index * 2 - 1));

		if (asset == null)
			throw new AmberException(ExceptionScope.Data, $"80x80 graphic {index} not found.");

		// Read palette
		var paletteAsset = assetProvider.GetAsset(new AssetIdentifier(AssetType.Graphics80x80, (int)index * 2));

		if (paletteAsset == null)
			throw new AmberException(ExceptionScope.Data, $"Palette for 80x80 graphic {index} not found.");

		var palette = PaletteLoader.LoadPalette(paletteAsset.GetReader());

		// Load graphic
		graphic = PaletteGraphic.FromBitPlanes(80, 80, asset.GetReader().ReadToEnd(), 4, palette);

		graphics80x80.Add(index, graphic);

		return graphic;
	}

	public IGraphic LoadItemGraphic(ItemGraphic index)
	{
		if (itemGraphics.TryGetValue(index, out var graphic))
			return graphic;

		var asset = assetProvider.GetAsset(new AssetIdentifier(AssetType.ItemGraphic, (int)index));

		if (asset == null)
			throw new AmberException(ExceptionScope.Data, $"Item graphic {index} not found.");

		var reader = asset.GetReader();

		// Load graphic
		graphic = Graphic.FromBitPlanes(16, 16, reader.ReadBytes(16 * 16 / 2), 4);

		itemGraphics.Add(index, graphic);

		return graphic;
	}
}
