namespace Amberstar.GameData.Events;

/// <summary>
/// Player teleports to a new location.
/// 
/// Optionally a text can be displayed beforehand.
/// 
/// If the map index differs, this behaves like the
/// <see cref="IMapExitEvent"/> after the text was shown.
/// </summary>
public interface ITeleporterEvent : IEvent
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
	/// Byte 4
	/// 
	/// If non-zero, a text is shown before the teleport.
	/// </summary>
	byte TextIndex { get; }

	/// <summary>
	/// Word 6
	/// </summary>
	word MapIndex { get; }
}
