using Amber.Common;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy
{
	public class MapLoader(Amber.Assets.Common.IAssetProvider assetProvider) : IMapLoader
	{
		readonly Dictionary<int, IMap> maps = [];

		public IMap LoadMap(int index)
		{
			if (!maps.TryGetValue(index, out var map))
			{
				var asset = assetProvider.GetAsset(new AssetIdentifier(AssetType.Map, index));

				if (asset == null)
					throw new AmberException(ExceptionScope.Data, $"Map {index} not found.");

				map = Map.Load(asset);
				maps.Add(index, map);
			}

			return map;
		}

		public bool TryLoadMap2D(int index, out IMap2D? map2D)
		{
			var map = LoadMap(index);

			if (map is IMap2D m)
			{
				map2D = m;
				return true;
			}
			else
			{
				map2D = null;
				return false;
			}
		}

		public bool TryLoadMap3D(int index, out IMap3D? map3D)
		{
			var map = LoadMap(index);

			if (map is IMap3D m)
			{
				map3D = m;
				return true;
			}
			else
			{
				map3D = null;
				return false;
			}
		}
	}
}
