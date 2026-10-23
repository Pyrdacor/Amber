using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Events;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

/// <summary>
/// The camp (Camp in EYE.ASM of the original). The party can rest once per camp
/// (8 hours), the active party member can learn spells from scrolls and cast spells.
///
/// In an inn the party rests in a guestroom which fully regenerates everyone without
/// food. After leaving the camp, the party stands at the exit position of the inn.
/// </summary>
internal sealed class CampScreen : ButtonGridScreen, IWaitForClickScreen
{
	const int CastSpellButtonIndex = 0;
	const int ExitButtonIndex = 2;
	const int LearnSpellButtonIndex = 3;
	const int RestButtonIndex = 6;
	const int SlotCount = 12;
	const int HoursOfRest = 8;
	const int TicksPerHour = 12;
	static readonly Rect messageArea = new(112, 49, 192, 48);
	readonly ItemContainer[] itemContainers = new ItemContainer[SlotCount];
	Label? message;
	bool waitForClick = false;
	Action? afterClickAction = null;
	bool slept = false;
	bool learnSpellMode = false;

	public override ScreenType Type { get; } = ScreenType.Camp;

	internal override byte ButtonGridPaletteIndex => Game?.PaletteIndexProvider.Get80x80ImagePaletteIndex(Image80x80.Camp) ?? 0;

	public bool WaitingForClick => waitForClick;

	static Position GetSlotPosition(int slot) => new(16 + (slot % 6) * 32, 37 + 108 + (slot / 6) * 32);

	public override void Init()
	{
		base.Init();

		var image = AddImage(16, 49, 80, 80, Game.GraphicIndexProvider.Get80x80ImageIndex(Image80x80.Camp), 0, true);
		image.PaletteIndex = ButtonGridPaletteIndex;

		var (x, y, width, height) = messageArea;
		message = AddLabel(x, y, width, height, 20);

		for (int i = 0; i < SlotCount; i++)
		{
			var (slotX, slotY) = GetSlotPosition(i);
			itemContainers[i] = AddItem(slotX, slotY, item: null, count: 0, displayLayer: 10);
		}
	}

	public override void Open(Action? closeAction)
	{
		slept = false;
		learnSpellMode = false;
		waitForClick = false;
		afterClickAction = null;

		base.Open(closeAction);

		message!.Visible = false;
		Game.SetLayout(Layout.Place, ButtonGridPaletteIndex);
		Game.Cursor.CursorType = CursorType.Sword;
		ShowItems();
	}

	public override void Close()
	{
		message!.Visible = false;

		foreach (var itemContainer in itemContainers)
			itemContainer.ClearItem();

		Game.UntrapMouse();

		base.Close();
	}

	public override void ScreenPopped(Screen screen)
	{
		base.ScreenPopped(screen);

		if (!screen.Transparent)
		{
			Game.SetLayout(Layout.Place, ButtonGridPaletteIndex);
			Game.Cursor.CursorType = CursorType.Sword;
		}
	}

	static bool CanUseMagic(IPartyMember partyMember) => partyMember.LearnedSpellSchools != SpellSchoolFlags.None;

	protected override void SetupButtons(ButtonGrid buttonGrid)
	{
		for (int i = 0; i < 9; i++)
			buttonGrid.SetButton(i, ButtonType.Empty);

		buttonGrid.SetButton(CastSpellButtonIndex, ButtonType.UseMagic);
		buttonGrid.SetButton(ExitButtonIndex, ButtonType.Exit);
		buttonGrid.SetButton(LearnSpellButtonIndex, ButtonType.ReadScroll);
		buttonGrid.SetButton(RestButtonIndex, ButtonType.Sleep);

		var partyMember = Game.State.ActivePartyMember;
		bool magic = partyMember != null && CanUseMagic(partyMember);

		buttonGrid.EnableButton(CastSpellButtonIndex, magic && !learnSpellMode);
		buttonGrid.EnableButton(LearnSpellButtonIndex, magic && !learnSpellMode);
		buttonGrid.EnableButton(RestButtonIndex, !slept && !learnSpellMode);
		buttonGrid.EnableButton(ExitButtonIndex, !learnSpellMode);
	}

