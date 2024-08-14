using Amber.Assets.Common;
using Amber.Common;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy
{
	public class MapLoader : IMapLoader
	{
		readonly Dictionary<AssetIdentifier, IMap> maps = [];

		public IMap LoadMap(IAsset asset)
		{
			if (!maps.TryGetValue(asset.Identifier, out var map))
			{
				map = Map.Load(asset);
				maps.Add(asset.Identifier, map);
			}

			return map;
		}

		public bool TryLoadMap2D(IAsset asset, out IMap2D? map2D)
		{
			var map = LoadMap(asset);

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

		public bool TryLoadMap3D(IAsset asset, out IMap3D? map3D)
		{
			var map = LoadMap(asset);

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
