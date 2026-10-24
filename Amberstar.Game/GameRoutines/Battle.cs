using Amberstar.Game.Battle;
using Amberstar.Game.Screens;
using Amberstar.GameData;

namespace Amberstar.Game;

partial class Game
{
	const int BattleSong = 16;

	/// <summary>
	/// The battle which is shown by the battle screen.
	/// </summary>
	internal Battle.Battle? CurrentBattle { get; private set; }

	/// <summary>
	/// The 1-based combat background of the current battle.
	/// </summary>
	internal int CurrentBattleBackground { get; private set; } = 1;

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
	/// Starts a battle against a monster group (MON_DATA.AMB).
	/// </summary>
	internal void StartBattle(int monsterGroupIndex, bool partyAdvantage = false, bool monsterAdvantage = false)
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

		CurrentBattle = new(AssetProvider, monsterGroup, partyMembers, State.CombatPositions, partyAdvantage, monsterAdvantage,
			Probe, (min, max) => Random(min, max));

		PlaySong(BattleSong);
		ScreenHandler.PushScreen(ScreenType.Battle, () => AfterScreenClosed(FinishBattle));
	}

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

	void FinishBattle()
	{
		var battle = CurrentBattle!;
		CurrentBattle = null;
		int experience = battle.Finish();

		UpdatePartyMembers();
		ScreenHandler.FindMapScreen()?.RestartMusic();

		// TODO: Loot, the death of the party and removing the monsters from the map
		if (battle.Result == BattleResult.Won && experience != 0)
			ShowTextMessage($"{string.Join(" ", LoadMessageText(Message.EachMemberReceives).GetLines(int.MaxValue))} {experience} EP.");
	}

	internal void ShowNotImplementedMessage(string feature, Action? nextAction = null) => ShowTextMessage($"{feature} is not implemented yet.", nextAction);
}
