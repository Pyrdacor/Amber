using Amber.Common;
using Amberstar.GameData;

namespace Amberstar.Game.Battle;

/// <summary>
/// The battle logic without any display. The combat code is not part of the released original
/// source code. It was disassembled from the program (see Docs/Combat.md).
///
/// The battle field has 6 columns and 5 rows. The monsters stand in rows 0-2 (row 2 is the front row)
/// and the party in rows 3-4 (row 3 is the front row). Each round the actions of the party members are
/// chosen first. Then the monsters choose their actions and all actions are executed by initiative.
/// </summary>
internal sealed class Battle
{
	public const int Columns = 6;
	public const int Rows = 5;
	public const int MonsterRows = 3;
	public const int PartyFrontRow = 3;
	public const int PartyBackRow = 4;
	const int MonsterFrontRow = 2;
	// monsters don't cast spells anymore after this round
	const int LastMonsterSpellRound = 25;
	const Condition NotPlacedConditions = Condition.Petrified | Condition.Dead | Condition.Ashes | Condition.Dust;
	// conditions of members which are out of the battle ($e080 in the original)
	const Condition OutOfBattleConditions = Condition.Dead | Condition.Ashes | Condition.Dust | Condition.Fled;
	// conditions of members which can't be the active member ($e586 in the original)
	const Condition CannotLeadConditions = OutOfBattleConditions | Condition.Petrified | Condition.Stunned | Condition.Sleeping | Condition.Mad;
	// conditions which are removed after the battle (mask $ff72 in the original)
	const Condition RemovedAfterBattleConditions = Condition.Irritated | Condition.Sleeping | Condition.Panicked | Condition.Fled;

	static readonly Position[] Neighbors = [new(-1, -1), new(0, -1), new(1, -1), new(-1, 0), new(1, 0), new(-1, 1), new(0, 1), new(1, 1)];
	static readonly Position[] MeleeOffsets = [new(0, -1), new(-1, -1), new(1, -1)];

	readonly IAssetProvider assetProvider;
	readonly Func<int, bool> probe;
	readonly Func<int, int, int> random;
	readonly BattleParticipant?[] grid = new BattleParticipant?[Columns * Rows];
	readonly Dictionary<int, BattleAction> partyActions = [];
	readonly Dictionary<int, int> initialMonsterCounts = [];
	readonly List<MonsterParticipant> defeatedMonsters = [];
	readonly HashSet<int> hastedSlots = [];
	// the actions of the current round (to check if a defender parries)
	readonly Dictionary<BattleParticipant, BattleAction> roundActions = [];
	readonly IReadOnlyList<PartyMemberParticipant> partyMembers;
	bool partyAdvantage;
	bool monsterFrontRowWasEmpty = false;
	bool shiftBackRowNextRound = false;
	int fledMembers = 0;

	/// <param name="probe">Probes a percentage (true on success).</param>
	/// <param name="random">Random number between min and max (inclusive).</param>
	public Battle(IAssetProvider assetProvider, IMonsterGroup monsterGroup, IReadOnlyList<PartyMemberParticipant> partyMembers,
		IReadOnlyList<int> battlePositions, bool partyAdvantage, bool monsterAdvantage, Func<int, bool> probe, Func<int, int, int> random)
	{
		this.assetProvider = assetProvider;
		this.probe = probe;
		this.random = random;
		this.partyMembers = partyMembers;

		// Both advantages cancel each other out.
		if (partyAdvantage && monsterAdvantage)
			partyAdvantage = monsterAdvantage = false;

		this.partyAdvantage = partyAdvantage;
		MonsterAdvantage = monsterAdvantage;

		// The first monster type stands in the front row.
		for (int i = 0; i < IMonsterGroup.RowCount; i++)
		{
			var row = monsterGroup.Rows[i];

			if (row == null)
				continue;

			var monster = assetProvider.MonsterLoader.LoadMonster(row.MonsterIndex);

			for (int column = 0; column < Columns; column++)
			{
				if (row.HasColumn(column))
					Place(new MonsterParticipant(monster) { MonsterIndex = row.MonsterIndex }, new(column, MonsterFrontRow - i));
			}
		}

		foreach (var monster in Monsters)
			initialMonsterCounts[monster.MonsterIndex] = initialMonsterCounts.GetValueOrDefault(monster.MonsterIndex) + 1;

		foreach (var partyMember in partyMembers)
		{
			if (partyMember.HasAnyConditionOf(NotPlacedConditions))
				continue;

			int position = battlePositions[partyMember.Slot - 1];
			Place(partyMember, new(position % Columns, PartyFrontRow + position / Columns));
		}

		// With the monster advantage the party can't act in the first round.
		foreach (var partyMember in partyMembers)
			partyActions[partyMember.Slot] = monsterAdvantage ? BattleAction.None : BattleAction.Parry;
	}

	/// <summary>
	/// The monsters strike first. The first round is executed without any input.
	/// </summary>
	public bool MonsterAdvantage { get; private set; }

	public BattleResult Result { get; private set; } = BattleResult.Ongoing;

	/// <summary>
	/// Ends the battle as if the party fled (used by scenario scripts to leave battles).
	/// </summary>
	public void Abort() => Result = BattleResult.PartyFled;

	/// <summary>
	/// The number of executed rounds.
	/// </summary>
	public int Round { get; private set; } = 0;

	/// <summary>
	/// The active member can't act anymore and another one must be chosen.
	/// </summary>
	public bool NewActiveMemberNeeded { get; set; }

	/// <summary>
	/// 1-based slot of the active party member (the original checks the battle state when it can't act anymore).
	/// </summary>
	public int ActiveSlot { get; set; } = 1;

	/// <summary>
	/// The values of the active armour protection, weapon power and anti-magic spells (percentages).
	/// </summary>
	public int ArmorProtection { get; set; }
	public int WeaponPower { get; set; }
	public int AntiMagic { get; set; }

	/// <summary>
	/// Experience of the defeated and fled monsters.
	/// </summary>
	public int Experience { get; private set; } = 0;

	public IReadOnlyList<MonsterParticipant> DefeatedMonsters => defeatedMonsters;

	public IEnumerable<BattleParticipant> Participants => grid.OfType<BattleParticipant>();

	public IEnumerable<MonsterParticipant> Monsters => grid.OfType<MonsterParticipant>();

