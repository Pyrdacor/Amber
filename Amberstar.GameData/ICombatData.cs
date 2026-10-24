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
/// A particle of a spell animation. The position is relative to the center of the monster
/// and the icon is one of the 6 icons of the animation in F_T_ANIM.ICN.
/// </summary>
public record SpellParticle(int X, int Y, int Icon);

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

	public const int SpellAnimationFrameCount = 24;
	public const int SpellAnimationParticleCount = 6;

	/// <summary>
	/// Number of particle paths. A random one is used for each spell animation.
	/// </summary>
	int SpellAnimationPathCount { get; }

	/// <summary>
	/// Gets the particles of a frame of a spell animation path.
	/// </summary>
	IReadOnlyList<SpellParticle> GetSpellParticles(int path, int frame);

	/// <summary>
	/// Gets the graphic (4x5) of a digit of the damage numbers on the tactical battle field.
	/// The pixels use the color index <see cref="DamageDigitColorIndex"/>.
	/// </summary>
	IGraphic GetDamageDigit(int digit);

	public const int DamageDigitWidth = 4;
	public const int DamageDigitHeight = 5;
	public const byte DamageDigitColorIndex = 9;
}
