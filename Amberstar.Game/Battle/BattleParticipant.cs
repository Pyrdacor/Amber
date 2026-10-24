using Amber.Common;
using Amberstar.GameData;

namespace Amberstar.Game.Battle;

internal enum BattleSide
{
	Party,
	Monsters
}

/// <summary>
/// A monster or party member on the battle field.
/// </summary>
internal abstract class BattleParticipant
{
	public abstract BattleSide Side { get; }

	public abstract IBattleCharacter Character { get; }

	/// <summary>
	/// Column (0-5) and row (0-2 monsters, 3-4 party).
	/// </summary>
	public Position Position { get; set; }

	public string Name => Character.Name;

	public Condition Conditions => Character.GetConditions();

	public bool HasAnyConditionOf(Condition conditions) => Character.HasAnyConditionOf(conditions);

	/// <summary>
	/// Initiative of the participant (speed and its bonus).
	/// </summary>
	public int Speed => Character.Attributes[GameData.Attribute.Speed].TotalCurrent;
}

internal sealed class PartyMemberParticipant(IPartyMember partyMember, int slot) : BattleParticipant
{
	public override BattleSide Side => BattleSide.Party;

	public override IBattleCharacter Character => PartyMember;

	public IPartyMember PartyMember { get; } = partyMember;

	/// <summary>
	/// 1-based party slot.
	/// </summary>
	public int Slot { get; } = slot;
}

/// <summary>
/// Each monster of a group has its own copy of the monster data (hit points, conditions, items, ...).
/// </summary>
internal sealed class MonsterParticipant(IMonster monster) : BattleParticipant
{
	public override BattleSide Side => BattleSide.Monsters;

	public override IBattleCharacter Character => Monster;

	public IMonster Monster { get; } = monster.Clone();

	/// <summary>
	/// The monster type (index of the monster data) for the morale.
	/// </summary>
	public int MonsterIndex { get; init; }
}
