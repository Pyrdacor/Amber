namespace Amberstar.GameData.Events;

/// <summary>
/// Changes a tile on the same map.
/// </summary>
public interface IChangeTileEvent : IEvent
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
	/// Word 6
	/// </summary>
	word IconIndex { get; }
}
