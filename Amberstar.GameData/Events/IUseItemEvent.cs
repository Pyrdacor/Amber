namespace Amberstar.GameData.Events;

/// <summary>
/// If the text is given, it is displayed if the
/// player steps onto the tile or if it is part
/// of an event chain.
/// 
/// But this event is meant to be triggered by using an item.
/// If you use the right item, a tile is changed on the map.
/// The text is not displayed then!
/// </summary>
public interface IUseItemEvent : IEvent
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
	/// </summary>
	byte TextIndex { get; }

	/// <summary>
	/// Word 6
	/// </summary>
	word ItemIndex { get; }

	/// <summary>
	/// Word 8
	/// </summary>
	word IconIndex { get; }
}
