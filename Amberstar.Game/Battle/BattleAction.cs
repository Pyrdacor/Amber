using Amber.Common;
using Amberstar.GameData;

namespace Amberstar.Game.Battle;

/// <summary>
/// The actions in the order of the original (the values are used for the status icons).
/// </summary>
internal enum BattleActionType
{
	Parry,
	Move,
	Flee,
	Attack,
	Magic,
	/// <summary>
	/// The participant does nothing (asleep, surprised, ...).
	/// </summary>
	None,
	Stunned,
	Petrified
}

/// <summary>
/// The action of a participant in the next round. The target is a grid position (move and attack).
/// Spells are given by school and number and their targets are chosen with the action.
/// Spells of items also give the item slot (its charge is used when the spell is cast).
/// </summary>
internal sealed record BattleAction(BattleActionType Type, Position Target = default, bool Ranged = false,
	SpellSchool SpellSchool = SpellSchool.None, int SpellNumber = 0, IReadOnlyList<BattleParticipant>? Targets = null,
	ItemSlot? Item = null, bool ItemEquipped = false)
{
	/// <summary>
	/// The party can't cast most special spells in battles, not even with items (the setup table L1879e in the original).
	/// </summary>
	public static bool PartyCanCastInBattle(SpellSchool school, int spellNumber) =>
		school != SpellSchool.Special || spellNumber is 13 or 14 or 15 or 17;

	/// <summary>
	/// The targets of a spell are given by the target flags of the spell data.
	/// </summary>
	public static SpellTarget GetSpellTarget(ISpellData spellData)
	{
		int flags = spellData.TargetFlags;

		if ((flags & 0x01) != 0)
			return SpellTarget.Member;
		if ((flags & 0x02) != 0)
			return SpellTarget.Party;
		if ((flags & 0x04) != 0)
			return SpellTarget.Monster;
		if ((flags & 0x08) != 0)
			return SpellTarget.MonsterGroup;
		if ((flags & 0x10) != 0)
			return SpellTarget.AllMonsters;

		return SpellTarget.None;
	}

	public static readonly BattleAction Parry = new(BattleActionType.Parry);
	public static readonly BattleAction None = new(BattleActionType.None);
}

/// <summary>
/// Something which happened in a round. The battle screen shows them one after another.
/// </summary>
internal abstract record BattleEvent;

/// <summary>
/// A text like "NAME TRIFFT NICHT!". The value is shown for hits ("A TRIFFT B MIT n LP.").
/// </summary>
internal sealed record BattleTextEvent(BattleParticipant? Actor, Message Message, BattleParticipant? Target = null, int? Value = null) : BattleEvent;

/// <summary>
/// A spell is cast. Party members show the spell name, monsters only that they cast a spell.
/// </summary>
internal sealed record BattleSpellEvent(BattleParticipant Caster, SpellSchool School, int SpellNumber, bool Success) : BattleEvent;

/// <summary>
/// The damage is shown on the tactical battle field for a while.
/// </summary>
internal sealed record BattleDamageEvent(BattleParticipant Target, int Damage) : BattleEvent;

/// <summary>
/// A monster attacks or casts a spell and shows its attack frames.
/// </summary>
internal sealed record BattleAttackAnimationEvent(MonsterParticipant Monster) : BattleEvent;

/// <summary>
/// A spell of the party affects a monster. Particles of the animation (1-based, F_T_ANIM.ICN) fly around the monster.
/// </summary>
internal sealed record BattleSpellAnimationEvent(MonsterParticipant Target, int Animation) : BattleEvent;

internal sealed record BattleMoveEvent(BattleParticipant Participant, Position From, Position To) : BattleEvent;

/// <summary>
/// The participant left the battle field (dead, petrified or fled).
/// </summary>
internal sealed record BattleRemoveEvent(BattleParticipant Participant) : BattleEvent;

internal enum BattleResult
{
	Ongoing,
	Won,
	/// <summary>
	/// Every party member which can still act fled.
	/// </summary>
	PartyFled,
	/// <summary>
	/// No party member can act anymore.
	/// </summary>
	PartyDefeated
}