	public IReadOnlyList<PartyMemberParticipant> PartyMembers => partyMembers;

	public BattleParticipant? this[Position position] => IsValid(position) ? grid[position.X + position.Y * Columns] : null;

	public BattleParticipant? this[int column, int row] => this[new Position(column, row)];

	public BattleAction GetPartyAction(int slot) => partyActions.GetValueOrDefault(slot, BattleAction.Parry);

	public void SetPartyAction(int slot, BattleAction action) => partyActions[slot] = action;

	/// <summary>
	/// Haste doubles the attacks of the next attack.
	/// </summary>
	public void Haste(int slot) => hastedSlots.Add(slot);

	public bool IsHasted(int slot) => hastedSlots.Contains(slot);

	public PartyMemberParticipant? GetPartyMember(int slot) => partyMembers.FirstOrDefault(partyMember => partyMember.Slot == slot);

	public bool IsOnBattleField(BattleParticipant participant) => this[participant.Position] == participant;

	static bool IsValid(Position position) => position.X >= 0 && position.X < Columns && position.Y >= 0 && position.Y < Rows;

	bool IsFree(int column, int row) => IsValid(new(column, row)) && this[column, row] == null;

	void Place(BattleParticipant participant, Position position)
	{
		participant.Position = position;
		grid[position.X + position.Y * Columns] = participant;
	}

	void Remove(BattleParticipant participant)
	{
		if (IsOnBattleField(participant))
			grid[participant.Position.X + participant.Position.Y * Columns] = null;
	}

	void MoveTo(BattleParticipant participant, Position position)
	{
		Remove(participant);
		Place(participant, position);
	}

	static int Total(CharacterValue value) => (short)(value.CurrentValue + value.BonusValue);

	static int Attribute(IBattleCharacter character, GameData.Attribute attribute) => Total(character.Attributes[attribute]);

	static int Skill(IBattleCharacter character, GameData.Skill skill) => Total(character.Skills[skill]);

	static int WeaponDamage(IBattleCharacter character) => character.Damage + (short)character.BonusDamage;

	int Random(int count) => count <= 0 ? 0 : random(0, count - 1);

	#region Party actions

	/// <summary>
	/// A ranged weapon in the right hand needs ammunition of the same type in the left hand.
	/// Returns null without a ranged weapon.
	/// </summary>
	static bool? HasRangedWeaponWithAmmunition(IBattleCharacter character)
	{
		var weapon = character.Equipment[EquipmentSlot.RightHand];

		if (weapon.Count == 0 || weapon.Item?.Type != ItemType.LongRangedWeapon)
			return null;

		var ammunition = character.Equipment[EquipmentSlot.LeftHand];

		return ammunition.Count != 0 && ammunition.Item?.Type == ItemType.Ammunition && ammunition.Item.AmmoType == weapon.Item.UsedAmmoType;
	}

	/// <summary>
	/// The free neighbor cells in the party rows which no other member wants to move to (L162ac in the original).
	/// </summary>
	public IEnumerable<Position> GetMoveTargets(int slot)
	{
		var partyMember = GetPartyMember(slot);

		if (partyMember == null || !IsOnBattleField(partyMember))
			yield break;

		foreach (var offset in Neighbors)
		{
			var target = partyMember.Position + offset;

			if (target.Y < PartyFrontRow || !IsFree(target.X, target.Y))
				continue;

			if (partyActions.Any(action => action.Key != slot && action.Value.Type == BattleActionType.Move && action.Value.Target == target))
				continue;

			yield return target;
		}
	}

	/// <summary>
	/// Melee weapons reach the monsters in front of the member and diagonal in front of it which
	/// is only possible from the front row. Ranged weapons reach every monster (L1634c in the original).
	/// </summary>
	public IEnumerable<Position> GetAttackTargets(int slot, out bool ranged)
	{
		ranged = false;
		var partyMember = GetPartyMember(slot);

		if (partyMember == null || !IsOnBattleField(partyMember))
			return [];

		var rangedWeapon = HasRangedWeaponWithAmmunition(partyMember.Character);

		if (rangedWeapon == false)
			return [];

		if (rangedWeapon == true)
		{
			ranged = true;
			return Monsters.Select(monster => monster.Position).ToList();
		}

		return GetMeleeTargets(partyMember.Position).ToList();
	}

	IEnumerable<Position> GetMeleeTargets(Position position)
	{
		foreach (var offset in MeleeOffsets)
		{
			var target = position + offset;

			if (target.Y == MonsterFrontRow && IsValid(target) && this[target] is MonsterParticipant)
				yield return target;
		}
	}

	/// <summary>
	/// Members which are asleep, mad or panicking can't choose their action.
	/// </summary>
	public bool CanChooseAction(int slot)
	{
		var partyMember = GetPartyMember(slot);

		return partyMember != null && IsOnBattleField(partyMember) &&
			!partyMember.HasAnyConditionOf(Condition.Sleeping | Condition.Mad | Condition.Panicked);
	}

	/// <summary>
	/// Only members in the back row can flee.
	/// </summary>
	public bool CanFlee(int slot) => CanChooseAction(slot) && !GetPartyMember(slot)!.HasAnyConditionOf(Condition.Stunned) &&
		GetPartyMember(slot)!.Position.Y == PartyBackRow;

	public bool CanMove(int slot) => CanChooseAction(slot) && !GetPartyMember(slot)!.HasAnyConditionOf(Condition.Stunned) && GetMoveTargets(slot).Any();

	public bool CanAttack(int slot) => CanChooseAction(slot) && !GetPartyMember(slot)!.HasAnyConditionOf(Condition.Stunned) &&
		WeaponDamage(GetPartyMember(slot)!.Character) > 0 && GetAttackTargets(slot, out _).Any();

	public bool CanParry(int slot) => CanChooseAction(slot) && !GetPartyMember(slot)!.HasAnyConditionOf(Condition.Stunned);

	/// <summary>
	/// Irritated members and members without spell schools can't cast spells.
	/// </summary>
	public bool CanCastSpells(int slot) => CanChooseAction(slot) && !GetPartyMember(slot)!.HasAnyConditionOf(Condition.Irritated) &&
		GetPartyMember(slot)!.PartyMember.LearnedSpellSchools != 0;

