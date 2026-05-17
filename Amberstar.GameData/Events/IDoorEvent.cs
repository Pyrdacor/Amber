namespace Amberstar.GameData.Events;

/// <summary>
/// Shows the "open door" screen.
/// </summary>
public interface IDoorEvent : ILockedEvent
{
	/// <summary>
	/// Word 6
	/// 
	/// Item to unlock the door (key, amberstar, etc).
	/// </summary>
	word ItemIndex { get; }
}
