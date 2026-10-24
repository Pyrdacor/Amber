using Amber.Common;
using Amberstar.Game.Battle;
using MapCharacter = Amberstar.Game.Characters.MapCharacter;
using Amberstar.Game.Screens;
using Amberstar.GameData;

namespace Amberstar.Game;

/// <summary>
/// The things of the defeated monsters.
/// </summary>
internal sealed record BattleLoot(List<(IItem Item, int Count)> Items, int Gold, int Food);

partial class Game
{
	const int BattleSong = 16;
	// Shandra rescues the party if this quest bit is set and the party is lucky.
	const int ShandraQuestBit = 6;
	const int ResurrectionMap = 70;
	static readonly Position ResurrectionPosition = new(16, 13);

	/// <summary>
	/// The battle which is shown by the battle screen.
	/// </summary>
	internal Battle.Battle? CurrentBattle { get; private set; }

	/// <summary>
	/// The 1-based combat background of the current battle.
	/// </summary>
	internal int CurrentBattleBackground { get; private set; } = 1;

	/// <summary>
	/// The loot which is shown by the loot screen.
	/// </summary>
	internal BattleLoot? CurrentLoot { get; private set; }

	// the monster on the map which is fought (it is removed when the party wins)
	MapCharacter? battleMapCharacter;

	/// <summary>
	/// The combat background is given by the first set bit 16-29 of the tile flags at the party position
	/// (CombatYes in PERSONS.ASM). Without any of these bits the first background is used.
	/// </summary>
	internal static int GetCombatBackground(uint tileFlags)
	{
		for (int bit = 16; bit < 30; bit++)
		{
			if ((tileFlags & (1u << bit)) != 0)
				return bit - 15;
		}

		return 1;
	}

	/// <summary>
	/// A monster on the map reached the party (Battle_check in the program). Its character index is the monster group.
	/// </summary>
	internal void MonsterReachedParty(MapCharacter monster)
	{
		if (CurrentBattle != null || State.TravelType >= TravelType.Eagle || ScreenHandler.ActiveScreen is not IMapScreen ||
			!State.IsMapCharacterActive(monster.Map.Index, 1 + monster.Index))
			return;

		RequestBattle(monster.CharacterIndex, monster);
	}

	/// <summary>
	/// Asks the player to fight or to flee (CombatReq_Mod in PERSONS.ASM).
	/// </summary>
	internal void RequestBattle(int monsterGroupIndex, MapCharacter? mapCharacter = null)
	{
		void Fight() => StartBattle(monsterGroupIndex, mapCharacter: mapCharacter);

		// The combat requester uses a larger window than the other confirmations.
		AskForConfirmation(LoadMessageText(Message.AttackOrFlee), () => AfterScreenClosed(Fight),
			() => AfterScreenClosed(() => TryToAvoidBattle(monsterGroupIndex, mapCharacter)), true);
	}

	/// <summary>
	/// Invisible parties always escape. Otherwise the dexterity and luck of the active member are probed.
	/// If this fails the monsters strike first (CombatNo in PERSONS.ASM).
	/// </summary>
	void TryToAvoidBattle(int monsterGroupIndex, MapCharacter? mapCharacter)
	{
		bool escaped = State.ActiveSpells[(int)ActiveSpellType.Invisibility].Duration != 0 || State.TravelType == TravelType.SuperChicken;

		if (!escaped)
		{
			var partyMember = State.ActivePartyMember!;
			int value = partyMember.Attributes[GameData.Attribute.Dexterity].TotalCurrent + partyMember.Attributes[GameData.Attribute.Luck].TotalCurrent;
			escaped = value >= 150 || value >= Random(0, 149);
		}

		if (!escaped)
		{
			StartBattle(monsterGroupIndex, monsterAdvantage: true, mapCharacter: mapCharacter);
			return;
		}

		// The party goes back to its previous position.
		switch (ScreenHandler.FindMapScreen())
		{
			case Map2DScreen map2DScreen:
				map2DScreen.ResetPartyPosition();
				break;
			case Map3DScreen map3DScreen:
				map3DScreen.ResetPartyPosition();
				break;
		}
	}

