namespace Amberstar.GameData;

/// <summary>
/// A monster type of a monster group and the columns of its row.
/// </summary>
public record MonsterGroupRow(int MonsterIndex, byte ColumnMask)
{
	/// <summary>
	/// The first column is the most significant of the 6 bits.
	/// </summary>
	public bool HasColumn(int column) => (ColumnMask & (0x20 >> column)) != 0;
}

/// <summary>
/// A group of monsters which is fought (MON_DATA.AMB). The first monster type stands in the
/// front row (next to the party), the second one in the middle and the third one in the back row.
/// </summary>
public interface IMonsterGroup
{
	public const int RowCount = 3;
	public const int ColumnCount = 6;

	IReadOnlyList<MonsterGroupRow?> Rows { get; }
}