	/// <summary>
	/// Resets the actions for the next round and chooses the actions of members which can't act on their own (L16a66 in the original).
	/// </summary>
	public void PrepareRound()
	{
		foreach (var partyMember in partyMembers)
		{
			int slot = partyMember.Slot;
			var action = GetPartyAction(slot);

			// Parry, flee and attacks of still existing targets are kept.
			if (action.Type == BattleActionType.Attack ? this[action.Target] == null : action.Type is not (BattleActionType.Parry or BattleActionType.Flee))
				action = BattleAction.Parry;

			bool onBattleField = IsOnBattleField(partyMember);

			if (partyMember.HasAnyConditionOf(Condition.Petrified))
				action = new(BattleActionType.Petrified);
			else if (onBattleField)
			{
				if (partyMember.HasAnyConditionOf(Condition.Stunned))
					action = new(BattleActionType.Stunned);
				else if (partyMember.HasAnyConditionOf(Condition.Sleeping))
					action = BattleAction.None;
				else if (partyMember.HasAnyConditionOf(Condition.Panicked))
					action = ChoosePanicAction(partyMember);
				else if (partyMember.HasAnyConditionOf(Condition.Mad))
					action = ChooseMadAction(partyMember);
			}

			partyActions[slot] = action;
		}
	}

	/// <summary>
	/// Panicking members flee from the back row or move back (L16b14 in the original).
	/// </summary>
	BattleAction ChoosePanicAction(PartyMemberParticipant partyMember)
	{
		var (column, row) = partyMember.Position;

		if (row == PartyBackRow)
			return new(BattleActionType.Flee);

		foreach (int targetColumn in new[] { column, column - 1, column + 1 })
		{
			if (IsFree(targetColumn, PartyBackRow))
				return new(BattleActionType.Move, new(targetColumn, PartyBackRow));
		}

		return BattleAction.Parry;
	}

	/// <summary>
	/// Mad members attack, flee or move randomly (L16b9a in the original).
	/// </summary>
	BattleAction ChooseMadAction(PartyMemberParticipant partyMember)
	{
		int value = Random(32768);
		int choice = value % 37;

		if ((choice & 1) != 0)
		{
			// Attack the first target in reach (not with ranged weapons).
			if (WeaponDamage(partyMember.Character) <= 0 || HasRangedWeaponWithAmmunition(partyMember.Character) != null)
				return BattleAction.Parry;

			var targets = GetMeleeTargets(partyMember.Position).ToList();

			return targets.Count == 0 ? BattleAction.Parry : new(BattleActionType.Attack, targets[0]);
		}

		if (partyMember.Position.Y == PartyBackRow && (choice & 4) == 0)
			return new(BattleActionType.Flee);

		var moveTargets = GetMoveTargets(partyMember.Slot).ToList();
		int index = value % 5;

		return index < moveTargets.Count ? new(BattleActionType.Move, moveTargets[index]) : BattleAction.Parry;
	}

	#endregion

	#region Monster actions

	/// <summary>
	/// Chooses the action of a monster (L16fe0 in the original).
	/// </summary>
	BattleAction ChooseMonsterAction(MonsterParticipant monster, HashSet<Position> reservedCells, IReadOnlyDictionary<int, int> currentMonsterCounts)
	{
		if (partyAdvantage)
			return BattleAction.None;

		if (monster.HasAnyConditionOf(Condition.Stunned))
			return ChooseMonsterFightAction(monster, reservedCells);

		if (monster.HasAnyConditionOf(Condition.Sleeping))
			return BattleAction.None;

		bool retreat = monster.HasAnyConditionOf(Condition.Panicked);

		if (!retreat)
		{
			// The monsters retreat if too many monsters of their type were defeated.
			int initialCount = initialMonsterCounts[monster.MonsterIndex];
			int lostPercentage = 100 - currentMonsterCounts.GetValueOrDefault(monster.MonsterIndex) * 100 / initialCount;
			retreat = monster.Monster.Morale <= lostPercentage;
		}

		if (retreat)
			return ChooseMonsterRetreat(monster, reservedCells);

		var (column, row) = monster.Position;
		// The monsters approach up to their preferred distance from the front row.
		// When the front row was empty once, all monsters approach the front row.
		int wantedRow = monsterFrontRowWasEmpty ? MonsterFrontRow : MonsterFrontRow - (sbyte)monster.Monster.BattlePosition;

		if (wantedRow != row)
		{
			var move = TryMonsterMove(column, row + Math.Sign(wantedRow - row), reservedCells);

			if (move != null)
				return move;
		}

		return ChooseMonsterFightAction(monster, reservedCells);
	}

	/// <summary>
	/// Monsters flee from the back row and move back otherwise (L1706e in the original).
	/// </summary>
	BattleAction ChooseMonsterRetreat(MonsterParticipant monster, HashSet<Position> reservedCells)
	{
		var (column, row) = monster.Position;

		if (row == 0)
			return new(BattleActionType.Flee);

		return TryMonsterMove(column, row - 1, reservedCells) ?? BattleAction.Parry;
	}

	/// <summary>
	/// The monster moves to the given row: to the same column or one beside it (L170aa in the original).
	/// </summary>
	BattleAction? TryMonsterMove(int column, int row, HashSet<Position> reservedCells)
	{
		int targetColumn = column;

		if (!IsFree(column, row))
		{
			targetColumn = column - 1;

			if (targetColumn < 0 || !IsFree(targetColumn, row))
			{
				targetColumn = column + 1;

				if (targetColumn == Columns || !IsFree(targetColumn, row))
					return null;
			}
		}

		var target = new Position(targetColumn, row);

		return reservedCells.Add(target) ? new(BattleActionType.Move, target) : null;
	}

