namespace Amberstar.GameData.Events;

/// <summary>
/// Shows the chest screen (locked or unlocked).
/// </summary>
public interface IChestEvent : ILockedEvent
{
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

    /// <summary>
    /// Word 8
    /// 
    /// Displayed if the chest is open.
    /// </summary>
	word TextIndex { get; }
}
