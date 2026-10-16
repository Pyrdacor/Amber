namespace Amberstar.GameData;

/// <summary>
/// Class information which is used by the guilds (CLASSDAT.DAT in the original).
/// </summary>
public interface IClassData
{
	public const int MaxLevel = 20;

	Class Class { get; }

	/// <summary>
	/// Maximum skill value for every skill.
	/// </summary>
	byte GetSkillMaxValue(Skill skill);

	/// <summary>
	/// Experience points which are needed to advance from the given level to the next one.
	/// </summary>
	uint GetExperienceForNextLevel(int level);

	/// <summary>
	/// The attacks per round are the level divided by this value (0: no change).
	/// </summary>
	word AttacksPerRoundLevel { get; }

	/// <summary>
	/// Additional hit points per level (the stamina adds 10% of its value as well).
	/// </summary>
	word HitPointsPerLevel { get; }

	/// <summary>
	/// Additional spell points per level (the intelligence adds 5% of its value as well).
	/// </summary>
	word SpellPointsPerLevel { get; }

	/// <summary>
	/// Additional spell learning points per level (the intelligence adds 5% of its value as well).
	/// </summary>
	word SpellLearningPointsPerLevel { get; }

	/// <summary>
	/// Spell schools of the class. If none, the class can't use magic.
	/// </summary>
	SpellSchoolFlags SpellSchools { get; }
}
