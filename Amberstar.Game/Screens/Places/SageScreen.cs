using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

/// <summary>
/// The sage examines (identifies) items of the active party member. Either the
/// equipped items or the backpack items are shown. After an item was examined,
/// its details are shown. Clicking a portrait selects another party member.
/// </summary>
internal sealed class SageScreen : PlaceScreen
{
	const int ShowEquipmentButtonIndex = 0;
	const int ShowInventoryButtonIndex = 3;
	const int SlotCount = 12;
	readonly ItemContainer[] itemContainers = new ItemContainer[SlotCount];
	readonly ItemSlot?[] shownSlots = new ItemSlot?[SlotCount];
	bool showInventory = false;

	public override ScreenType Type { get; } = ScreenType.Sage;

	protected override Image80x80 Image { get; } = Image80x80.Sage;

	protected override Message LeaveMessage { get; } = Message.GiveGoldToMerchant;

	static Position GetSlotPosition(int slot) => new(16 + (slot % 6) * 32, 37 + 108 + (slot / 6) * 32);

	public override void Init()
	{
		base.Init();

		for (int i = 0; i < SlotCount; i++)
		{
			var (x, y) = GetSlotPosition(i);
			itemContainers[i] = AddItem(x, y, item: null, count: 0, displayLayer: 10);
		}
	}

	public override void Open(Action? closeAction)
	{
		showInventory = false;

		base.Open(closeAction);

		ShowItems();
	}

	public override void Close()
	{
		foreach (var itemContainer in itemContainers)
			itemContainer.ClearItem();

		base.Close();
	}

	public override void ScreenPopped(Screen screen)
	{
		base.ScreenPopped(screen);

		if (!screen.Transparent)
			ShowItems();
	}

	private void ShowItems()
	{
		var partyMember = Game.State.ActivePartyMember!;
		IEnumerable<ItemSlot> slots = showInventory
			? partyMember.Inventory
			// monster items are not shown
			: Enum.GetValues<EquipmentSlot>().Select(slot => partyMember.Equipment[slot]).Where(slot => slot.Item?.Type != ItemType.MonsterItem);
		int index = 0;

		foreach (var slot in slots.Where(slot => slot.Item != null && slot.Count != 0))
		{
			shownSlots[index] = slot;
			itemContainers[index++].SetItem(slot.Count, slot.Item);
		}

		for (; index < SlotCount; index++)
		{
			shownSlots[index] = null;
			itemContainers[index].ClearItem();
		}

		RequestButtonSetup();
	}

	protected override void SetupPlaceButtons(ButtonGrid buttonGrid)
	{
		buttonGrid.SetButton(ShowEquipmentButtonIndex, ButtonType.Equipment);
		buttonGrid.SetButton(ShowInventoryButtonIndex, ButtonType.Inventory);
		buttonGrid.EnableButton(ShowEquipmentButtonIndex, showInventory);
		buttonGrid.EnableButton(ShowInventoryButtonIndex, !showInventory);
	}

	protected override void PlaceButtonClicked(int index)
	{
		if (index == ShowEquipmentButtonIndex || index == ShowInventoryButtonIndex)
		{
			showInventory = index == ShowInventoryButtonIndex;
			ShowItems();
		}
	}

	/// <summary>
	/// Examines the item in the given slot (0-based). This is also used by scenario scripts.
	/// </summary>
	internal void ExamineItemInSlot(int slot)
	{
		var itemSlot = slot >= 0 && slot < SlotCount ? shownSlots[slot] : null;

		if (itemSlot?.Item == null)
			return;

		if (itemSlot.Item.SlotFlags.HasFlag(ItemSlotFlags.Identified))
		{
			ShowMessage(Message.ItemAlreadyExamined);
			return;
		}

		if (Place.Data.Price > PlaceGold)
		{
			ShowMessage(Message.NotEnoughGoldForHealer);
			return;
		}

		AskToBuy(Game.LoadMessageText(Message.ExaminationPrice), Place.Data.Price, () =>
		{
			Game.IdentifyItem(itemSlot);
			ShowItems();
			Game.CurrentItem = itemSlot.Item;
			Game.ScreenHandler.PushScreen(ScreenType.ItemView);
		});
	}

	protected override bool PlaceMouseDown(Position position, MouseButtons buttons)
	{
		if (buttons != MouseButtons.Left)
			return false;

		for (int i = 0; i < SlotCount; i++)
		{
			if (new Rect(GetSlotPosition(i), new Size(16, 16)).Contains(position))
			{
				ExamineItemInSlot(i);
				return true;
			}
		}

		int? portraitSlot = Game.TestPartyPortraitHit(position);

		if (portraitSlot == null)
			return false;

		var partyMember = Game.State.GetPartyMember(portraitSlot.Value);

		// Only members who are able to act can be selected.
		if (partyMember != null && !partyMember.HasAnyConditionOf(Condition.Dead | Condition.Ashes | Condition.Dust |
			Condition.Petrified | Condition.Stunned | Condition.Mad | Condition.Sleeping))
		{
			Game.State.SetActivePartyMember(portraitSlot.Value);
			Game.UpdatePartyMembers();
			ShowItems();
		}

		return true;
	}
}
