namespace Amberstar.GameData.Events;

public enum ShowPictureTextTrigger
{
    Examine,
    Walk,
    Both
}

/// <summary>
/// Shows a picture and/or text.
/// </summary>
public interface IShowPictureTextEvent : IEvent
{
	/// <summary>
	/// Byte 1
	/// 
	/// If 0, no picture is shown.
	/// Otherwise it is a 80x80 picture.
	/// </summary>
	byte Picture { get; }

	/// <summary>
	/// Byte 2
	/// </summary>
	byte TextIndex { get; }

	/// <summary>
	/// Byte 3
	/// </summary>
	ShowPictureTextTrigger Trigger { get; }

	/// <summary>
	/// Word 6
	/// 
	/// If non-zero, this gives the bit to a
	/// known word inside the savegame. So
	/// you learn a new word which you can
	/// use in conversations.
	/// </summary>
	word SetWordBit { get; }
}
