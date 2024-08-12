namespace Amberstar.GameData.Events;

/// <summary>
/// Shows the riddle mouth window.
/// </summary>
public interface IRiddleMouthEvent : IEvent
{
	/// <summary>
	/// Byte 1
	/// 
	/// See <see cref="IconIndex"/>.
	/// </summary>
	byte X { get; }

	/// <summary>
	/// Byte 2
	/// 
	/// See <see cref="IconIndex"/>.
	/// </summary>
	byte Y { get; }

	/// <summary>
	/// Byte 3
	/// 
	/// If non-zero, a text is shown when the mouth is asked.
	/// </summary>
	byte RiddleTextIndex { get; }

	/// <summary>
	/// Byte 4
	/// 
	/// If non-zero, a text is shown after the riddle was solved.
	/// </summary>
	byte SolvedTextIndex { get; }

	/// <summary>
	/// Word 6
	/// 
	/// Index of the known word which solves
	/// the riddle.
	/// </summary>
	word WordIndex { get; }

	/// <summary>
	/// Word 8
	/// 
	/// Map tile changes to this icon after
	/// solving the riddle. The map tile
	/// is specified by X and Y.
	/// 
	/// If 0, no tile is changed.
	/// </summary>
	word IconIndex { get; }
}
