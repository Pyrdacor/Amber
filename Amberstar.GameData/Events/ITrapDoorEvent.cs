namespace Amberstar.GameData.Events;

/// <summary>
/// Player falls down and optionally gets damaged.
/// 
/// This was intended to also be used to climb up
/// through a trap door in the ceiling. But this
/// case is not implemented. If the flag is set,
/// nothing happens.
/// 
/// Note that the view direction is preserved.
/// </summary>
public interface ITrapDoorEvent : IEvent
{
	/// <summary>
	/// Byte 1
	/// </summary>
	byte X { get; }

	/// <summary>
	/// Byte 2
	/// </summary>
	byte Y { get; }

	/// <summary>
	/// Byte 3
	/// 
	/// If not set, the trap door is in the ceiling
	/// instead of the floor. The original code
	/// does not implement this case, so if it
	/// is false, nothing happens at all after
	/// the text was shown.
	/// </summary>
	bool Floor { get; }

	/// <summary>
	/// Byte 4
    /// 
    /// If non-zero, a text is shown before the fall.
	/// </summary>
	byte TextIndex { get; }

	/// <summary>
	/// Word 6
	/// </summary>
	word MapIndex { get; }

	/// <summary>
	/// Word 8
    /// 
    /// A random value between 1 and this value
    /// is calculated per player.
	/// </summary>
	word MaxFallDamage { get; }
}
