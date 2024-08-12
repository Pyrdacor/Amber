namespace Amberstar.GameData.Events;

/// <summary>
/// Shows the chest screen (locked or unlocked).
/// </summary>
public interface IChestEvent : IEvent
{
    /// <summary>
    /// Byte 1
    /// 
    /// If 100, can't be lockpicked. Otherwise this
    /// values is subtracted from the active player's
    /// lockpicking skill value if lockpicking is
    /// performed. A value of 0 means "always open".
    /// 
    /// It seems that chest in Amberstar can only be
    /// opened by lockpicking or the lockpick item.
    /// There is no possibility to use or specify
    /// another item like a specific key.
    /// 
    /// It seems that all chests are open if you
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
    /// 
    /// If set, a check against the active player's
    /// search skill is performed. And only if it
    /// succeeds, the chest is shown. The check is
    /// silent, so you won't even know that a chest
    /// exists if it fails.
	/// </summary>
	bool Hidden { get; }

	/// <summary>
	/// Word 6
	/// </summary>
	word ChestIndex { get; }
}
