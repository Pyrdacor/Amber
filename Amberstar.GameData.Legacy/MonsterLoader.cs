using Amber.Common;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy
{
	public class MonsterLoader(AssetProvider assetProvider) : IMonsterLoader
	{
		readonly Dictionary<int, IMonster> monsters = [];

		public IMonster LoadMonster(int index)
		{
			if (!monsters.TryGetValue(index, out var monster))
			{
				var asset = assetProvider.GetAsset(new(AssetType.Monster, index));

				if (asset == null)
					throw new AmberException(ExceptionScope.Data, $"Monster {index} not found.");

				monster = Monster.Load(asset);
				monsters.Add(index, monster);
			}

			return monster;
		}

		public IReadOnlyDictionary<int, IMonster> LoadAllMonsters()
		{
			var keys = assetProvider.GetAssetKeys(AssetType.Monster);

			if (monsters.Count == keys.Count)
				return monsters.AsReadOnly();

			foreach (var key in keys)
			{
				if (monsters.ContainsKey(key))
					continue;

				var asset = assetProvider.GetAsset(new(AssetType.Monster, key));

				if (asset == null)
					throw new AmberException(ExceptionScope.Data, $"Monster {key} not found.");

				var monster = Monster.Load(asset);
                monsters.Add(key, monster);
			}

            return monsters.AsReadOnly();
        }
	}
}