	/// <summary>
	/// Spells, ranged attacks or melee attacks of monsters (L170fa in the original).
	/// </summary>
	BattleAction ChooseMonsterFightAction(MonsterParticipant monster, HashSet<Position> reservedCells)
	{
		var character = monster.Monster;

		if (!monster.HasAnyConditionOf(Condition.Irritated) && Round < LastMonsterSpellRound)
		{
			// A random spell slot is chosen. Slots can be empty.
			int index = Random(25);
			var spells = character.SpellSlots;

			if (spells[index] is MonsterSpell spell && probe(character.SpellCastChance))
				return new(BattleActionType.Magic, SpellSchool: spell.School, SpellNumber: spell.SpellIndex);
		}

		if (monster.HasAnyConditionOf(Condition.Stunned))
			return BattleAction.None;

		var rangedWeapon = HasRangedWeaponWithAmmunition(character);

		if (rangedWeapon == true)
			return new(BattleActionType.Attack, GetRandomPartyPosition(), true);

		// Ranged weapons without ammunition make the monster retreat.
		if (rangedWeapon == false)
			return ChooseMonsterRetreat(monster, reservedCells);

		var (column, row) = monster.Position;

		if (row != MonsterFrontRow)
			return BattleAction.Parry;

		foreach (int targetColumn in new[] { column, column - 1, column + 1 })
		{
			if (IsValid(new(targetColumn, PartyFrontRow)) && this[targetColumn, PartyFrontRow] != null)
				return new(BattleActionType.Attack, new(targetColumn, PartyFrontRow));
		}

		// Move sideways towards party members in the front row.
		bool PartyInColumns(int from, int to) => Enumerable.Range(from, Math.Max(0, to - from + 1)).Any(c => this[c, PartyFrontRow] != null);

		if (column > 0 && PartyInColumns(0, column - 1) && IsFree(column - 1, row) && reservedCells.Add(new(column - 1, row)))
			return new(BattleActionType.Move, new(column - 1, row));

		if (column < Columns - 1 && PartyInColumns(column + 1, Columns - 1) && IsFree(column + 1, row) && reservedCells.Add(new(column + 1, row)))
			return new(BattleActionType.Move, new(column + 1, row));

		return BattleAction.Parry;
	}

	/// <summary>
	/// A random cell of a party member (L175c4 in the original).
	/// </summary>
	Position GetRandomPartyPosition()
	{
		int index = Random(0x1000);

		while (true)
		{
			index %= 2 * Columns;

			if (this[index % Columns, PartyFrontRow + index / Columns] != null)
				return new(index % Columns, PartyFrontRow + index / Columns);

			index++;
		}
	}

	#endregion

	#region Round

	sealed record PlannedAction(BattleParticipant Participant, BattleAction Action, int Initiative);

	/// <summary>
	/// Executes a round: the monsters choose their actions and all actions are executed by initiative.
	/// </summary>
	public List<BattleEvent> ExecuteRound()
	{
		var events = new List<BattleEvent>();

		// e.g. killed by party spells which are executed by the game
		RemoveDefeated(events);

		if (!Monsters.Any(monster => monster.Position.Y == MonsterFrontRow))
			monsterFrontRowWasEmpty = true;

		var reservedCells = new HashSet<Position>();
		var currentMonsterCounts = Monsters.GroupBy(monster => monster.MonsterIndex).ToDictionary(group => group.Key, group => group.Count());
		var plannedActions = new List<PlannedAction>();

		// The monsters in grid order, then the party members.
		foreach (var monster in Monsters.ToList())
			plannedActions.Add(new(monster, ChooseMonsterAction(monster, reservedCells, currentMonsterCounts), monster.Speed));

		partyAdvantage = false;

		foreach (var participant in Participants.OfType<PartyMemberParticipant>().ToList())
			plannedActions.Add(new(participant, GetPartyAction(participant.Slot), participant.Speed));

		// Stable sort by initiative, the fastest act first (from the end of the sorted list).
		roundActions.Clear();

		foreach (var planned in plannedActions)
			roundActions[planned.Participant] = planned.Action;

		var order = plannedActions.Select((action, index) => (action, index))
			.OrderBy(entry => entry.action.Initiative).ThenBy(entry => entry.index)
			.Select(entry => entry.action).Reverse().ToList();

		foreach (var planned in order)
		{
			if (!IsOnBattleField(planned.Participant))
				continue;

			ExecuteAction(planned.Participant, planned.Action, events);
			RemoveDefeated(events);

			if (CheckBattleEnd())
				break;
		}

		Round++;
		MonsterAdvantage = false;

		if (Result == BattleResult.Ongoing)
			EndRound(events);

		return events;
	}

	/// <summary>
	/// Moves are not kept. If the party's front row is empty, the back row moves to the front (L174bc in the original).
	/// </summary>
	void EndRound(List<BattleEvent> events)
	{
		foreach (var (slot, action) in partyActions.ToList())
		{
			if (action.Type == BattleActionType.Move)
				partyActions[slot] = BattleAction.Parry;
		}

		var frontRow = Enumerable.Range(0, Columns).Select(column => this[column, PartyFrontRow]).ToList();

		if (frontRow.Any(participant => participant != null))
		{
			shiftBackRowNextRound = false;
			return;
		}

		var backRow = Enumerable.Range(0, Columns).Select(column => this[column, PartyBackRow]).OfType<BattleParticipant>().ToList();

		if (backRow.Count > 1 || shiftBackRowNextRound)
		{
			foreach (var participant in backRow)
			{
				var from = participant.Position;
				MoveTo(participant, new(from.X, PartyFrontRow));
				events.Add(new BattleMoveEvent(participant, from, participant.Position));
			}

			events.Add(new BattleTextEvent(null, Message.OpponentsAreClosingIn));
			shiftBackRowNextRound = false;
		}
		else
		{
			shiftBackRowNextRound = true;
		}
	}

	/// <summary>
	/// Dead monsters and dead or petrified party members leave the battle field (L17392 in the original).
	/// </summary>
	void RemoveDefeated(List<BattleEvent> events)
	{
		foreach (var monster in Monsters.ToList())
		{
			if (monster.HasAnyConditionOf(Condition.Dead))
			{
				Remove(monster);
				defeatedMonsters.Add(monster);
				Experience += monster.Monster.DefeatExperience;
				events.Add(new BattleRemoveEvent(monster));
			}
		}

		foreach (var partyMember in Participants.OfType<PartyMemberParticipant>().ToList())
		{
			if (partyMember.HasAnyConditionOf(Condition.Petrified))
			{
				Remove(partyMember);
				partyActions[partyMember.Slot] = new(BattleActionType.Petrified);
				partyMember.Character.AddCondition(Condition.Fled);
				events.Add(new BattleRemoveEvent(partyMember));
			}
			else if (partyMember.HasAnyConditionOf(Condition.Dead))
			{
				Remove(partyMember);
				events.Add(new BattleRemoveEvent(partyMember));
			}
		}
	}

