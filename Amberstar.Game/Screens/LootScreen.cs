using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

/// <summary>
/// The things of the defeated monsters after a battle ("After-battle loot dividing party" in the original).
/// The items are shown like the items of a chest (12 per page). Gold and rations are distributed among the party.
/// </summary>
internal sealed class LootScreen : ItemGridScreen, IWaitForClickScreen
{
	const int PreviousPageButtonIndex = 0;
	const int ExitButtonIndex = 2;
	const int NextPageButtonIndex = 3;
	const int ExamineButtonIndex = 4;
	const int TakeItemButtonIndex = 5;
	const int FoodButtonIndex = 7;
	const int GoldButtonIndex = 8;
	const int SlotCount = 12;
	const int SlotsPerRow = 6;
	static readonly Rect messageDisplayArea = new(112, 49, 192, 48);
	static readonly Rect goldDisplayArea = new(112, 113, 64, 16);
	static readonly Rect foodDisplayArea = new(240, 113, 64, 16);
	readonly ItemContainer[] itemSlots = new ItemContainer[SlotCount];
	readonly List<(IItem Item, int Count)> items = [];
	Label? message;
	Label? goldLabel;
	Label? foodLabel;
	int page = 0;
	int gold = 0;
	int food = 0;
	int? givenItemIndex = null;
	bool waitForClick = false;

	public override ScreenType Type { get; } = ScreenType.Loot;

	public override bool Transparent { get; } = false;

	internal override byte ButtonGridPaletteIndex => Game?.PaletteIndexProvider.Get80x80ImagePaletteIndex(Image80x80.OpenChest) ?? 0;

	internal override ItemContainer[] ItemContainers => itemSlots;

	public bool WaitingForClick => waitForClick;

	internal int Gold => gold;

	internal int Food => food;

	internal int ItemCount => items.Count;

	public override void Init()
	{
		base.Init();

		for (int i = 0; i < SlotCount; i++)
			itemSlots[i] = AddItem(16 + (i % SlotsPerRow) * 32, 37 + 108 + (i / SlotsPerRow) * 32, item: null, count: 0, displayLayer: 10);

		var (x, y, width, height) = messageDisplayArea;
		message = AddLabel(x, y, width, height, 20);
	}

	public override void Open(Action? closeAction)
	{
		var loot = Game.CurrentLoot!;

		items.Clear();
		items.AddRange(loot.Items);
		gold = loot.Gold;
		food = loot.Food;
		page = 0;
		givenItemIndex = null;
		waitForClick = false;

		base.Open(closeAction);

		Game.SetLayout(Layout.Chest, ButtonGridPaletteIndex);
		Game.Cursor.CursorType = CursorType.Sword;

		goldLabel = AddAmountLabel(goldDisplayArea, UIText.Gold);
		foodLabel = AddAmountLabel(foodDisplayArea, UIText.Food);

		UpdateItems();
		UpdateAmounts();
		ShowMessage(Message.AfterBattleLoot, false);
	}

	Label AddAmountLabel(Rect area, UIText caption)
	{
		var (x, y, width, height) = area;
		var title = AddLabel(x, y, Game.AssetProvider.TextLoader.LoadText(new(AssetType.UIText, (int)caption)), width, height);
		title.Alignment = TextAlignment.Center;
		title.PaletteIndex = ButtonGridPaletteIndex;

		var amount = AddLabel(x, y + 8, "0", width, height);
		amount.Alignment = TextAlignment.Center;
		amount.PaletteIndex = ButtonGridPaletteIndex;

		return amount;
	}

	public override void Close()
	{
		HideMessage();

		foreach (var slot in itemSlots)
			slot.ClearItem();

		Game.UntrapMouse();
		Game.ResetStatusIcons();

		base.Close();
	}

	protected override void SetupButtons(ButtonGrid buttonGrid)
	{
		bool hasItems = items.Count != 0;

		buttonGrid.SetButton(PreviousPageButtonIndex, ButtonType.ArrowUp);
		buttonGrid.SetButton(1, ButtonType.Empty);
		buttonGrid.SetButton(ExitButtonIndex, ButtonType.Exit);
		buttonGrid.SetButton(NextPageButtonIndex, ButtonType.ArrowDown);
		buttonGrid.SetButton(ExamineButtonIndex, ButtonType.ExamineItem);
		buttonGrid.SetButton(TakeItemButtonIndex, ButtonType.GiveItem);
		buttonGrid.SetButton(6, ButtonType.Empty);
		buttonGrid.SetButton(FoodButtonIndex, ButtonType.DistributeFood);
		buttonGrid.SetButton(GoldButtonIndex, ButtonType.DistributeGold);

		buttonGrid.EnableButton(PreviousPageButtonIndex, page > 0);
		buttonGrid.EnableButton(NextPageButtonIndex, (page + 1) * SlotCount < items.Count);
		buttonGrid.EnableButton(ExamineButtonIndex, hasItems);
		buttonGrid.EnableButton(TakeItemButtonIndex, hasItems);
		buttonGrid.EnableButton(FoodButtonIndex, food != 0);
		buttonGrid.EnableButton(GoldButtonIndex, gold != 0);
	}

