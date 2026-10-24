using Amber.Common;
using Amber.IO.Common.Serialization;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy;

internal class MonsterGroup(MonsterGroupRow?[] rows) : IMonsterGroup
{
	public IReadOnlyList<MonsterGroupRow?> Rows { get; } = rows;
}

/// <summary>
/// Each monster group consists of 3 words with the monster indices (0 = none)
/// and 3 bytes with the columns (6 bits) of the monsters of each row.
/// </summary>
internal class MonsterGroupLoader(AssetProvider assetProvider) : IMonsterGroupLoader
{
	readonly Dictionary<int, IMonsterGroup> monsterGroups = [];

	public IMonsterGroup LoadMonsterGroup(int index)
	{
		if (monsterGroups.TryGetValue(index, out var monsterGroup))
			return monsterGroup;

		var asset = assetProvider.GetAsset(new(AssetType.MonsterGroup, index))
			?? throw new AmberException(ExceptionScope.Data, $"Monster group {index} not found.");
		var reader = asset.GetReader();
		var monsterIndices = reader.ReadWords(IMonsterGroup.RowCount);
		var columnMasks = reader.ReadBytes(IMonsterGroup.RowCount);
		var rows = new MonsterGroupRow?[IMonsterGroup.RowCount];

		for (int i = 0; i < IMonsterGroup.RowCount; i++)
		{
			if (monsterIndices[i] != 0 && columnMasks[i] != 0)
				rows[i] = new(monsterIndices[i], columnMasks[i]);
		}

		monsterGroup = new MonsterGroup(rows);
		monsterGroups.Add(index, monsterGroup);

		return monsterGroup;
	}
}
