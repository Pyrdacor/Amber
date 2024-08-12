namespace Amberstar.GameData.Events;

/// <summary>
/// Almost similar to <see cref="IMapExitEvent"/>.
/// But the travel type is reset to "walk" before
/// the new map is loaded.
/// </summary>
public interface ITravelExitEvent : IEvent
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
	/// </summary>
	Direction Direction { get; }

	/// <summary>
	/// Word 6
	/// </summary>
	word MapIndex { get; }
}
