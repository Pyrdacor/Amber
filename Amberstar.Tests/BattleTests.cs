using Amber.Common;
using Amberstar.Frontend;
using Amberstar.Frontend.Scripting;
using Amberstar.Game.Battle;
using Amberstar.GameData;

namespace Amberstar.Tests;

/// <summary>
/// Checks the battle logic (disassembled from the original, see Docs/Combat.md) with fixed random values.
/// </summary>
public sealed class BattleTests
{
	// Monster group 4: giant rats (monster 6) in the front row (columns 1 and 4) and the middle row (column 2),
	// a rat king (monster 7) in the back row (column 3).
	const int RatGroup = 4;

	sealed class Setup : IDisposable
	{
		public HeadlessGame Game { get; }
		public Battle Battle { get; private set; } = null!;
		public bool ProbeResult { get; set; } = true;
		public Func<int, int, int> Random { get; set; } = (min, max) => max;

		public Setup(params int[] battlePositions) : this(false, battlePositions)
		{
		}

		public Setup(bool partyAdvantage, params int[] battlePositions)
		{
			Game = new(TestData.GetDataPath(GameSetup.RepositoryDataFolders[0]));
			var result = Game.RunScript(new ScenarioScript(["waittext", "dismissall"], _ => { }), output: _ => { });
			Assert.True(result.Success, Game.Game.ScriptState());

			var state = Game.Game.State;
			var partyMembers = state.PartyMembersWithSlot.Where(p => p.PartyMember != null).Select(p => new PartyMemberParticipant(p.PartyMember!, p.SlotIndex + 1)).ToList();
			int[] positions = battlePositions.Length == 0 ? [.. state.CombatPositions] : battlePositions;
			Battle = new(Game.Game.AssetProvider, Game.Game.AssetProvider.MonsterGroupLoader.LoadMonsterGroup(RatGroup), partyMembers,
				positions, partyAdvantage, false, _ => ProbeResult, (min, max) => Random(min, max));
		}

		public void Dispose() => Game.Dispose();
	}

	static Position Cell(int column, int row) => new(column, row);

	[Fact]
	public void MonstersAndPartyArePlaced()
	{
		using var setup = new Setup(0);
		var battle = setup.Battle;

		Assert.IsType<MonsterParticipant>(battle[1, 2]);
		Assert.IsType<MonsterParticipant>(battle[4, 2]);
		Assert.IsType<MonsterParticipant>(battle[2, 1]);
		Assert.Equal(7, ((MonsterParticipant)battle[3, 0]!).MonsterIndex);
		Assert.Equal(4, battle.Monsters.Count());
		Assert.IsType<PartyMemberParticipant>(battle[0, 3]);
	}

	[Fact]
	public void MeleeReachesOnlyTheFrontRowFromTheFront()
	{
		using var setup = new Setup(0);
		var battle = setup.Battle;

		// column 0 reaches the rat diagonal in front (column 1)
		Assert.Equal([Cell(1, 2)], battle.GetAttackTargets(1, out bool ranged));
		Assert.False(ranged);
		Assert.True(battle.CanAttack(1));
		// only the back row can flee
		Assert.False(battle.CanFlee(1));
	}

	[Fact]
	public void BackRowCanFleeButNotAttack()
	{
		using var setup = new Setup(6);
		var battle = setup.Battle;

		Assert.Empty(battle.GetAttackTargets(1, out _));
		Assert.False(battle.CanAttack(1));
		Assert.True(battle.CanFlee(1));
		Assert.Contains(Cell(1, 4), battle.GetMoveTargets(1));
		Assert.Contains(Cell(0, 3), battle.GetMoveTargets(1));
	}

	[Fact]
	public void AttackKillsAMonsterAndGivesExperience()
	{
		using var setup = new Setup(0);
		var battle = setup.Battle;
		var rat = (MonsterParticipant)battle[1, 2]!;
		rat.Monster.HitPoints.CurrentValue = 1;
		// the hero hits with full damage, the monsters never hit
		setup.Random = (min, max) => max;
		battle.SetPartyAction(1, new(BattleActionType.Attack, Cell(1, 2)));

		var events = battle.ExecuteRound();

		Assert.Contains(events, e => e is BattleTextEvent { Message: Message.Hits } hit && hit.Target == rat);
		Assert.Contains(events, e => e is BattleRemoveEvent remove && remove.Participant == rat);
		Assert.Null(battle[1, 2]);
		Assert.Equal(15, battle.Experience);
		Assert.Single(battle.DefeatedMonsters);
	}

