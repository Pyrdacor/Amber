using Amber.Common;

namespace Amberstar.GameData;

/// <summary>
/// Graphics of the altar where the Amberstar is assembled (PUZZLE.ICN in the original).
/// </summary>
public enum PuzzleGraphic
{
	Background, // 176x144
	Center, // 96x96, the 13th piece which lies on the altar from the start
	FirstPiece, // 12 pieces with 32x32 each
	FirstSign = FirstPiece + IAltarPuzzle.PieceCount, // 12 signs with 32x32 each
	Count = FirstSign + IAltarPuzzle.PieceCount
}

/// <summary>
/// The data of the altar puzzle in the program data. The pieces of the Amberstar
/// must be laid on the altar in a given order.
/// </summary>
public interface IAltarPuzzle
{
	public const int PieceCount = 12;

	/// <summary>
	/// The puzzle text index which asks for the piece of each step.
	/// </summary>
	IReadOnlyList<int> StepTextIndices { get; }

	/// <summary>
	/// The 0-based piece which must be laid in each step.
	/// </summary>
	IReadOnlyList<int> PieceOrder { get; }

	/// <summary>
	/// The 0-based sign which is shown on each piece.
	/// </summary>
	IReadOnlyList<int> PieceSigns { get; }

	/// <summary>
	/// The position of each piece on the altar background.
	/// </summary>
	IReadOnlyList<Position> PiecePositions { get; }

	/// <summary>
	/// The name which is shown on each piece.
	/// </summary>
	IReadOnlyList<string> PieceNames { get; }
}
