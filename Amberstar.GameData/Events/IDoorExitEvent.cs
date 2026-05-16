namespace Amberstar.GameData.Events;

/// <summary>
/// The same as <see cref="IDoorEvent"/> but
/// can specify an additional event which is
/// triggered after the door is opened. The
/// additional event is also triggered if
/// the door is already open or if you
/// possess the Amberstar.
/// </summary>
public interface IDoorExitEvent : IDoorEvent
{
	/// <summary>
	/// Byte 4
	/// </summary>
	byte OpenedEventIndex { get; }
}