	protected override void ButtonClicked(int index)
	{
		if (waitForClick || givenItemIndex != null)
			return;

		switch (index)
		{
			case PreviousPageButtonIndex:
				page--;
				UpdateItems();
				break;
			case NextPageButtonIndex:
				page++;
				UpdateItems();
				break;
			case ExitButtonIndex:
				Game.ScreenHandler.PopScreen();
				break;
			case ExamineButtonIndex:
				Game.ScreenHandler.PushScreen(ScreenType.ChestExamineItem);
				break;
			case TakeItemButtonIndex:
				Game.ScreenHandler.PushScreen(ScreenType.ChestGiveItem);
				break;
			case FoodButtonIndex:
				food = Game.DistributeFood(food);
				UpdateAmounts();
				break;
			case GoldButtonIndex:
				gold = Game.DistributeGold(gold);
				UpdateAmounts();
				break;
		}
	}

	void UpdateItems()
	{
		page = Math.Clamp(page, 0, Math.Max(0, (items.Count - 1) / SlotCount));

		for (int i = 0; i < SlotCount; i++)
		{
			int index = page * SlotCount + i;

			if (index < items.Count)
				itemSlots[i].SetItem(items[index].Count, items[index].Item);
			else
				itemSlots[i].ClearItem();
		}

		RequestButtonSetup();
	}

	void UpdateAmounts()
	{
		goldLabel!.SetText($"{gold:00000}", 15, TextManager.TransparentPaper, ButtonGridPaletteIndex);
		foodLabel!.SetText($"{food:00000}", 15, TextManager.TransparentPaper, ButtonGridPaletteIndex);
		RequestButtonSetup();
	}

	internal override void PickItem(ScreenType sourceScreen, int? index)
	{
		if (index == null || page * SlotCount + index.Value >= items.Count)
			return;

		int itemIndex = page * SlotCount + index.Value;
		var (item, count) = items[itemIndex];

		if (sourceScreen == ScreenType.ChestExamineItem)
		{
			Game.CurrentItem = item;
			Game.ScreenHandler.PushScreen(ScreenType.ItemView);
		}
		else if (sourceScreen == ScreenType.ChestGiveItem)
		{
			// The receiver is chosen by its portrait.
			if (Game.SetHandIcons(null, partyMember => Game.CanReceiveItems(partyMember, item, count)) > 0)
			{
				givenItemIndex = itemIndex;
				Game.TrapMouseInPortraitArea();
			}
			else
			{
				Game.ResetStatusIcons();
				ShowMessage(Message.NoMemberHasRoomForItem);
			}
		}
	}

	/// <summary>
	/// Gives the chosen item to the party member of the given 1-based slot (also used by scenario scripts).
	/// </summary>
	internal bool GiveItem(int slot)
	{
		if (givenItemIndex == null)
			return false;

		var (item, count) = items[givenItemIndex.Value];
		var partyMember = Game.State.GetPartyMember(slot);
		bool given = partyMember != null && Game.CanReceiveItems(partyMember, item, count) && Game.TryAddItem(slot, item, count);

		if (given)
			items.RemoveAt(givenItemIndex.Value);

		givenItemIndex = null;
		Game.UntrapMouse();
		Game.ResetStatusIcons();
		UpdateItems();

		return given;
	}

	public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		if (waitForClick)
		{
			HideMessage();
			return true;
		}

		if (givenItemIndex != null)
		{
			int? slot = buttons == MouseButtons.Left ? Game.TestPartyPortraitHit(position) : null;

			if (slot != null)
				GiveItem(slot.Value);
			else if (buttons == MouseButtons.Right)
				GiveItem(0);

			return true;
		}

		return base.MouseDown(position, buttons, keyModifiers);
	}

	internal override void ShowMessage(Message messageIndex, bool waitForClick = true, bool closeAfterClick = false)
	{
		var text = Game.LoadMessageText(messageIndex);
		Game.ScriptTextShown(text);
		message!.SetText(text, messageDisplayArea.Size.Width, 15, TextManager.TransparentPaper, ButtonGridPaletteIndex);
		message.Visible = true;
		this.waitForClick = waitForClick;
	}

	internal override void HideMessage()
	{
		if (message != null)
			message.Visible = false;

		waitForClick = false;
	}
}