	/// <summary>
	/// The battle ends if all monsters are gone or the active member can't act and no other member
	/// can take over (0x6fe and L17432 in the original).
	/// </summary>
	bool CheckBattleEnd()
	{
		if (!Monsters.Any())
		{
			Result = BattleResult.Won;
			return true;
		}

		var activeMember = GetPartyMember(ActiveSlot);

		if (activeMember == null || activeMember.HasAnyConditionOf(OutOfBattleConditions | Condition.Petrified))
		{
			if (partyMembers.Any(partyMember => !partyMember.HasAnyConditionOf(CannotLeadConditions)))
			{
				NewActiveMemberNeeded = true;
				return false;
			}

			Result = activeMember?.HasAnyConditionOf(Condition.Fled) == true || fledMembers != 0 ? BattleResult.PartyFled : BattleResult.PartyDefeated;
			return true;
		}

		return false;
	}

	/// <summary>
	/// Diseases, irritation and blindness lower the chance to hit (L1733c in the original).
	/// </summary>
	static int GetSuccessFactor(BattleParticipant participant)
	{
		int factor = 100;

		if (participant.HasAnyConditionOf(Condition.Diseased))
			factor /= 2;

		if (participant.HasAnyConditionOf(Condition.Irritated))
			factor = factor * 75 / 100;

		if (participant.HasAnyConditionOf(Condition.Blind))
			factor /= 4;

		return factor;
	}

	void ExecuteAction(BattleParticipant actor, BattleAction action, List<BattleEvent> events)
	{
		bool cannotAct = actor.HasAnyConditionOf(Condition.Stunned | Condition.Sleeping);

		switch (action.Type)
		{
			case BattleActionType.Move:
				if (!cannotAct)
					Move(actor, action.Target, events);
				break;
			case BattleActionType.Flee:
				if (!cannotAct)
					Flee(actor, events);
				break;
			case BattleActionType.Attack:
				if (!cannotAct && !actor.HasAnyConditionOf(Condition.Panicked))
					Attack(actor, action, GetSuccessFactor(actor), events);
				break;
			case BattleActionType.Magic:
				if (!actor.HasAnyConditionOf(Condition.Irritated | Condition.Panicked | Condition.Sleeping))
					CastSpell(actor, action, events);
				break;
		}
	}

	void Move(BattleParticipant actor, Position target, List<BattleEvent> events)
	{
		if (this[target] != null)
			return;

		var from = actor.Position;
		MoveTo(actor, target);
		events.Add(new BattleMoveEvent(actor, from, target));
		events.Add(new BattleTextEvent(actor, Message.ChangesLocation));
	}

	/// <summary>
	/// Dexterity and luck against a random value below 150 (L1768a in the original).
	/// </summary>
	void Flee(BattleParticipant actor, List<BattleEvent> events)
	{
		var character = actor.Character;
		int value = Attribute(character, GameData.Attribute.Dexterity) + Attribute(character, GameData.Attribute.Luck);
		bool success = value >= 0 && (value >= 150 || value >= Random(150));

		if (!success)
		{
			events.Add(new BattleTextEvent(actor, Message.FailsToFlee));
			return;
		}

		Remove(actor);

		if (actor is MonsterParticipant monster)
		{
			// Fled monsters give experience as well.
			Experience += monster.Monster.DefeatExperience;
		}
		else
		{
			character.AddCondition(Condition.Fled);
			fledMembers++;
		}

		events.Add(new BattleRemoveEvent(actor));
		events.Add(new BattleTextEvent(actor, Message.FleesSuccessfully));
	}

	#endregion

	#region Attack

	/// <summary>
	/// Executes the attacks of a participant (L17734 in the original).
	/// </summary>
	void Attack(BattleParticipant attacker, BattleAction action, int successFactor, List<BattleEvent> events)
	{
		var defender = this[action.Target];

		if (defender == null)
		{
			events.Add(new BattleTextEvent(attacker, Message.MissesTheTarget));
			return;
		}

		var attackerData = attacker.Character;
		int attacks = Math.Max(1, (int)attackerData.AttacksPerRound);

		// Haste doubles the attacks once.
		if (attacker is PartyMemberParticipant partyMember && hastedSlots.Remove(partyMember.Slot))
			attacks *= 2;

		// Some monsters can only be hurt by magical weapons.
		if (defender is MonsterParticipant monsterDefender && attackerData.MagicBonusWeapon < monsterDefender.Monster.MagicHitBonus)
		{
			events.Add(new BattleTextEvent(attacker, Message.AttackHitsButWeaponPowerless));
			return;
		}

		for (int i = 0; i < attacks; i++)
		{
			if (!IsOnBattleField(defender) || defender.HasAnyConditionOf(Condition.Dead))
				break;

			AttackOnce(attacker, defender, successFactor, events);

			if (action.Ranged && !ReloadAmmunition(attacker))
			{
				if (attacker is PartyMemberParticipant member)
					partyActions[member.Slot] = BattleAction.Parry;

				break;
			}
		}
	}

	/// <summary>
	/// One attack (L177e0 in the original).
	/// </summary>
	void AttackOnce(BattleParticipant attacker, BattleParticipant defender, int successFactor, List<BattleEvent> events)
	{
		var attackerData = attacker.Character;
		var defenderData = defender.Character;

		if (attacker is MonsterParticipant monster)
			events.Add(new BattleAttackAnimationEvent(monster));

		// hit
		int attack = Skill(attackerData, GameData.Skill.Attack);
		attack = attacker.Side == BattleSide.Party ? (attack + WeaponPower) / 2 * 2 : attack * 2;

		if (!probe(attack * successFactor / 100))
		{
			events.Add(new BattleTextEvent(attacker, Message.AttackMisses));
			return;
		}

		// Only defenders who parry this round can parry.
		if (roundActions.GetValueOrDefault(defender)?.Type == BattleActionType.Parry)
		{
			int parry = Skill(defenderData, GameData.Skill.Parry);

			if (defender.Side == BattleSide.Party)
				parry += ArmorProtection;

			if (probe(parry))
			{
				events.Add(new BattleTextEvent(defender, Message.AttackParried));
				return;
			}
		}

		// damage
		int damageRange = WeaponDamage(attackerData) + Attribute(attackerData, GameData.Attribute.Strength) / 25;
		int damage = Random(damageRange);
		int defenseRange = defenderData.Defense + (short)defenderData.BonusDefense + defenderData.MagicBonusArmor +
			Attribute(defenderData, GameData.Attribute.Stamina) / 25;
		damage -= Random(defenseRange);

		if (damage <= 0)
		{
			events.Add(new BattleTextEvent(attacker, Message.AttackHitsButNoDamage));
			return;
		}

		events.Add(new BattleTextEvent(attacker, Message.Hits, defender, damage));
		Damage(defender, damage, events);
	}

