using Amber.Common;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy
{
	public class MapLoader(AssetProvider assetProvider) : IMapLoader
	{
		readonly Dictionary<int, IMap> maps = [];

		public IMap LoadMap(int index)
		{
			if (!maps.TryGetValue(index, out var map))
			{
				var asset = assetProvider.GetAsset(new(AssetType.Map, index));

				if (asset == null)
					throw new AmberException(ExceptionScope.Data, $"Map {index} not found.");

				var loadedMap = Map.Load(asset);
                loadedMap.Index = index;
                maps.Add(index, loadedMap);
				map = loadedMap;

            }

			return map;
		}

        public IReadOnlyDictionary<int, IMap> LoadAllMaps()
		{
            var keys = assetProvider.GetAssetKeys(AssetType.Map);

            if (maps.Count == keys.Count)
                return maps.AsReadOnly();

            foreach (var key in keys)
            {
                if (maps.ContainsKey(key))
                    continue;

                var asset = assetProvider.GetAsset(new(AssetType.Map, key));

                if (asset == null)
                    throw new AmberException(ExceptionScope.Data, $"Map {key} not found.");

                var map = Map.Load(asset);
                maps.Add(key, map);
            }

            return maps.AsReadOnly();
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
