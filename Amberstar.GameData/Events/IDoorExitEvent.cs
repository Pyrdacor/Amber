namespace Amberstar.GameData.Events;

/// <summary>
/// The same as <see cref="IDoorEvent"/> but
/// can specify an additional event which is
/// triggered after the door is opened. The
/// additional event is also triggered if
/// the door is already open or if you
/// possess the Amberstar.
/// </summary>
public interface IDoorExitEvent : IEvent
{
	/// <summary>
	/// Byte 1
	/// 
	/// If 100, can't be lockpicked. Otherwise this
	/// values is subtracted from the active player's
	/// lockpicking skill value if lockpicking is
	/// performed.
	/// 
	/// It seems that all doors are open if you
	/// possess the Amberstar (needs verification).
	/// </summary>
	byte LockpickReduction { get; }

    /// <summary>
    /// Byte 2
    /// 
    /// If given and lockpicking failed, a check
    /// against the player's dexterity is performed.
    /// If failed, the trap is triggered.
    /// 
    /// The same check happens if you try to disarm
    /// a found trap. The trap is also triggered if
    /// the dexterity check fails.
    /// </summary>
    TrapType TrapType { get; }

	/// <summary>
	/// Byte 3
	/// </summary>
	byte TrapDamage { get; }

	/// <summary>
	/// Byte 4
	/// </summary>
	byte OpenedEventIndex { get; }

	/// <summary>
	/// Word 6
	/// 
	/// Item to unlock the door (key, amberstar, etc).
	/// </summary>
	word ItemIndex { get; }
}