	/// <summary>
	/// Damages a participant. Damaged participants wake up.
	/// </summary>
	void Damage(BattleParticipant target, int damage, List<BattleEvent> events)
	{
		var character = target.Character;

		character.RemoveCondition(Condition.Sleeping);

		int hitPoints = character.HitPoints.CurrentValue - damage;

		if (hitPoints <= 0)
		{
			character.HitPoints.CurrentValue = 0;
			character.AddCondition(Condition.Dead);
		}
		else
		{
			character.HitPoints.CurrentValue = (word)hitPoints;
		}

		events.Add(new BattleDamageEvent(target, damage));
	}

	/// <summary>
	/// Ranged attacks use the ammunition in the left hand. New ammunition is taken from the inventory.
	/// Returns false if there is no ammunition left.
	/// </summary>
	static bool ReloadAmmunition(BattleParticipant attacker)
	{
		var character = attacker.Character;
		var weapon = character.Equipment[EquipmentSlot.RightHand].Item;
		var ammunitionSlot = character.Equipment[EquipmentSlot.LeftHand];
		var ammunition = ammunitionSlot.Item;

		if (weapon == null || ammunition == null || ammunitionSlot.Count == 0)
			return false;

		if (attacker is PartyMemberParticipant partyMember)
		{
			Game.DestroyUsedItem(partyMember.PartyMember, ammunitionSlot, true);

			if (ammunitionSlot.Count != 0)
				return true;
		}
		else
		{
			ammunitionSlot.ClearItem();
		}

		// Take ammunition of the same type from the inventory.
		var inventorySlot = character.Inventory.FirstOrDefault(slot => slot.Count != 0 && slot.Item?.Type == ItemType.Ammunition &&
			slot.Item.AmmoType == weapon.UsedAmmoType);

		if (inventorySlot == null)
			return false;

		var item = inventorySlot.Item!;
		ammunitionSlot.SetItem(item, 1);

		if (--inventorySlot.Count == 0)
			inventorySlot.ClearItem();

		if (attacker is PartyMemberParticipant)
			Game.ApplyEquipmentBoni((IPartyMember)character, item, true);

		return true;
	}

	#endregion

	#region Magic

	/// <summary>
	/// Casts a spell of a monster or a party member.
	/// </summary>
	void CastSpell(BattleParticipant caster, BattleAction action, List<BattleEvent> events)
	{
		if (caster is PartyMemberParticipant partyMember)
		{
			CastPartySpell(partyMember, action, events);
			return;
		}

		var monster = (MonsterParticipant)caster;

		var spellData = assetProvider.SpellDataLoader.LoadSpellData(action.SpellSchool, action.SpellNumber);
		var character = monster.Monster;

		events.Add(new BattleAttackAnimationEvent(monster));

		character.SpellPoints.CurrentValue = (word)Math.Max(0, character.SpellPoints.CurrentValue - spellData.SpellPointCost);
		bool success = probe(Skill(character, GameData.Skill.UseMagic));
		events.Add(new BattleSpellEvent(caster, action.SpellSchool, action.SpellNumber, success));

		if (!success)
			return;

		if (action.SpellSchool == SpellSchool.Black && action.SpellNumber >= 1 && action.SpellNumber <= 20)
			CastMonsterDamageSpell(action.SpellNumber, spellData, events);
		else if (action.SpellSchool == SpellSchool.Special)
			CastMonsterConditionSpell((SpecialSpell)action.SpellNumber, events);
	}

	/// <summary>
	/// Activates the armour protection, weapon power or anti-magic spell (type, value, duration in steps of 5 minutes).
	/// </summary>
	public Action<Game.ActiveSpellType, int, int>? ActivateSpell { get; set; }

	static readonly int[] ActiveSpellValues = [5, 10, 15];
	static readonly int[] ActiveSpellDurations = [24, 96, 192];
	// conditions which are healed by the white spells 10-19 (the 6th one wakes up the whole party)
	static readonly Condition[] CuredConditions =
	[
		Condition.Poisoned, Condition.Stunned, Condition.Diseased, Condition.Aging, Condition.Petrified,
		Condition.Sleeping, Condition.Panicked, Condition.Irritated, Condition.Blind, Condition.Mad
	];
	const int FirstCureSpell = 10;
	const int LastCureSpell = 19;
	const int SalvationSpell = 6;
	const int SleepSpell = 21;
	const int DestroyUndeadSpell = 25;
	const int HolyWordSpell = 26;
	const int HasteSpell = 23;
	const int MassHasteSpell = 24;
	const int FirstActiveSpell = 4;
	const int LastActiveSpell = 12;
	const int DissolveSpell = 21;
	const int MagicArrowsSpell = 22;
	const int MagicArrowsDamage = 8;
	const int MagicArrowsAnimation = 10;
	const int SleepAnimation = 5;
	static readonly Dictionary<int, (Condition Condition, Message Message, int Animation)> MonsterConditionWhiteSpells = new()
	{
		{ 20, (Condition.Stunned, Message.IsStunned, 4) },
		{ 22, (Condition.Panicked, Message.Panics, 6) },
		{ 23, (Condition.Irritated, Message.IsIrritated, 7) },
		{ 24, (Condition.Blind, Message.GoesBlind, 14) },
	};

