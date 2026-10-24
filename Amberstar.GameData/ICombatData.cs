using Amber.Assets.Common;

namespace Amberstar.GameData;

/// <summary>
/// Damage range and animation of a black magic damage spell.
/// </summary>
public record SpellDamage(int Min, int Max, int Animation);

/// <summary>
/// The foot point of a monster graphic on the combat background and the frame group (0 small to 2 big).
/// </summary>
public record MonsterGraphicPosition(int X, int Y, int FrameGroup);

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

	/// <summary>
	/// Gets the palette of a combat background (1-based). The background, the monsters and the
	/// tactical icons use it.
	/// </summary>
	IGraphic GetPalette(int background);

	public const int CombatBackgroundCount = 14;

	MonsterGraphicPosition GetMonsterPosition(int column, int row);

	/// <summary>
	/// Gets the number of frames of the 3 frame groups of a monster graphic (1-based).
	/// </summary>
	IReadOnlyList<int> GetMonsterFrameCounts(int monsterGraphic);
}
