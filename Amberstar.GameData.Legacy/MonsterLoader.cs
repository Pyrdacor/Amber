using Amber.Assets.Common;
using Amber.Common;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy
{
	// Monsters are character records of type monster inside CHARDATA.AMB
	// like the persons. The index is the index of the character record.
	// Note: MON_DATA.AMB contains the monster groups (combat layouts) instead.
	public class MonsterLoader(AssetProvider assetProvider) : IMonsterLoader
	{
		readonly Dictionary<int, IMonster> monsters = [];

		static bool IsMonster(IAsset asset)
		{
			var reader = asset.GetReader();
			reader.Position = 0x02;
			return (CharacterType)reader.ReadByte() == CharacterType.Monster;
		}

		public IMonster LoadMonster(int index)
		{
			if (!monsters.TryGetValue(index, out var monster))
			{
				var asset = assetProvider.GetAsset(new(AssetType.Person, index));

				if (asset == null || !IsMonster(asset))
					throw new AmberException(ExceptionScope.Data, $"Monster {index} not found.");

				monster = Monster.Load(asset);
				monsters.Add(index, monster);
			}

			return monster;
		}

		public IReadOnlyDictionary<int, IMonster> LoadAllMonsters()
		{
			foreach (var key in assetProvider.GetAssetKeys(AssetType.Person))
			{
				if (monsters.ContainsKey(key))
					continue;

				var asset = assetProvider.GetAsset(new(AssetType.Person, key));

				if (asset == null)
					throw new AmberException(ExceptionScope.Data, $"Character {key} not found.");

				if (IsMonster(asset))
					monsters.Add(key, Monster.Load(asset));
			}

			return monsters.AsReadOnly();
		}
	}
}