	[Fact]
	public void ParryingDefenderParriesWhenTheProbeSucceeds()
	{
		using var setup = new Setup(0);
		var battle = setup.Battle;
		battle.SetPartyAction(1, BattleAction.Parry);

		var events = battle.ExecuteRound();

		// the rat in front attacks and every probe succeeds, so the hero parries
		Assert.Contains(events, e => e is BattleTextEvent { Message: Message.AttackParried } parried && parried.Actor is PartyMemberParticipant);
	}

	[Fact]
	public void MonstersRetreatWithLowMorale()
	{
		using var setup = new Setup(0);
		var battle = setup.Battle;
		// Kill one of the three rats. 34% are lost, a morale of 50 keeps the other rats fighting:
		// the front rat moves sideways towards the hero and the middle rat advances to the front row.
		((MonsterParticipant)battle[1, 2]!).Monster.HitPoints.CurrentValue = 1;
		var sideRat = (MonsterParticipant)battle[4, 2]!;
		var middleRat = (MonsterParticipant)battle[2, 1]!;
		battle.SetPartyAction(1, new(BattleActionType.Attack, Cell(1, 2)));
		battle.ExecuteRound();
		battle.PrepareRound();

		Assert.Null(battle[1, 2]);
		Assert.Equal(Cell(3, 2), sideRat.Position);
		Assert.Equal(Cell(2, 2), middleRat.Position);

		// After the second rat 67% are lost and the last rat retreats one row.
		sideRat.Monster.AddCondition(Condition.Dead);
		battle.SetPartyAction(1, BattleAction.Parry);
		battle.ExecuteRound();

		Assert.Equal(Cell(2, 1), middleRat.Position);
	}

	[Fact]
	public void FleeingFromTheBackRowEndsTheBattle()
	{
		using var setup = new Setup(6);
		var battle = setup.Battle;
		// flee succeeds with a random value of 0
		setup.Random = (min, max) => min;
		battle.SetPartyAction(1, new(BattleActionType.Flee));

		var events = battle.ExecuteRound();

		Assert.Contains(events, e => e is BattleTextEvent { Message: Message.FleesSuccessfully });
		Assert.Equal(BattleResult.PartyFled, battle.Result);
		Assert.True(battle.PartyMembers[0].HasAnyConditionOf(Condition.Fled));

		battle.Finish();
		Assert.False(battle.PartyMembers[0].HasAnyConditionOf(Condition.Fled));
	}

	[Fact]
	public void PartyIsDefeatedWhenNobodyCanAct()
	{
		using var setup = new Setup(0);
		var battle = setup.Battle;
		var hero = battle.PartyMembers[0];
		hero.Character.HitPoints.CurrentValue = 1;
		// the monsters always hit with full damage and the hero never parries
		setup.ProbeResult = true;
		battle.SetPartyAction(1, BattleAction.None);

		battle.ExecuteRound();

		Assert.Equal(BattleResult.PartyDefeated, battle.Result);
		Assert.True(hero.HasAnyConditionOf(Condition.Dead));
	}

	[Fact]
	public void WinningDistributesTheExperience()
	{
		// With the party advantage the monsters do nothing in the first round (the last rat would flee otherwise).
		using var setup = new Setup(true, 0);
		var battle = setup.Battle;

		foreach (var monster in battle.Monsters.Where(m => m.Position != Cell(1, 2)))
			monster.Monster.AddCondition(Condition.Dead);

		((MonsterParticipant)battle[1, 2]!).Monster.HitPoints.CurrentValue = 1;
		battle.SetPartyAction(1, new(BattleActionType.Attack, Cell(1, 2)));
		var hero = battle.PartyMembers[0].PartyMember;
		uint experience = hero.ExperiencePoints;

		var events = battle.ExecuteRound();

		Assert.True(battle.Result == BattleResult.Won, string.Join("; ", events));
		int perMember = battle.Finish();
		Assert.Equal(battle.Experience, perMember);
		Assert.Equal(experience + (uint)perMember, hero.ExperiencePoints);
	}
}