	/// <summary>
	/// Party spells use the spell points and probe the skill to use magic (L18b28 in the original).
	/// The targets were chosen with the action (L18936 and the spell routines in the original).
	/// </summary>
	void CastPartySpell(PartyMemberParticipant caster, BattleAction action, List<BattleEvent> events)
	{
		var spellData = assetProvider.SpellDataLoader.LoadSpellData(action.SpellSchool, action.SpellNumber);
		var character = caster.PartyMember;

		character.SpellPoints.CurrentValue = (word)Math.Max(0, character.SpellPoints.CurrentValue - spellData.SpellPointCost);
		bool success = probe(Skill(character, GameData.Skill.UseMagic));
		events.Add(new BattleSpellEvent(caster, action.SpellSchool, action.SpellNumber, success));

		// The spell name is shown first and the result after the effects of the spell (L17ba4 in the original).
		if (!success)
		{
			events.Add(new BattleTextEvent(caster, Message.SpellCastFails));
			return;
		}

		CastPartySpellEffects(action, spellData, events);
		events.Add(new BattleTextEvent(caster, Message.SpellCastSucceeds));
	}

	void CastPartySpellEffects(BattleAction action, ISpellData spellData, List<BattleEvent> events)
	{
		var targets = BattleAction.GetSpellTarget(spellData) == SpellTarget.AllMonsters
			? Monsters.ToList<BattleParticipant>()
			: (action.Targets ?? []).Where(target => target.Side == BattleSide.Party || IsOnBattleField(target)).ToList();
		var aliveMembers = partyMembers.Where(partyMember => !partyMember.HasAnyConditionOf(OutOfBattleConditions)).ToList();

		switch (action.SpellSchool)
		{
			case SpellSchool.White:
				CastWhiteSpell(action.SpellNumber, targets, aliveMembers, events);
				break;
			case SpellSchool.Gray:
				CastGraySpell(action.SpellNumber, targets, aliveMembers);
				break;
			case SpellSchool.Black:
				CastBlackSpell(action.SpellNumber, spellData, targets, events);
				break;
		}
	}

	void CastWhiteSpell(int spell, List<BattleParticipant> targets, List<PartyMemberParticipant> aliveMembers, List<BattleEvent> events)
	{
		bool Alive(BattleParticipant target) => !target.HasAnyConditionOf(OutOfBattleConditions);

		if (spell >= 1 && spell <= 5)
		{
			// 1-5, 2-9, 4-19, 8-39 and 16-79 hit points
			int level = spell - 1;
			int amount = level == 0 ? 1 + Random(5) : (1 << level) + Random(4 << level);

			foreach (var target in targets.Where(Alive))
				target.Character.HealHitPoints((word)amount);
		}
		else if (spell == SalvationSpell)
		{
			foreach (var partyMember in aliveMembers)
				partyMember.Character.HealHitPoints((word)(4 + Random(16)));
		}
		else if (spell >= FirstCureSpell && spell <= LastCureSpell)
		{
			var condition = CuredConditions[spell - FirstCureSpell];

			if (condition == Condition.Sleeping)
			{
				foreach (var partyMember in aliveMembers)
					partyMember.Character.RemoveCondition(Condition.Sleeping);
			}
			else
			{
				foreach (var target in targets.Where(Alive))
					target.Character.RemoveCondition(condition);
			}
		}
		else if (spell == SleepSpell)
		{
			foreach (var target in targets.OfType<MonsterParticipant>())
			{
				if (!ResistsPartySpell(target, events))
				{
					target.Character.AddCondition(Condition.Sleeping);
					events.Add(new BattleSpellAnimationEvent(target, SleepAnimation));
					events.Add(new BattleTextEvent(target, Message.FallsAsleep));
				}
			}
		}
		else if (spell == DestroyUndeadSpell || spell == HolyWordSpell)
		{
			// only undead monsters are affected
			foreach (var target in targets.OfType<MonsterParticipant>().Where(target => target.Monster.MonsterFlags.HasFlag(MonsterFlags.Undead)))
			{
				if (ResistsSpell(target, false))
					events.Add(new BattleTextEvent(target, Message.ResistsTheSpell));
				else
					target.Character.AddCondition(Condition.Dead);
			}
		}
		else if (MonsterConditionWhiteSpells.TryGetValue(spell, out var effect))
		{
			foreach (var target in targets.OfType<MonsterParticipant>())
			{
				if (!ResistsPartySpell(target, events))
				{
					target.Character.AddCondition(effect.Condition);
					events.Add(new BattleSpellAnimationEvent(target, effect.Animation));
					events.Add(new BattleTextEvent(target, effect.Message));
				}
			}
		}
	}

	/// <summary>
	/// Monsters resist with their anti-magic. Some monsters are immune to the condition spells (L18dbc in the original).
	/// </summary>
	bool ResistsPartySpell(MonsterParticipant target, List<BattleEvent> events)
	{
		if (ResistsSpell(target, false))
		{
			events.Add(new BattleTextEvent(target, Message.ResistsTheSpell));
			return true;
		}

		if (target.Monster.MonsterFlags.HasFlag(MonsterFlags.Boss))
		{
			events.Add(new BattleTextEvent(target, Message.IsImmuneToTheSpell));
			return true;
		}

		return false;
	}

	void CastGraySpell(int spell, List<BattleParticipant> targets, List<PartyMemberParticipant> aliveMembers)
	{
		if (spell >= FirstActiveSpell && spell <= LastActiveSpell)
		{
			// armour protection, weapon power and anti-magic with 3 levels each
			int index = spell - FirstActiveSpell;
			var type = (Game.ActiveSpellType)(1 + index / 3);
			int value = ActiveSpellValues[index % 3];

			if (type == Game.ActiveSpellType.ArmorProtection)
				ArmorProtection = value;
			else if (type == Game.ActiveSpellType.WeaponPower)
				WeaponPower = value;
			else
				AntiMagic = value;

			ActivateSpell?.Invoke(type, value, ActiveSpellDurations[index % 3]);
		}
		else if (spell == HasteSpell)
		{
			foreach (var target in targets.OfType<PartyMemberParticipant>())
				hastedSlots.Add(target.Slot);
		}
		else if (spell == MassHasteSpell)
		{
			foreach (var partyMember in aliveMembers)
				hastedSlots.Add(partyMember.Slot);
		}
	}

