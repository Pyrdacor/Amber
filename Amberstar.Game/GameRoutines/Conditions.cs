using Amber.Common;
using Amberstar.Game.Screens;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game;

/// <summary>
/// Effects of time, conditions and the ground on the party (TIME.ASM and After_move in MAP.ASM of the original).
/// </summary>
partial class Game
{
	// conditions of party members which are not affected (like $e400 in the original)
	const Condition UnaffectedConditions = Condition.Dead | Condition.Ashes | Condition.Dust | Condition.Petrified;
	const long TicksPerDamageSplash = TicksPerSecond;

	void InitConditionEffects()
	{
		Time.HourChanged += EveryHour;
		Time.DayChanged += EveryDay;
		Time.YearChanged += EveryYear;
	}

	/// <summary>
	/// Poisoned party members lose 1 to 5 hit points every hour (Every_hour in the original).
	/// </summary>
	void EveryHour()
	{
		ForeachPartyMember(partyMember =>
		{
			if (partyMember.HasAnyConditionOf(Condition.Poisoned))
				DamagePartyMember(partyMember, Random(1, 5));
		}, UnaffectedConditions);
	}

	/// <summary>
	/// Diseased party members lose a point of a random attribute every day (Every_day in the original).
	/// The attributes don't fall below 1.
	/// </summary>
	void EveryDay()
	{
		ForeachPartyMember(partyMember =>
		{
			if (!partyMember.HasAnyConditionOf(Condition.Diseased))
				return;

			// only the first 8 attributes
			var attribute = partyMember.Attributes[(GameData.Attribute)Random(1, 8)];

			if (attribute.CurrentValue > 1)
				attribute.CurrentValue--;

			ShowDamageSplash(partyMember, 0);
		}, UnaffectedConditions);
	}

	/// <summary>
	/// Party members who are too old die (Every_year in the original).
	/// </summary>
	void EveryYear()
	{
		ForeachPartyMember(partyMember => CheckAge(partyMember), UnaffectedConditions);
		PartyMembersChanged();
	}

	/// <summary>
	/// Party members with the aging condition get older when they rest in the camp (Camp in EYE.ASM of the original).
	/// </summary>
	internal void AgePartyMembers()
	{
		ForeachPartyMember(partyMember =>
		{
			if (!partyMember.HasAnyConditionOf(Condition.Aging))
				return;

			partyMember.Age.CurrentValue++;
			CheckAge(partyMember);
		}, UnaffectedConditions);

		PartyMembersChanged();
	}

	/// <summary>
	/// Note: The original compares the age plus the years since the game start with the maximum age.
	/// </summary>
	void CheckAge(IPartyMember partyMember)
	{
		var age = partyMember.Age;

		if (age.CurrentValue + age.BonusValue + State.RelativeYear > age.MaxValue)
			partyMember.AddCondition(Condition.Dead);
	}

	/// <summary>
	/// Effects of the ground after each step (After_move in the original).
	/// On poisonous ground each party member gets poisoned unless a luck probe succeeds.
	/// Swimming party members lose 1 to 5 hit points unless a swim probe succeeds.
	/// </summary>
	internal void AfterPartyMoved(bool poisonousGround, bool water)
	{
		var travelType = State.TravelType;

		if (poisonousGround && travelType is TravelType.Walk or TravelType.Horse or TravelType.MagicDisc)
		{
			ForeachPartyMember(partyMember =>
			{
				if (!Probe(partyMember.Attributes[GameData.Attribute.Luck].TotalCurrent))
				{
					partyMember.AddCondition(Condition.Poisoned);
					ShowDamageSplash(partyMember, 0);
				}
			}, UnaffectedConditions);
		}

		if (water && travelType == TravelType.Walk)
		{
			ForeachPartyMember(partyMember =>
			{
				if (!Probe(partyMember.Skills[Skill.Swim].TotalCurrent))
					DamagePartyMember(partyMember, Random(1, 5));
			}, UnaffectedConditions);
		}
	}

	/// <summary>
	/// Damages a party member and shows the damage at the portrait (Do_damage in the original).
	/// </summary>
	internal void DamagePartyMember(IPartyMember partyMember, int damage, Action? finishHandler = null)
	{
		partyMember.Damage((word)Math.Clamp(damage, 0, word.MaxValue));
		ShowDamageSplash(partyMember, damage);
		PartyMembersChanged();
		finishHandler?.Invoke();
	}

	/// <summary>
	/// Shows the damage splash with the damage over the portrait (Display_damage in the original).
	/// Without damage only the splash is shown (e.g. for diseases).
	/// </summary>
	void ShowDamageSplash(IPartyMember partyMember, int damage)
	{
		var (slotIndex, _) = State.PartyMembersWithSlot.FirstOrDefault(p => p.PartyMember == partyMember, (-1, null));

		if (slotIndex == -1)
			return;

		var position = new Position(16 + slotIndex * 48, 1);
		var splash = CreateSprite(Layer.UI, position, new Size(32, 32), GraphicIndexProvider.GetUIGraphicIndex(UIGraphic.DamageSplash),
			PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI])!;
		splash.DisplayLayer = 50;

		IRenderText? damageText = null;

		if (damage != 0)
		{
			damageText = TextManager.Create(damage.ToString(), 15, TextManager.TransparentPaper, PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI]);
			damageText.ShowInArea(position.X, position.Y + 12, 32, 7, 60, TextAlignment.Center);
		}

		AddDelayedAction(TicksPerDamageSplash, () =>
		{
			splash.Visible = false;
			damageText?.Delete();
		});
	}

	/// <summary>
	/// Updates the portraits. If the active party member can't lead the party anymore,
	/// a new one must be chosen (ActSel_Mod in the original).
	/// </summary>
	internal void PartyMembersChanged()
	{
		UpdatePartyMembers();

		var activePartyMember = State.ActivePartyMember;

		if (activePartyMember == null || !activePartyMember.HasAnyConditionOf(UnaffectedConditions))
			return;

		if (!State.PartyMembers.Any(partyMember => !partyMember.HasAnyConditionOf(UnaffectedConditions)))
		{
			// The battle handles its end itself.
			if (CurrentBattle == null)
				PartyDefeated();

			return;
		}

		ChooseNewLeader();
	}

	bool choosingNewLeader = false;

	void ChooseNewLeader()
	{
		if (choosingNewLeader)
			return;

		choosingNewLeader = true;
		CurrentText = LoadMessageText(Message.ChooseNewLeader);
		SelectedPartyMemberSlot = null;

		ScreenHandler.PushScreen(ScreenType.SelectPartyMember, () => AfterScreenClosed(() =>
		{
			choosingNewLeader = false;
			int? slot = SelectedPartyMemberSlot;
			var partyMember = slot == null ? null : State.GetPartyMember(slot.Value);

			// The player must choose a party member who can lead the party.
			if (partyMember == null || partyMember.HasAnyConditionOf(UnaffectedConditions))
			{
				ChooseNewLeader();
				return;
			}

			State.SetActivePartyMember(slot!.Value);
			UpdatePartyMembers();
		}));
	}
}
