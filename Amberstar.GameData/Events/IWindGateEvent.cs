namespace Amberstar.GameData.Events;

/// <summary>
/// Exactly like <see cref="ITeleporterEvent"/> but first
/// it is checked if you possess the wind chain.
/// </summary>
public interface IWindGateEvent : IEvent
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
	/// 
	/// Should always match the current map.
	/// </summary>
	word MapIndex { get; }
}