	protected override void ButtonClicked(int index)
	{
		if (waitForClick)
			return;

		switch (index)
		{
			case CastSpellButtonIndex:
				Game.CastSpell(SpellLocation.Camp, RequestButtonSetup);
				break;
			case ExitButtonIndex:
				Leave();
				break;
			case LearnSpellButtonIndex:
				learnSpellMode = true;
				ShowMessage(Game.LoadMessageText(Message.LearnFromWhichScrolll), false);
				ShowItems();
				break;
			case RestButtonIndex:
				Rest();
				break;
		}
	}

	private void Leave()
	{
		Game.ScreenHandler.PopScreen();

		if (Game.CampInInn is IInnData innData)
		{
			Game.CampInInn = null;
			Game.Teleport(innData.ExitX, innData.ExitY, innData.ExitDirection, Game.State.GetIndexOfMapWithPlayer(), false);
		}
	}

	private void ShowItems()
	{
		var partyMember = Game.State.ActivePartyMember!;

		for (int i = 0; i < SlotCount; i++)
		{
			var slot = partyMember.Inventory[i];

			if (!learnSpellMode || slot.Item == null || slot.Count == 0)
				itemContainers[i].ClearItem();
			else
				itemContainers[i].SetItem(slot.Count, slot.Item);
		}

		RequestButtonSetup();
	}

	private void EndLearnSpellMode()
	{
		learnSpellMode = false;
		message!.Visible = false;
		ShowItems();
	}

	/// <summary>
	/// Learns the spell of the scroll in the given backpack slot (0-based) like Learn_spell in the original.
	/// This is also used by scenario scripts.
	/// </summary>
	internal void LearnSpellFromSlot(int slot)
	{
		var partyMember = Game.State.ActivePartyMember!;
		var itemSlot = partyMember.Inventory[slot];
		var item = itemSlot.Item;

		if (item == null || itemSlot.Count == 0)
		{
			EndLearnSpellMode();
			return;
		}

		void ShowAndContinue(Message message) => ShowMessage(Game.LoadMessageText(message), true,
			() => ShowMessage(Game.LoadMessageText(Message.LearnFromWhichScrolll), false));

		if (item.Type != ItemType.SpellScroll)
		{
			ShowAndContinue(Message.NotASpellScroll);
			return;
		}

		var spellSchoolFlag = (SpellSchoolFlags)(1 << (int)item.SpellSchool);

		if (!partyMember.LearnedSpellSchools.HasFlag(spellSchoolFlag))
		{
			ShowAndContinue(Message.CannotLearnSpellOfThisClass);
			return;
		}

		var spellData = Game.AssetProvider.SpellDataLoader.LoadSpellData(item.SpellSchool, item.SpellIndex);

		if (spellData.SpellLearningPointCost > partyMember.SpellLearningPoints)
		{
			ShowAndContinue(Message.NotEnoughExperienceForSpell);
			return;
		}

		Message result;

		if (Game.Probe(partyMember.Skills[Skill.ReadMagic].TotalCurrent))
		{
			partyMember.SpellLearningPoints -= spellData.SpellLearningPointCost;
			Game.LearnSpell(partyMember, item.SpellSchool, item.SpellIndex);
			result = Message.SpellLearnedScrollCrumbles;
		}
		else
		{
			result = Message.SpellLearnFailedScrollCrumbles;
		}

		// The scroll crumbles to dust.
		Game.RemoveInventoryItem(partyMember, slot, 1);
		itemContainers[slot].ReduceItemCount(1, true, () =>
		{
			ShowItems();
			ShowAndContinue(result);
		});
	}

