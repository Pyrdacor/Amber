using Amber.Common;
using Amber.IO.Common.Serialization;
using Amber.Serialization;

namespace Amberstar.GameData.Legacy;

/// <summary>
/// The altar puzzle data of the program data (the routines are not part of the released source code).
///
/// It consists of 12 text indices, the 1-based piece order, the 1-based sign of each piece,
/// 12 word coordinate pairs, 12 pointers to the piece names and the zero-terminated names.
/// The names are stored in the order of the pointers, so the pointers are skipped.
/// </summary>
internal class AltarPuzzle : IAltarPuzzle
{
	const int Count = IAltarPuzzle.PieceCount;
	const int NamesOffset = 3 * Count + 4 * Count + 4 * Count;

	public AltarPuzzle(byte[] data)
	{
		var reader = new DataReader(data);

		StepTextIndices = [.. reader.ReadBytes(Count).Select(index => (int)index)];
		PieceOrder = [.. reader.ReadBytes(Count).Select(piece => piece - 1)];
		PieceSigns = [.. reader.ReadBytes(Count).Select(sign => sign - 1)];
		PiecePositions = [.. Enumerable.Range(0, Count).Select(_ => new Position(reader.ReadWord(), reader.ReadWord()))];
		reader.Position += 4 * Count;
		PieceNames = [.. Enumerable.Range(0, Count).Select(_ => reader.ReadNullTerminatedString())];
	}

	/// <summary>
	/// Gets the size of the puzzle data at the current reader position.
	/// </summary>
	public static int DetermineSize(IDataReader reader)
	{
		int start = reader.Position;
		reader.Position += NamesOffset;

		for (int i = 0; i < Count; i++)
			reader.ReadNullTerminatedString();

		int size = reader.Position - start;
		reader.Position = start;

		return size;
	}

	public IReadOnlyList<int> StepTextIndices { get; }
	public IReadOnlyList<int> PieceOrder { get; }
	public IReadOnlyList<int> PieceSigns { get; }
	public IReadOnlyList<Position> PiecePositions { get; }
	public IReadOnlyList<string> PieceNames { get; }
}
