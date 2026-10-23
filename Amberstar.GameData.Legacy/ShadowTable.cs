using Amber.Common;
using Amber.IO.Common.Serialization;
using Amber.Serialization;

namespace Amberstar.GameData.Legacy;

/// <summary>
/// The shadow table (SHADOW.DAT in the original).
///
/// It starts with a word offset for each relative position (index = dy * 9 + dx without (0,0)).
/// The offsets are relative to the end of the offset table. Each entry is a list of signed
/// byte coordinate pairs which ends with a zero word.
/// </summary>
internal class ShadowTable : IShadowTable
{
	const int EntryCount = (IShadowTable.MaxDistanceX + 1) * (IShadowTable.MaxDistanceY + 1) - 1;
	readonly Position[][] shadows = new Position[EntryCount + 1][];

	public ShadowTable(byte[] data)
	{
		shadows[0] = [];

		if (data.Length == 0)
		{
			for (int i = 1; i <= EntryCount; i++)
				shadows[i] = [];

			return;
		}

		var reader = new DataReader(data);
		var offsets = reader.ReadWords(EntryCount);
		int dataStart = reader.Position;

		for (int i = 0; i < EntryCount; i++)
		{
			reader.Position = dataStart + offsets[i];
			var positions = new List<Position>();

			while (reader.PeekWord() != 0)
				positions.Add(new(unchecked((sbyte)reader.ReadByte()), unchecked((sbyte)reader.ReadByte())));

			shadows[i + 1] = [.. positions];
		}
	}

	/// <summary>
	/// The size of the table in the given data (or 0 if it's no shadow table).
	/// </summary>
	public static int DetermineSize(IDataReader reader)
	{
		int start = reader.Position;
		var offsets = reader.ReadWords(EntryCount);
		int dataStart = reader.Position;

		reader.Position = dataStart + offsets[^1];

		while (reader.ReadWord() != 0)
			;

		int size = reader.Position - start;
		reader.Position = start;

		return size;
	}

	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	public IReadOnlyList<Position> GetShadow(int distanceX, int distanceY)
	{
		if (distanceX < 0 || distanceY < 0 || distanceX > IShadowTable.MaxDistanceX || distanceY > IShadowTable.MaxDistanceY)
			return [];

		return shadows[distanceY * (IShadowTable.MaxDistanceX + 1) + distanceX];
	}
}
