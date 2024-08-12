namespace Amberstar.GameData.Events;

/// <summary>
/// Changes an attribute.
/// 
/// Optionally displays a text beforehand.
/// </summary>
public interface IAttributeChangeEvent : IEvent
{
    /// <summary>
    /// Byte 1
    /// </summary>
    byte Attribute { get; }

	/// <summary>
	/// Byte 2
	/// 
	/// If false, the amount is subtracted.
	/// </summary>
	bool Add { get; }

	/// <summary>
	/// Byte 3
	/// 
	/// If true, a random value between 1 and the amount is calculated.
	/// </summary>
	bool Random { get; }

	/// <summary>
	/// Byte 4
	/// 
	/// If non-zero, a text is shown before the change.
	/// </summary>
	byte TextIndex { get; }

	/// <summary>
	/// Word 6
	/// 
	/// 0: All, otherwise only the active player.
	/// </summary>
	bool AffectAllPlayers { get; }

	/// <summary>
	/// Word 8
	/// </summary>
	word Amount { get; }
}
