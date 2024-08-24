using Amber.Common;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy;

internal class TilesetLoader(Amber.Assets.Common.IAssetProvider assetProvider) : ITilesetLoader
{
	private readonly Dictionary<int, ITileset> tilesets = [];

	public ITileset LoadTileset(int index)
	{
		if (tilesets.TryGetValue(index, out var tileset))
			return tileset;

		var asset = assetProvider.GetAsset(new(AssetType.Tileset, index));

		if (asset == null)
			throw new AmberException(ExceptionScope.Data, $"Tileset {index} not found.");

		tileset = new Tileset(asset.GetReader());

		tilesets.Add(index, tileset);

		return tileset;
	}
}
