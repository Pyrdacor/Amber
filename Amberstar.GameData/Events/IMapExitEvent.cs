namespace Amberstar.GameData.Events;

/// <summary>
/// Just moves the party to a new map.
/// </summary>
public interface IMapExitEvent : IEvent
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
    byte Direction { get; }

	/// <summary>
	/// Word 6
	/// </summary>
	word MapIndex { get; }
}
