namespace Amberstar.GameData.Events;

public enum ActiveSpellRemoval
{
	All,
	Light,
	ArmorProtection,
	WeaponPower,
	AntiMagic,
	Clairvoyance,
	Invisibility
}

/// <summary>
/// Removes active buffs.
/// 
/// Optionally displays a text beforehand.
/// </summary>
public interface IAntiMagicEvent : IEvent
{
	/// <summary>
	/// Byte 1
	/// 
	/// 0 means "all".
	/// </summary>
	ActiveSpellRemoval ActiveSpell { get; }

	/// <summary>
	/// Byte 4
	/// 
	/// If non-zero, a text is shown before the spin.
	/// </summary>
	byte TextIndex { get; }
}
