namespace Amberstar.GameData.Events;

/// <summary>
/// Spins the party around.
/// 
/// Optionally displays a text beforehand.
/// 
/// Note that for random direction, the spin
/// will never end in the same direction as before.
/// </summary>
public interface ISpinnerEvent : IEvent
{
    /// <summary>
    /// Byte 1
	/// 
	/// Value 4 here means "random".
    /// </summary>
    Direction Direction { get; }

	/// <summary>
	/// Byte 4
	/// 
	/// If non-zero, a text is shown before the spin.
	/// </summary>
	byte TextIndex { get; }
}