	/// <summary>
	/// Rests for 8 hours like Rest in the original.
	/// </summary>
	private void Rest()
	{
		slept = true;
		RequestButtonSetup();

		for (int i = 0; i < HoursOfRest * TicksPerHour; i++)
			Game.Time.Tick();

		bool inn = Game.CampInInn != null;
		var texts = new List<string>();
		string Text(Message message) => Game.GetFullText(Game.LoadMessageText(message));

		// Party members with the aging condition get older in the camp (but not in inns).
		if (!inn)
			Game.AgePartyMembers();

		Game.ForeachPartyMember(partyMember =>
		{
			if (!inn && partyMember.Food == 0)
			{
				texts.Add($"{partyMember.Name} {Text(Message.NoFoodCannotRecover)}");
				return;
			}

			int hitPoints;
			int spellPoints;

			if (inn)
			{
				// full regeneration in a warm room
				hitPoints = Math.Max(0, partyMember.HitPoints.TotalMax - partyMember.HitPoints.TotalCurrent);
				spellPoints = Math.Max(0, partyMember.SpellPoints.TotalMax - partyMember.SpellPoints.TotalCurrent);
			}
			else
			{
				// 10% of the maximum in the camp for one food ration
				hitPoints = Math.Min(partyMember.HitPoints.TotalMax / 10, Math.Max(0, partyMember.HitPoints.TotalMax - partyMember.HitPoints.TotalCurrent));
				spellPoints = Math.Min(partyMember.SpellPoints.TotalMax / 10, Math.Max(0, partyMember.SpellPoints.TotalMax - partyMember.SpellPoints.TotalCurrent));
				partyMember.Food--;
				partyMember.TotalWeight -= Game.FoodWeight;
			}

			partyMember.HitPoints.CurrentValue += (word)hitPoints;
			partyMember.SpellPoints.CurrentValue += (word)spellPoints;
			texts.Add($"{partyMember.Name} {Text(Message.Receives)} {hitPoints} LP, {spellPoints} PP {Text(Message.Back)}");
		}, Condition.Dead | Condition.Ashes | Condition.Dust | Condition.Petrified);

		// All active spells end.
		Array.Clear(Game.State.ActiveSpells);
		Game.UpdatePartyMembers();

		void ShowNext(int index)
		{
			if (index < texts.Count)
				ShowMessage(Game.AssetProvider.TextLoader.FromString(texts[index]), true, () => ShowNext(index + 1));
		}

		ShowMessage(Game.LoadMessageText(Message.PartyRestsEightHours), true, () => ShowNext(0));
	}

	private void ShowMessage(IText text, bool waitForClick = true, Action? afterClickAction = null)
	{
		Game.ScriptTextShown(text);
		message!.SetText(text, messageArea.Size.Width, 15, TextManager.TransparentPaper, ButtonGridPaletteIndex);
		message.Visible = true;

		this.waitForClick = waitForClick;
		this.afterClickAction = afterClickAction;

		if (waitForClick)
		{
			Game.Cursor.CursorType = CursorType.Zzz;
			Game.TrapMouse(messageArea);
		}
	}

	private void EndClickWait()
	{
		waitForClick = false;
		Game.Cursor.CursorType = CursorType.Sword;
		message!.Visible = false;
		Game.UntrapMouse();

		var action = afterClickAction;
		afterClickAction = null;
		action?.Invoke();
	}

	public override bool KeyDown(Key key, KeyModifiers keyModifiers)
	{
		if (waitForClick)
		{
			if (key == Key.Space || key == Key.Escape || key == Key.Enter)
				EndClickWait();

			return true;
		}

		if (key == Key.Escape)
		{
			if (learnSpellMode)
				EndLearnSpellMode();
			else
				Leave();

			return true;
		}

		return base.KeyDown(key, keyModifiers);
	}

	public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		if (waitForClick)
		{
			EndClickWait();
			return true;
		}

		if (learnSpellMode)
		{
			if (buttons == MouseButtons.Left)
			{
				for (int i = 0; i < SlotCount; i++)
				{
					if (new Rect(GetSlotPosition(i), new Size(16, 16)).Contains(position))
					{
						LearnSpellFromSlot(i);
						return true;
					}
				}
			}

			EndLearnSpellMode();
			return true;
		}

		if (buttons == MouseButtons.Left && Game.TestPartyPortraitHit(position) is int portraitSlot)
		{
			var partyMember = Game.State.GetPartyMember(portraitSlot);

			// Only members who are able to act can be selected.
			if (partyMember != null && !partyMember.HasAnyConditionOf(Condition.Dead | Condition.Ashes | Condition.Dust |
				Condition.Petrified | Condition.Stunned | Condition.Mad | Condition.Sleeping))
			{
				Game.State.SetActivePartyMember(portraitSlot);
				Game.UpdatePartyMembers();
				RequestButtonSetup();
			}

			return true;
		}

		return base.MouseDown(position, buttons, keyModifiers);
	}
}
