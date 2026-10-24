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
/// Spells are given by school and number. Item spells give the inventory slot instead.
/// </summary>
internal sealed record BattleAction(BattleActionType Type, Position Target = default, bool Ranged = false,
	SpellSchool SpellSchool = SpellSchool.None, int SpellNumber = 0, int? ItemSlot = null)
{
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

internal sealed record BattleDamageEvent(BattleParticipant Target, int Damage) : BattleEvent;

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
