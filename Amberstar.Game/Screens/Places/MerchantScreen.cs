using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Events;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

/// <summary>
/// Item merchants and magic merchants (MerchOBJC and MerchMAGIC in the original).
///
/// Bought items are placed on the counter (the item slots) and are given to the
/// party members from there. Item merchants also buy items and let the player
/// look at their wares. Magic merchants only sell.
/// </summary>
internal sealed class MerchantScreen : PlaceScreen
{
	const int BuyButtonIndex = 0;
	const int SellButtonIndex = 3;
	const int ViewButtonIndex = 4;
	const int TransferButtonIndex = 5;
	const int SlotCount = 12;
	const int MaxStackSize = 99;
	// A ware count of 255 means unlimited
	const int UnlimitedWares = 255;
	readonly ItemContainer[] itemContainers = new ItemContainer[SlotCount];
	readonly ItemSlot[] counter = Enumerable.Range(0, SlotCount).Select(_ => new ItemSlot(0, null)).ToArray();
	IItem?[] wares = [];
	int[] wareCounts = [];
	Mode mode = Mode.Counter;
	Action<int>? amountEntered = null;

	enum Mode
	{
		Counter,
		Buy,
		Sell,
		View,
		Transfer
	}

	public override ScreenType Type { get; } = ScreenType.Merchant;

	bool MagicMerchant => PlaceEvent?.PlaceType == PlaceType.Library;

	protected override Image80x80 Image => MagicMerchant ? Image80x80.Library : Image80x80.Merchant;

	protected override Message LeaveMessage { get; } = Message.GiveItemsToMerchant;

	protected override bool SomethingLeftBehind => base.SomethingLeftBehind || counter.Any(slot => slot.Count != 0);

	internal string ScriptMode => mode.ToString();

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
		foreach (var slot in counter)
			slot.ClearItem();

		mode = Mode.Counter;

		base.Open(closeAction);

		wares = Game.AssetProvider.PlaceLoader.LoadMerchantWares(PlaceEvent.WaresIndex);
		wareCounts = Game.State.GetWareCounts(PlaceEvent.WaresIndex);

		ShowSlots();
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

		if (screen is ItemAmountScreen)
		{
			var action = amountEntered;
			amountEntered = null;

			if (Game.CurrentAmount > 0)
				action?.Invoke(Game.CurrentAmount);
		}