	/// <summary>
	/// Starts a battle against a monster group (MON_DATA.AMB).
	/// </summary>
	internal void StartBattle(int monsterGroupIndex, bool partyAdvantage = false, bool monsterAdvantage = false, MapCharacter? mapCharacter = null)
	{
		var monsterGroup = AssetProvider.MonsterGroupLoader.LoadMonsterGroup(monsterGroupIndex);
		var partyMembers = State.PartyMembersWithSlot.Where(p => p.PartyMember != null)
			.Select(p => new PartyMemberParticipant(p.PartyMember!, p.SlotIndex + 1)).ToList();

		CurrentBattleBackground = ScreenHandler.FindMapScreen() switch
		{
			Map2DScreen map2DScreen => GetCombatBackground(map2DScreen.PartyTileFlags),
			Map3DScreen map3DScreen => GetCombatBackground(map3DScreen.PartyTileFlags),
			_ => 1
		};

		// Invisible parties strike first.
		partyAdvantage |= State.ActiveSpells[(int)ActiveSpellType.Invisibility].Duration != 0;
		battleMapCharacter = mapCharacter;

		CurrentBattle = new(AssetProvider, monsterGroup, partyMembers, State.CombatPositions, partyAdvantage, monsterAdvantage,
			Probe, (min, max) => Random(min, max))
		{
			ActivateSpell = (type, value, duration) => ActivateSpell(type, value, duration)
		};

		PlaySong(BattleSong);
		ScreenHandler.PushScreen(ScreenType.Battle, () => AfterScreenClosed(FinishBattle));
	}

	/// <summary>
	/// The active member chooses a spell for the next battle round. The spell is cast during the round.
	/// </summary>
	internal void SelectBattleSpell(Action<(SpellSchool School, int SpellNumber)?> selected)
	{
		SpellCaster = State.ActivePartyMember;
		SpellLocation = SpellLocation.Battle;
		SelectedSpell = null;
		magicItem = null;

		ScreenHandler.PushScreen(ScreenType.SpellSelect, () => AfterScreenClosed(() => selected(SelectedSpell)));
	}

	/// <summary>
	/// The texts which ask for the target of a battle spell (Message files 7 and 8).
	/// </summary>
	internal static Message? GetSpellTargetMessage(SpellSchool school, int spellNumber, SpellTarget target) => (school, spellNumber) switch
	{
		(SpellSchool.White, >= 1 and <= 5) => Message.WhoToHeal,
		(SpellSchool.White, 10) => Message.WhosePoisonToCure,
		(SpellSchool.White, 11) => Message.WhoseParalysisToCure,
		(SpellSchool.White, 12) => Message.WhoseDiseaseToCure,
		(SpellSchool.White, 13) => Message.WhoseAgingToCure,
		(SpellSchool.White, 14) => Message.WhosePetrificationToCure,
		(SpellSchool.White, 16) => Message.WhosePanicToRemove,
		(SpellSchool.White, 17) => Message.WhoseIrritationToRemove,
		(SpellSchool.White, 18) => Message.WhoseBlindnessToCure,
		(SpellSchool.White, 19) => Message.WhoseMadnessToCure,
		(SpellSchool.White, 20) => Message.WhichMonsterToParalyze,
		(SpellSchool.White, 21) => Message.WhichGroupToSleep,
		(SpellSchool.White, 22) => Message.WhichMonsterToPanic,
		(SpellSchool.White, 23) => Message.WhichMonsterToIrritate,
		(SpellSchool.White, 24) => Message.WhichMonsterToBlind,
		(SpellSchool.White, 25) => Message.WhichUndeadToDestroy,
		(SpellSchool.White, 26) => Message.WhichUndeadGroup,
		(SpellSchool.Gray, 23) => Message.WhoToHaste,
		(SpellSchool.Black, 21) => Message.WhichMonsterToDissolve,
		_ => target switch
		{
			SpellTarget.Monster => Message.WhichMonster,
			SpellTarget.MonsterGroup => Message.WhichMonsterGroup,
			_ => null
		}
	};

	/// <summary>
	/// The armour protection, weapon power and anti-magic spells affect the battle.
	/// </summary>
	internal void UpdateBattleSpells(Battle.Battle battle)
	{
		int Value(ActiveSpellType type) => State.ActiveSpells[(int)type].Duration == 0 ? 0 : State.ActiveSpells[(int)type].Value;

		battle.ArmorProtection = Value(ActiveSpellType.ArmorProtection);
		battle.WeaponPower = Value(ActiveSpellType.WeaponPower);
		battle.AntiMagic = Value(ActiveSpellType.AntiMagic);
	}