	/// <summary>
	/// Damage spells, dissolve and magic arrows on monsters (L19162 in the original).
	/// </summary>
	void CastBlackSpell(int spellNumber, ISpellData spellData, List<BattleParticipant> targets, List<BattleEvent> events)
	{
		foreach (var target in targets.OfType<MonsterParticipant>())
		{
			if (ResistsSpell(target, false))
			{
				events.Add(new BattleTextEvent(target, Message.ResistsTheSpell));
				continue;
			}

			if (spellNumber == DissolveSpell)
			{
				target.Character.AddCondition(Condition.Dead);
				continue;
			}

			if (spellNumber == MagicArrowsSpell)
			{
				Damage(target, MagicArrowsDamage, events);
				events.Add(new BattleSpellAnimationEvent(target, MagicArrowsAnimation));
				continue;
			}

			var immunity = assetProvider.CombatData.GetElementImmunity(spellData.LocationFlags >> 6);

			if (target.Monster.ElementalFlags.HasFlag(immunity))
			{
				events.Add(new BattleTextEvent(target, Message.IsImmuneToTheSpell));
				continue;
			}

			var damageRange = assetProvider.CombatData.GetSpellDamage(spellNumber);
			int damage = damageRange.Min + 1 + Random(damageRange.Max - damageRange.Min);

			// vulnerable monsters get double damage
			if (target.Monster.ElementalFlags.HasFlag((MonsterElementalFlags)((int)immunity << 4)))
				damage *= 2;

			Damage(target, damage, events);
			events.Add(new BattleSpellAnimationEvent(target, damageRange.Animation));
		}
	}

	IEnumerable<PartyMemberParticipant> GetSpellTargets(ISpellData spellData)
	{
		var partyOnBattleField = Participants.OfType<PartyMemberParticipant>().ToList();

		if ((spellData.TargetFlags & 0x10) != 0)
			return partyOnBattleField;

		if ((spellData.TargetFlags & 0x08) != 0)
		{
			int row = Random(2) == 0 ? PartyFrontRow : PartyBackRow;
			return partyOnBattleField.Where(partyMember => partyMember.Position.Y == row);
		}

		return [(PartyMemberParticipant)this[GetRandomPartyPosition()]!];
	}

	/// <summary>
	/// The anti-magic spell only protects the party.
	/// </summary>
	bool ResistsSpell(BattleParticipant target, bool withAntiMagicSpell = true) =>
		probe(Attribute(target.Character, GameData.Attribute.AntiMagic) + (withAntiMagicSpell ? AntiMagic : 0));

	/// <summary>
	/// Black magic damage spells of monsters (L19212 in the original).
	/// </summary>
	void CastMonsterDamageSpell(int spellNumber, ISpellData spellData, List<BattleEvent> events)
	{
		var damage = assetProvider.CombatData.GetSpellDamage(spellNumber);

		foreach (var target in GetSpellTargets(spellData).ToList())
		{
			if (ResistsSpell(target))
			{
				events.Add(new BattleTextEvent(target, Message.ResistsTheSpell));
				continue;
			}

			Damage(target, damage.Min + 1 + Random(damage.Max - damage.Min), events);
		}
	}

	static readonly Dictionary<SpecialSpell, (Condition Condition, Message Message)> MonsterConditionSpells = new()
	{
		{ SpecialSpell.Stun, (Condition.Stunned, Message.IsStunned) },
		{ SpecialSpell.Poison, (Condition.Poisoned, Message.IsPoisoned) },
		{ SpecialSpell.FleshToStone, (Condition.Petrified, Message.IsPetrified) },
		{ SpecialSpell.MakeIll, (Condition.Diseased, Message.BecomesIll) },
		{ SpecialSpell.Aging, (Condition.Aging, Message.AgesRapidly) },
		{ SpecialSpell.Irritation, (Condition.Irritated, Message.IsIrritated) },
		{ SpecialSpell.MakeMad, (Condition.Mad, Message.GoesMad) },
		{ SpecialSpell.Panic, (Condition.Panicked, Message.Panics) },
		{ SpecialSpell.BlindingFlash, (Condition.Blind, Message.GoesBlind) },
	};

	/// <summary>
	/// Condition spells of monsters on a random party member and sleep on the whole party (L19360 and L193f4 in the original).
	/// </summary>
	void CastMonsterConditionSpell(SpecialSpell spell, List<BattleEvent> events)
	{
		if (spell == SpecialSpell.Sleep)
		{
			foreach (var target in Participants.OfType<PartyMemberParticipant>().ToList())
			{
				if (ResistsSpell(target))
				{
					events.Add(new BattleTextEvent(target, Message.ResistsTheSpell));
					continue;
				}

				target.Character.AddCondition(Condition.Sleeping);
				events.Add(new BattleTextEvent(target, Message.FallsAsleep));
			}

			return;
		}

		if (!MonsterConditionSpells.TryGetValue(spell, out var effect))
			return;

		var victim = this[GetRandomPartyPosition()]!;

		if (ResistsSpell(victim))
		{
			events.Add(new BattleTextEvent(victim, Message.ResistsTheSpell));
			return;
		}

		victim.Character.AddCondition(effect.Condition);
		events.Add(new BattleTextEvent(victim, effect.Message));
	}

	#endregion

	#region After the battle

	/// <summary>
	/// Removes the battle conditions and distributes the experience among the members
	/// which are neither dead nor fled. Returns the experience per member.
	/// </summary>
	public int Finish()
	{
		var receivers = partyMembers.Where(partyMember => !partyMember.HasAnyConditionOf(OutOfBattleConditions)).ToList();
		int experience = receivers.Count == 0 ? 0 : Experience / receivers.Count;

		if (Result == BattleResult.Won)
		{
			foreach (var receiver in receivers)
				receiver.PartyMember.ExperiencePoints += (uint)experience;
		}

		foreach (var partyMember in partyMembers)
			partyMember.Character.RemoveCondition(RemovedAfterBattleConditions);

		return Result == BattleResult.Won ? experience : 0;
	}

	public int LootGold => defeatedMonsters.Sum(monster => monster.Monster.Gold);

	public int LootFood => defeatedMonsters.Sum(monster => monster.Monster.Food);

	/// <summary>
	/// The equipment and inventory items of the defeated monsters.
	/// </summary>
	public IEnumerable<ItemSlot> LootItems => defeatedMonsters.SelectMany(monster => monster.Monster.Equipment.Values.Concat(monster.Monster.Inventory))
		.Where(slot => slot.Count != 0 && slot.Item != null);

	#endregion
}
