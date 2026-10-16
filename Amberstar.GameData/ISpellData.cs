namespace Amberstar.GameData;

/// <summary>
/// Spell information (SPELLDAT.DAT in the original).
/// There are 30 spells per spell school.
/// </summary>
public interface ISpellData
{
	public const int SpellsPerSchool = 30;

	SpellSchool School { get; }

	/// <summary>
	/// 1-based spell number inside the spell school.
	/// </summary>
	int SpellNumber { get; }

	/// <summary>
	/// Spell points which are needed to cast the spell.
	/// </summary>
	byte SpellPointCost { get; }

	/// <summary>
	/// Spell learning points which are needed to learn the spell from a scroll.
	/// </summary>
	byte SpellLearningPointCost { get; }

	/// <summary>
	/// Raw flags where the spell can be cast (byte 0).
	/// </summary>
	byte LocationFlags { get; }

	/// <summary>
	/// Raw flags of the spell targets (byte 3).
	/// </summary>
	byte TargetFlags { get; }
}