	/// <summary>
	/// After a won battle the monster leaves the map, the experience is distributed and the
	/// things of the monsters can be taken (0x19662 in the program).
	/// </summary>
	void FinishBattle()
	{
		var battle = CurrentBattle!;
		CurrentBattle = null;
		var mapCharacter = battleMapCharacter;
		battleMapCharacter = null;
		int experience = battle.Finish();

		UpdatePartyMembers();

		if (battle.Result == BattleResult.PartyDefeated)
		{
			PartyDefeated();
			return;
		}

		ScreenHandler.FindMapScreen()?.RestartMusic();
		PartyMembersChanged();

		if (battle.Result != BattleResult.Won)
			return;

		if (mapCharacter != null)
			State.SetMapCharacterActive(mapCharacter.Map.Index, 1 + mapCharacter.Index, false);

		var loot = new BattleLoot([.. battle.LootItems.Select(slot => (slot.Item!, (int)slot.Count))], battle.LootGold, battle.LootFood);

		void ShowLoot()
		{
			if (loot.Items.Count == 0 && loot.Gold == 0 && loot.Food == 0)
				return;

			CurrentLoot = loot;
			ScreenHandler.PushScreen(ScreenType.Loot, () => CurrentLoot = null);
		}

		if (experience != 0)
			ShowTextMessage($"{string.Join(" ", LoadMessageText(Message.EachMemberReceives).GetLines(int.MaxValue))} {experience} EP.", () => AfterScreenClosed(ShowLoot));
		else
			ShowLoot();
	}

	/// <summary>
	/// Nobody of the party can act anymore. If the quest bit 6 is set and the average luck of
	/// the party succeeds, Shandra rescues the party. Otherwise the game is over (0x7a2 in the program).
	/// </summary>
	internal void PartyDefeated()
	{
		var partyMembers = State.PartyMembers.ToList();
		int averageLuck = partyMembers.Count == 0 ? 0 : partyMembers.Sum(partyMember => partyMember.Attributes[GameData.Attribute.Luck].TotalCurrent) / partyMembers.Count;

		if (State.IsQuestBitSet(ShandraQuestBit) && Probe(averageLuck))
			Resurrection();
		else
			GameOver();
	}

	/// <summary>
	/// Shandra heals the party which wakes up the next morning at her place ("Resurrection sequence" at 0x9a8 in the program).
	/// </summary>
	void Resurrection()
	{
		// TODO: The original plays song 19 but only 16 songs are loaded from the program data so far.

		State.Day++;
		State.TravelledDays++;

		if (State.Day == 31)
		{
			State.Day = 1;

			if (++State.Month == 13)
			{
				State.Month = 1;
				State.Year++;
				State.RelativeYear++;
			}
		}

		State.Hour = 7;
		State.Minute = 20;

		foreach (var partyMember in State.PartyMembers)
		{
			partyMember.RemoveCondition((Condition)0xffff);
			partyMember.HitPoints.CurrentValue = partyMember.HitPoints.TotalMax;
		}

		UpdatePartyMembers();
		ResetStatusIcons();
		ScreenHandler.ClearAllScreens();
		ShowTextMessage(LoadMessageText(Message.ShandraRescue), () => AfterScreenClosed(() =>
			Teleport(ResurrectionPosition.X, ResurrectionPosition.Y, Direction.Up, ResurrectionMap, true)));
	}

	/// <summary>
	/// The goddess Bala receives the party. The game starts again with the savegame
	/// or the initial game ("Game Over sequence" at 0xba4 in the program).
	/// </summary>
	void GameOver()
	{
		// TODO: The original plays song 19 but only 16 songs are loaded from the program data so far.
		ScreenHandler.ClearAllScreens();
		ShowTextMessage(LoadMessageText(Message.PartyDeath), () => AfterScreenClosed(RestartGame));
	}

	void RestartGame()
	{
		if (HasSavegame)
		{
			LoadGameAndContinue();
			return;
		}

		State.LoadFrom(AssetProvider.SavegameLoader.LoadSavegame());
		State.ResetPartyMembers();
		ResetStatusIcons();
		UpdatePartyMembers();
		ScreenHandler.ClearAllScreens();
		Teleport(1 + State.PartyPosition.X, 1 + State.PartyPosition.Y, State.PartyDirection, State.MapIndex, true);
	}

	internal void ShowNotImplementedMessage(string feature, Action? nextAction = null) => ShowTextMessage($"{feature} is not implemented yet.", nextAction);
}