		ShowSlots();
	}

	private void ShowSlots()
	{
		for (int i = 0; i < SlotCount; i++)
		{
			IItem? item;
			int count;

			switch (mode)
			{
				case Mode.Buy:
				case Mode.View:
					item = wareCounts[i] == 0 ? null : wares[i];
					count = item == null ? 0 : wareCounts[i];
					break;
				case Mode.Sell:
				{
					var slot = Game.State.ActivePartyMember!.Inventory[i];
					item = slot.Count == 0 ? null : slot.Item;
					count = item == null ? 0 : slot.Count;
					break;
				}
				default:
					item = counter[i].Count == 0 ? null : counter[i].Item;
					count = item == null ? 0 : counter[i].Count;
					break;
			}

			if (item == null)
				itemContainers[i].ClearItem();
			else
				itemContainers[i].SetItem(count, item);
		}

		RequestButtonSetup();
	}

	protected override void SetupPlaceButtons(ButtonGrid buttonGrid)
	{
		buttonGrid.SetButton(BuyButtonIndex, ButtonType.BuyItem);
		buttonGrid.SetButton(TransferButtonIndex, ButtonType.GiveItem);

		if (Place == null)
			return;

		bool anyWares = wareCounts.Any(count => count != 0);
		bool counterFull = counter.All(slot => slot.Count != 0);
		bool inCounterMode = mode == Mode.Counter;

		buttonGrid.EnableButton(BuyButtonIndex, inCounterMode && PlaceGold != 0 && anyWares && !counterFull);
		buttonGrid.EnableButton(TransferButtonIndex, inCounterMode && counter.Any(slot => slot.Count != 0));

		if (!MagicMerchant)
		{
			buttonGrid.SetButton(SellButtonIndex, ButtonType.SellItem);
			buttonGrid.SetButton(ViewButtonIndex, ButtonType.ExamineItem);
			buttonGrid.EnableButton(SellButtonIndex, inCounterMode && Game.State.ActivePartyMember!.Inventory.Any(slot => slot.Count != 0));
			buttonGrid.EnableButton(ViewButtonIndex, inCounterMode && anyWares);
		}
	}

	protected override void PlaceButtonClicked(int index)
	{
		switch (index)
		{
			case BuyButtonIndex:
				StartMode(Mode.Buy, Message.WhatToBuy);
				break;
			case SellButtonIndex:
				StartMode(Mode.Sell, Message.WhatToSell);
				break;
			case ViewButtonIndex:
				StartMode(Mode.View, Message.ExamineWhichItem);
				break;
			case TransferButtonIndex:
				StartMode(Mode.Transfer, Message.TransferWhichItem);
				break;
		}
	}

	private void StartMode(Mode mode, Message message)
	{
		this.mode = mode;
		ShowMessage(message, false);
		ShowSlots();
	}

	private void EndMode()
	{
		mode = Mode.Counter;
		HideMessage();
		ShowSlots();
	}

	private void AskForAmount(IItem item, int maxAmount, Message message, Action<int> entered)
	{
		if (maxAmount <= 1)
		{
			entered(1);
			return;
		}

		amountEntered = entered;
		ItemAmountScreen.Setup(Game, item, message);
		Game.CurrentAmount = 0;
		Game.CurrentMaxAmount = maxAmount;
		Game.ScreenHandler.PushScreen(ScreenType.PlaceItemAmount);
	}

	private IText ItemQuestion(IItem item, int count, Message message)
	{
		var name = Game.GetFullText(item.GetName(Game.AssetProvider.TextLoader));
		var amount = count == 1 ? "" : $" x {count}";

		return Game.AssetProvider.TextLoader.FromString($"{name}{amount} {Game.GetFullText(Game.LoadMessageText(message))}");
	}

	/// <summary>
	/// Clicks a slot (0-based) in the current mode. This is also used by scenario scripts.
	/// </summary>
	internal void SlotClicked(int slot)
	{
		switch (mode)
		{
			case Mode.Buy:
				Buy(slot);
				break;
			case Mode.Sell:
				Sell(slot);
				break;
			case Mode.View:
				View(slot);
				break;
			case Mode.Transfer:
				Transfer(slot);
				break;
		}
	}

	private void Buy(int slot)
	{
		var ware = wares[slot];

		if (ware == null || wareCounts[slot] == 0)
		{
			EndMode();
			return;
		}

		int price = ware.BuyPrice;

		if (price > PlaceGold)
		{
			EndMode();
			ShowMessage(Message.NotEnoughGold);
			return;
		}

		int maxAmount = 1;

		if (ware.Flags.HasFlag(ItemFlags.Stackable))
		{
			maxAmount = Math.Min(PlaceGold / Math.Max(1, price), MaxStackSize);

			if (wareCounts[slot] != UnlimitedWares)
				maxAmount = Math.Min(maxAmount, wareCounts[slot]);
		}

		AskForAmount(ware, maxAmount, Message.HowManyToBuy, amount =>
		{
			AskToBuy(ItemQuestion(ware, amount, Message.PriceForItem), price * amount, () =>
			{
				AddToCounter(ware, amount);

				if (wareCounts[slot] != UnlimitedWares)
				{
					wareCounts[slot] -= amount;
					Game.State.SetWareCounts(PlaceEvent.WaresIndex, wareCounts);
				}
			});
			EndMode();
		});
	}

	private void AddToCounter(IItem item, int count)
	{
		if (item.Flags.HasFlag(ItemFlags.Stackable))
		{
			foreach (var slot in counter.Where(slot => slot.Count != 0 && slot.Item?.Index == item.Index && slot.Count < MaxStackSize))
			{
				int added = Math.Min(count, MaxStackSize - slot.Count);
				slot.Count += (byte)added;
				count -= added;

				if (count == 0)
					return;
			}
		}

		var freeSlot = counter.FirstOrDefault(slot => slot.Count == 0);
		freeSlot?.SetItem(item.Clone(), (byte)count);
	}

	private void Sell(int slot)
	{
		var partyMember = Game.State.ActivePartyMember!;
		var itemSlot = partyMember.Inventory[slot];
		var item = itemSlot.Item;

		if (item == null || itemSlot.Count == 0)
		{
			EndMode();
			return;
		}

		// Only items which are not important and are worth something are bought.
		if (!item.Flags.HasFlag(ItemFlags.NotImportant) || item.BuyPrice < 2)
		{
			ShowMessage(Message.WontBuyThat, true, () => ShowMessage(Message.WhatToSell, false));
			return;
		}

		// The merchant pays a third of the price plus a bonus by the charisma.
		int charisma = partyMember.Attributes[GameData.Attribute.Charisma].TotalCurrent;
		int pricePerItem = item.BuyPrice / 3 + (charisma / 10) * item.BuyPrice / 100;
		int maxAmount = item.Flags.HasFlag(ItemFlags.Stackable) ? itemSlot.Count : 1;

		AskForAmount(item, maxAmount, Message.HowManyToSell, amount =>
		{
			int price = pricePerItem * amount;

			AskForPrice(ItemQuestion(item, amount, Message.OfferForItem), price, () =>
			{
				Game.RemoveInventoryItem(partyMember, slot, amount);
				PlaceGold = Math.Min(short.MaxValue, PlaceGold + price);

				if (partyMember.Inventory.All(slot => slot.Count == 0))
					EndMode();
				else
					ShowSlots();
			});
		});
	}

	private void View(int slot)
	{
		var ware = wares[slot];

		if (ware == null || wareCounts[slot] == 0)
		{
			EndMode();
			return;
		}

		Game.CurrentItem = ware;
		Game.ScreenHandler.PushScreen(ScreenType.ItemView);
	}

	private void Transfer(int slot)
	{
		var counterSlot = counter[slot];
		var item = counterSlot.Item;

		if (item == null || counterSlot.Count == 0)
		{
			EndMode();
			return;
		}

		int maxAmount = item.Flags.HasFlag(ItemFlags.Stackable) ? counterSlot.Count : 1;

		AskForAmount(item, maxAmount, Message.GiveHowMuch, amount =>
		{
			EndMode();
			StartItemTransfer(item, amount, () =>
			{
				counterSlot.Count -= (byte)amount;

				if (counterSlot.Count == 0)
					counterSlot.ClearItem();

				ShowSlots();
			});
		});
	}

	protected override bool PlaceMouseDown(Position position, MouseButtons buttons)
	{
		if (mode == Mode.Counter)
			return false;

		if (buttons == MouseButtons.Left)
		{
			for (int i = 0; i < SlotCount; i++)
			{
				if (new Rect(GetSlotPosition(i), new Size(16, 16)).Contains(position))
				{
					SlotClicked(i);
					return true;
				}
			}
		}

		// A click outside of the slots ends the mode.
		EndMode();

		return true;
	}

	public override bool KeyDown(Key key, KeyModifiers keyModifiers)
	{
		if (mode != Mode.Counter && key == Key.Escape && !WaitingForClick)
		{
			EndMode();
			return true;
		}

		return base.KeyDown(key, keyModifiers);
	}

	/// <summary>
	/// Amount input for items. The item and message are given before it is opened.
	/// </summary>
	internal sealed class ItemAmountScreen : InputAmountScreen
	{
		static IItem? item;
		static Message message = Message.HowManyToBuy;

		public static void Setup(Game game, IItem item, Message message)
		{
			ItemAmountScreen.item = item;
			ItemAmountScreen.message = message;
		}

		public override ScreenType Type { get; } = ScreenType.PlaceItemAmount;
		protected override bool DynamicContent => true;
		protected override ItemGraphic Graphic => item?.GraphicIndex ?? ItemGraphic.WishingCoins;
		protected override IText InputLabelText => item?.GetName(Game.AssetProvider.TextLoader) ?? Game.LoadUIText(UIText.Gold);
		protected override Message Message => message;
	}
}
