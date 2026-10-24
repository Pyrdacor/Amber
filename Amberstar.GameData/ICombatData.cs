namespace Amberstar.GameData;

/// <summary>
/// Damage range and animation of a black magic damage spell.
/// </summary>
public record SpellDamage(int Min, int Max, int Animation);

/// <summary>
/// Combat data of the program data which is not part of the released source code.
/// </summary>
public interface ICombatData
{
	/// <summary>
	/// Gets the damage of the black magic spells 1 to 20.
	/// </summary>
	SpellDamage GetSpellDamage(int blackSpellNumber);

	/// <summary>
	/// Gets the immunity flag of a spell element (bits 6-7 of the spell location flags).
	/// The vulnerability flag is the immunity flag shifted by 4 bits.
	/// </summary>
	MonsterElementalFlags GetElementImmunity(int element);
}
