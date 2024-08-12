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
	/// Byte 4
	/// 
	/// If non-zero, a text is shown before the change.
	/// </summary>
	byte TextIndex { get; }

	/// <summary>
	/// Word 6
	/// </summary>
	word IconIndex { get; }
}
