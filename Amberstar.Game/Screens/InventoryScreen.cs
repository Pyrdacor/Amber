using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

internal class InventoryScreen : ItemGridScreen, IWaitForClickScreen
{
    IPartyMember? partyMember;
    // the slots are refreshed after the destructions of used up items
    int runningDestructions = 0;
    Action? afterDestructions;
    bool itemDragged = false;
    bool waitForClick = false;
    bool closeAfterClick = false;
    readonly Action draggingStartedHandler;
    readonly Action draggingEndedHandler;
    readonly ItemContainer[] inventoryItemSlots = new ItemContainer[ICharacter.InventorySlotCount];
    readonly Dictionary<EquipmentSlot, ItemContainer> equippedItemSlots = [];
    readonly static Dictionary<EquipmentSlot, Position> EquipmentSlotPositions = [];
    readonly static Position[] InventorySlotPositions = new Position[ICharacter.InventorySlotCount];
    readonly static Rect messageDisplayArea = new(16, 50, 176, 14);
    readonly static Rect itemTooltipArea = new(16, 57, 176, 7);
    readonly static Rect backpackArea = new(112, 81, 80, 112);
    readonly static Rect itemArea = new(16, 81, 176, 112);
    PersonInfoView? personInfoView;
    Label? message;
    Label? weightText;
    bool ignoreItemChangeEvents = false;
    Action<int>? amountEntered;
    Func<IPartyMember, bool>? transferCheck;
    Action<IPartyMember>? transferAction;

    static InventoryScreen()
    {
        const int slotsPerRow = 3;

        foreach (var equipmentSlot in Enum.GetValues<EquipmentSlot>())
        {
            int column = (int)equipmentSlot % slotsPerRow;
            int row = (int)equipmentSlot / slotsPerRow;
            var position = new Position(16 + column * 32, 37 + 44 + row * 32);

            EquipmentSlotPositions.Add(equipmentSlot, position);
        }

        for (int i = 0; i < InventorySlotPositions.Length; i++)
        {
            int column = i % slotsPerRow;
            int row = i / slotsPerRow;
            var position = new Position(112 + column * 32, 37 + 44 + row * 32);

            InventorySlotPositions[i] = position;
        }
    }

    public InventoryScreen()
    {
        draggingStartedHandler = () =>
        {
            itemDragged = true;
            Game.Cursor.Visible = false;
        };
        draggingEndedHandler = () =>
        {
            itemDragged = false;
            Game.Cursor.Visible = true;
        };
    }

    public override ScreenType Type { get; } = ScreenType.Inventory;

    public bool WaitingForClick => waitForClick;

    internal override byte ButtonGridPaletteIndex => Game?.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI] ?? 0;

    internal override ItemContainer[] ItemContainers => [.. equippedItemSlots.Values, .. inventoryItemSlots];

    internal override ItemContainer[] BackpackItemContainers => inventoryItemSlots;

    internal override ItemContainer[] EquipmentItemContainers => [.. Enum.GetValues<EquipmentSlot>().Select(slot => equippedItemSlots[slot])];

    protected override void SetupButtons(ButtonGrid buttonGrid)
    {
        if (partyMember == null)
            return;

        // Upper row
        buttonGrid.SetButton(0, ButtonType.Stats);
        buttonGrid.SetButton(1, ButtonType.DropItem);
        buttonGrid.SetButton(2, ButtonType.Exit);
        // Middle row
        buttonGrid.SetButton(3, ButtonType.UseItem);
        buttonGrid.SetButton(4, ButtonType.GatherGold);
        buttonGrid.SetButton(5, ButtonType.ExamineItem);
        // Lower row
        buttonGrid.SetButton(6, ButtonType.GiveItem);
        buttonGrid.SetButton(7, ButtonType.GiveGold);
        buttonGrid.SetButton(8, ButtonType.GiveFood);

        // like Inv2CIL_evaluate in the original
        bool special = partyMember.Class >= Class.Animal;
        bool hasInventoryItems = partyMember.Inventory.Any(itemSlot => itemSlot.Count > 0);
        bool hasAnyItems = hasInventoryItems || partyMember.Equipment.Any(itemSlot => itemSlot.Value.Count > 0);
        bool canPoolGold = partyMember.TotalWeight < partyMember.MaxWeight() &&
            !partyMember.HasAnyConditionOf(Condition.Dead | Condition.Ashes | Condition.Dust);

        buttonGrid.EnableButton(1, hasInventoryItems);
        buttonGrid.EnableButton(3, !special && hasAnyItems);
        buttonGrid.EnableButton(4, !special && canPoolGold);
        buttonGrid.EnableButton(5, !special && hasAnyItems);
        buttonGrid.EnableButton(6, hasInventoryItems);
        buttonGrid.EnableButton(7, !special && partyMember.Gold > 0);
        buttonGrid.EnableButton(8, partyMember.Food > 0);
    }

    private void SetupEventHandlers()
    {
        ItemContainer.DraggingStarted += draggingStartedHandler;
        ItemContainer.DraggingEnded += draggingEndedHandler;
    }

    private void CleanUpEventHandlers()
    {
        ItemContainer.DraggingStarted -= draggingStartedHandler;
        ItemContainer.DraggingEnded -= draggingEndedHandler;
    }

    readonly TextScrollHandler textScrollHandler = new();

    public override void Init()
    {
        base.Init();

        // long messages are scrolled before they are closed
        textScrollHandler.ScrollEnded += EndClickWait;

        void UpdateInventoryItem(int index)
        {
            if (ignoreItemChangeEvents || partyMember == null)
                return;

            var slot = inventoryItemSlots[index];

            partyMember.Inventory[index] = new((byte)slot.ItemCount, slot.Item);
        }

        void UpdateEquipment(EquipmentSlot equipmentSlot)
        {
            if (ignoreItemChangeEvents || partyMember == null)
                return;

            var slot = equippedItemSlots[equipmentSlot];

            if (equipmentSlot == EquipmentSlot.LeftHand && slot.ItemCount == ItemContainer.TwoHandedSecondSlotMarker)
                partyMember.Equipment[equipmentSlot] = new(0, null);
            else
                partyMember.Equipment[equipmentSlot] = new((byte)slot.ItemCount, slot.Item);
        }

        for (int i = 0; i < inventoryItemSlots.Length; i++)
        {
            int index = i; // important to capture this for the click handler
            var position = InventorySlotPositions[i];
            var slot = AddItem(position.X, position.Y, item: null, count: 0, displayLayer: 10);
            slot.Draggable = true;
            slot.Clicked += (mouseButtons, keyModifiers) => InventorySlotClicked(index, mouseButtons, keyModifiers);
            slot.SlotChanged += () => UpdateInventoryItem(index);
            inventoryItemSlots[i] = slot;
        }

        foreach (var equipmentSlot in Enum.GetValues<EquipmentSlot>())
        {
            var targetSlot = equipmentSlot; // important to capture this for the click handler
            var position = EquipmentSlotPositions[equipmentSlot];
            var slot = AddItem(position.X, position.Y, item: null, count: 0, displayLayer: 10);
            slot.Draggable = true;            
            slot.Clicked += (mouseButtons, keyModifiers) => EquipmentSlotClicked(targetSlot, mouseButtons, keyModifiers);
            slot.SlotChanged += () => UpdateEquipment(targetSlot);
            equippedItemSlots.Add(equipmentSlot, slot);
        }

        var weightLabelName = Game.AssetProvider.TextLoader.LoadText(new AssetIdentifier(AssetType.UIText, (int)UIText.Weight));
        var weightLabel = AddLabel(16, 178, weightLabelName, width: 80, height: 10, displayLayer: 2);
        weightLabel.Alignment = TextAlignment.Center;

        var weightText = AddLabel(16, 186, "0", width: 80, height: 10, displayLayer: 2);
        weightText.Alignment = TextAlignment.Center;
        this.weightText = weightText;

        var (x, y, width, height) = messageDisplayArea;
        message = AddLabel(x, y, "", width, height, displayLayer: 20);
    }

    public override void Open(Action? closeAction)
	{
		base.Open(closeAction);

        var palette = Game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI];

        Game.SetLayout(Layout.Inventory, palette);
        Game.Cursor.CursorType = CursorType.Sword;

        HideMessage();

        SwitchToPartyMember(Game.State.CurrentInventoryIndex!.Value, true);
        SetupEventHandlers();

        // The inventory was opened to choose an item for a spell.
        if (Game.SpellItemMessage != null)
            Game.AddDelayedAction(0, () => Game.ScreenHandler.PushScreen(ScreenType.InventorySpellItem));

        // The inventory was opened by a healer to destroy cursed items.
        if (Game.EquipmentToDestroy is List<EquipmentSlot> equipmentToDestroy)
        {
            Game.EquipmentToDestroy = null;
            DestroyEquipment(equipmentToDestroy, () => Game.ScreenHandler.PopScreen());
        }
    }

    private void CleanUpItems()
    {
        ignoreItemChangeEvents = true;

        foreach (var equippedItemGraphic in equippedItemSlots)
            equippedItemGraphic.Value.ClearItem();

        for (int i = 0; i < inventoryItemSlots.Length; i++)
            inventoryItemSlots[i].ClearItem();

        ignoreItemChangeEvents = false;
    }

    public override void Close()
    {
        HideMessage();

        CleanUpEventHandlers();

        CleanUpItems();

        personInfoView?.Destroy();
        personInfoView = null;

        base.Close();
    }

    public override void ScreenPushed(Screen screen)
    {
        CleanUpEventHandlers();

        if (!screen.Transparent)
        {
            if (personInfoView != null)
                personInfoView.Visible = false;
            if (message != null)
                message.Visible = false;
        }

        base.ScreenPushed(screen);
    }

    public override void ScreenPopped(Screen screen)
    {
        base.ScreenPopped(screen);

        SetupEventHandlers();

        if (!screen.Transparent)
        {
            if (personInfoView != null)
                personInfoView.Visible = true;
            if (message != null)
                message.Visible = true;
        }

        if (screen is InputAmountScreen)
        {
            var action = amountEntered;
            amountEntered = null;

            if (Game.CurrentAmount > 0)
                action?.Invoke(Game.CurrentAmount);
        }
    }

    public override bool KeyDown(Key key, KeyModifiers keyModifiers)
	{
        if (waitForClick)
        {
            if (textScrollHandler.KeyDown(key, keyModifiers))
                return true;

            if (key == Key.Space || key == Key.Escape)
                EndClickWait();

            return true;
        }

        if (transferAction != null)
        {
            if (key == Key.Escape)
                EndTransfer(null);

            return true;
        }

        return base.KeyDown(key, keyModifiers);
	}

	public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
        if (waitForClick)
        {
            if (textScrollHandler.MouseDown(buttons))
                return true;

            EndClickWait();
            return true;
        }

        if (transferAction != null)
        {
            int? slot = buttons == MouseButtons.Left ? Game.TestPartyPortraitHit(position) : null;
            EndTransfer(slot == null ? null : Game.State.GetPartyMember(slot.Value));
            return true;
        }

        if (itemDragged && buttons == MouseButtons.Right)
        {
            ItemContainer.AbortDrag(Game);
            return true;
        }

        foreach (var inventorySlot in inventoryItemSlots)
        {
            if (inventorySlot.MouseClick(position, buttons, keyModifiers))
                return true;
        }

        foreach (var equippedItemSlot in equippedItemSlots)
        {
            if (equippedItemSlot.Value.MouseClick(position, buttons, keyModifiers))
                return true;
        }

        return base.MouseDown(position, buttons, keyModifiers);
    }

    public override void MouseMove(Position position, MouseButtons buttons)
    {
        base.MouseMove(position, buttons);

        ItemContainer.UpdateDragPosition(Game, position);
    }

    public void SwitchToPartyMember(int index, bool force)
    {
        if (!force && Game.State.CurrentInventoryIndex == index)
            return;

        CleanUpItems();

        Game.State.SetCurrentInventory(index);
        partyMember = Game.State.CurrentInventory!;
        var uiPaletteIndex = Game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI];

        foreach (var equipmentSlot in Enum.GetValues<EquipmentSlot>())
        {
            if (partyMember.Equipment.TryGetValue(equipmentSlot, out var equipment))
            {
                if (equipment.Item != null && equipment.Count > 0)
                    equippedItemSlots[equipmentSlot].SetItem(equipment.Count, equipment.Item);
            }
        }

        // Check for two-handed weapon
        if (partyMember.Equipment.TryGetValue(EquipmentSlot.RightHand, out var rightHandEquipment) && rightHandEquipment.Item != null
            && rightHandEquipment.Item.IsTwoHanded())
        {
            if (equippedItemSlots[EquipmentSlot.LeftHand].ItemCount > 0)
                throw new AmberException(ExceptionScope.Application, "Two-handed weapon equipped but left hand is not empty.");

            equippedItemSlots[EquipmentSlot.LeftHand].SetItem(ItemContainer.TwoHandedSecondSlotMarker, rightHandEquipment.Item);
        }

        for (int i = 0; i < partyMember.Inventory.Length; i++)
        {
            var itemSlot = partyMember.Inventory[i];

            if (itemSlot?.Item == null || itemSlot.Count <= 0)
                continue;

            inventoryItemSlots[i].SetItem(itemSlot.Count, itemSlot.Item);
        }

        personInfoView?.Destroy();
        int partyMemberIndex = Game.State.PartyCharacterIndices[index - 1];
        personInfoView = new(Game, partyMember, partyMemberIndex, uiPaletteIndex);

        var weightString = Game.AssetProvider.TextLoader.LoadText(new AssetIdentifier(AssetType.UIText, (int)UIText.WeightTwoValues)).GetString();
        weightString = Game.InsertNumberIntoString(weightString, " KG", false, partyMember.TotalWeight / 1000, 3, '0');
        var maxWeight = partyMember.MaxWeight() / 1000;
        weightString = Game.InsertNumberIntoString(weightString, "/", true, maxWeight, 3, '0');
        int colorIndex = partyMember.TotalWeight > partyMember.MaxWeight() ? 1 : 15;
        if (weightString[0] == 1) // The weight string might contain a SetInk command. We overwrite it to match the color.
            weightString = $"\x1{(char)colorIndex}{weightString[2..]}";
        weightText!.SetText(weightString, colorIndex);

        RequestButtonSetup();
    }

    // Understanding original mouse and key events:
    //
    // Mouse events start with 2 longs.
    // - First long gives the mask which should be applied to the incoming event.
    // - Second long is compared against the masked event data.
    //
    // If the result matches, the event is triggered.
    //
    // The event data is created like this: TUCB XXXX YYYY ZZZZ (every letter is a 2 bit value = 32 bits total)
    // - T: Trespass (low bit: X trespass, high bit Y trespass), trespass means out of bounds
    // - U: Unclick state (which mouse buttons have been released)
    // - C: Click state (which mouse buttons have been pressed)
    // - B: Button state (which mouse buttons are currently pressed)
    // - X: First layer index (1 byte)
    // - Y: Second layer index (1 byte)
    // - Z: Third layer index (1 byte)
    //
    // Layers are defined elsewhere and specify some areas in the UI.
    // Mouse buttons: 2 = left, 1 = right
    //
    // Button state stores the current mouse button states.
    // Click and unclick store the mouse button states at the time of the event.
    // They are cleared after each successful event. This means that if two events
    // listen for a left click, the second one won't be triggered if the first one
    // is. The button state can still be used to allow both to be triggered.
    //
    // For example: .DC.l $02ff0000,$02010000,Member_left
    //
    // This will mask the trespass, unclick and click state away (do not care).
    // It will also mask the button state to only care for left mouse down (2).
    // The ff masks the first layer. The other layers are ignored (00 and 00).
    // It then checks for layer 0 is 1, which is the portrait area.
    // The second layer would contain the member index (1 to 6) which can
    // be filtered out be the handler later. The event data is passed in d0
    // to the handler. Here Member_left.

    protected override void ButtonClicked(int index)
    {
        switch (index)
        {
            case 0: // Stats
                Game?.ScreenHandler.PopScreen();
                Game?.ScreenHandler.PushScreen(ScreenType.CharacterStats);
                break;
            case 1: // Drop item
                Game?.ScreenHandler.PushScreen(ScreenType.InventoryDropItem);
                break;
            case 2:
                Game?.ScreenHandler.PopScreen();
                break;
            case 3: // Use item
                Game.ScreenHandler.PushScreen(ScreenType.InventoryUseItem);
                break;
            case 4: // Gather gold
                Game.GatherGold(partyMember!);
                ItemActionDone(null);
                break;
            case 5: // Examine item
                Game.ScreenHandler.PushScreen(ScreenType.InventoryExamineItem);
                break;
            case 6: // Give item
                Game.ScreenHandler.PushScreen(ScreenType.InventoryGiveItem);
                break;
            case 7: // Give gold
            {
                var giver = partyMember!;
                AskForAmount(giver.Gold, ScreenType.PlaceGiveGold, amount => StartTransfer(
                    receiver => Game.CanReceiveGold(receiver, amount),
                    receiver => Game.TransferGold(giver, receiver, amount), CursorType.Gold));
                break;
            }
            case 8: // Give food
            {
                var giver = partyMember!;
                AskForAmount(giver.Food, ScreenType.PlaceGiveFood, amount => StartTransfer(
                    receiver => Game.CanReceiveFood(receiver, amount),
                    receiver => Game.TransferFood(giver, receiver, amount), CursorType.Food));
                break;
            }
        }
    }

    private void InventorySlotClicked(int index, MouseButtons mouseButtons, KeyModifiers keyModifiers)
    {
        if (mouseButtons != MouseButtons.Left)
            return;

        ItemActionDone(Game.EquipItem(partyMember!, index, Game.CurrentBattle != null));
    }

    private void EquipmentSlotClicked(EquipmentSlot equipmentSlot, MouseButtons mouseButtons, KeyModifiers keyModifiers)
    {
        if (mouseButtons != MouseButtons.Left)
            return;

        // the second hand of a two-handed weapon unequips the weapon
        if (equipmentSlot == EquipmentSlot.LeftHand && equippedItemSlots[equipmentSlot].TwoHandedSecondSlot)
            equipmentSlot = EquipmentSlot.RightHand;

        ItemActionDone(Game.UnequipItem(partyMember!, equipmentSlot, Game.CurrentBattle != null));
    }

    /// <summary>
    /// Shows the items and values after the party member data was changed
    /// and shows the given message if any.
    /// </summary>
    /// <summary>
    /// The party member whose inventory is shown.
    /// </summary>
    internal IPartyMember? PartyMember => partyMember;

    /// <summary>
    /// The last item of a slot was used up: its destruction is shown in the slot (Show_item_destruction in the
    /// original). Returns false if the slot is not shown.
    /// </summary>
    internal bool ShowItemDestruction(ItemSlot itemSlot, Action? finished = null) =>
        RunItemAnimation(itemSlot, (container, done) => container.ReduceItemCount(container.ItemCount, true, done), finished);

    /// <summary>
    /// An item sparkles in its slot when it is used or enchanted (L1212a in the original). Returns false if the slot is
    /// not shown.
    /// </summary>
    internal bool ShowItemSparkle(ItemSlot itemSlot, Action? finished = null) =>
        RunItemAnimation(itemSlot, (container, done) => container.ShowSparkle(done), finished);

    bool RunItemAnimation(ItemSlot itemSlot, Action<ItemContainer, Action> animation, Action? finished)
    {
        ItemContainer? container = null;

        foreach (var equipmentSlot in Enum.GetValues<EquipmentSlot>())
        {
            if (ReferenceEquals(partyMember?.Equipment[equipmentSlot], itemSlot))
                container = equippedItemSlots[equipmentSlot];
        }

        for (int i = 0; i < inventoryItemSlots.Length; i++)
        {
            if (ReferenceEquals(partyMember?.Inventory[i], itemSlot))
                container = inventoryItemSlots[i];
        }

        if (container?.Item == null)
            return false;

        runningDestructions++;
        animation(container, () =>
        {
            if (--runningDestructions == 0)
            {
                var action = afterDestructions;
                afterDestructions = null;
                action?.Invoke();
            }

            finished?.Invoke();
        });

        return true;
    }

    private void ItemActionDone(Message? message)
    {
        // the destruction of a used up item is shown first
        if (runningDestructions != 0)
        {
            afterDestructions += () => ItemActionDone(message);
            return;
        }

        SwitchToPartyMember(Game.State.CurrentInventoryIndex!.Value, true);

        if (message != null)
            ShowMessage(message.Value);
    }

    private void EndClickWait()
    {
        waitForClick = false;
        textScrollHandler.Detach();
        Game.Cursor.CursorType = CursorType.Sword;
        HideMessage();
        Game.UntrapMouse();

        if (closeAfterClick)
            Game.ScreenHandler.PopScreen();
    }

    internal override void ShowMessage(Message messageIndex, bool waitForClick = true, bool closeAfterClick = false)
    {
        var messageText = Game.AssetProvider.TextLoader.LoadText(new AssetIdentifier(AssetType.Message, (int)messageIndex));
        Game.ScriptTextShown(messageText);
        message!.SetText(messageText);
        message.Visible = true;

        if (waitForClick)
            textScrollHandler.Attach(message);

        this.waitForClick = waitForClick;
        this.closeAfterClick = closeAfterClick;

        if (waitForClick)
        {
            Game.Cursor.CursorType = CursorType.Zzz;
            Game.TrapMouse(messageDisplayArea);
        }
    }

    internal override void HideMessage()
    {
        message!.Visible = false;
    }

    internal override void PickItem(ScreenType sourceScreen, int? index)
    {
        if (index == null && sourceScreen != ScreenType.InventorySpellItem)
            return;

        switch (sourceScreen)
        {
            case ScreenType.InventoryDropItem:
                DropItem(index!.Value);
                break;
            case ScreenType.InventoryGiveItem:
                GiveItem(index!.Value);
                break;
            case ScreenType.InventoryUseItem:
                UseItem(index!.Value);
                break;
            case ScreenType.InventorySpellItem:
            {
                ItemSlot? itemSlot = null;
                bool equipped = true;

                if (index != null)
                {
                    itemSlot = Game.SpellItemEquipmentOnly
                        ? partyMember!.Equipment[(EquipmentSlot)index.Value]
                        : GetItemSlot(index.Value, out equipped);
                }

                Game.SpellItemPicked(itemSlot, equipped);
                break;
            }
            case ScreenType.InventoryExamineItem:
                Game.CurrentItem = GetItemSlot(index!.Value, out _).Item;
                Game.ScreenHandler.PushScreen(ScreenType.ItemView);
                break;
        }
    }

    /// <summary>
    /// Gets the item slot of an index in <see cref="ItemContainers"/>.
    /// </summary>
    private ItemSlot GetItemSlot(int index, out bool equipped)
    {
        equipped = index < equippedItemSlots.Count;

        return equipped
            ? partyMember!.Equipment[(EquipmentSlot)index]
            : partyMember!.Inventory[index - equippedItemSlots.Count];
    }

    /// <summary>
    /// Uses an equipped item or an item of the backpack (UseItem_entry in the original).
    /// </summary>
    private void UseItem(int index)
    {
        var user = partyMember!;
        var itemSlot = GetItemSlot(index, out bool equipped);
        var item = itemSlot.Item;

        if (item == null || itemSlot.Count == 0)
            return;

        // items which are used on the map (e.g. keys or a shovel)
        if (Game.CurrentBattle == null && Game.UseItemOnMap(item))
        {
            if (item.Flags.HasFlag(ItemFlags.DestroyAfterUsage))
            {
                Game.DestroyUsedItem(user, itemSlot, equipped);

                // the inventory is left after the destruction
                if (itemSlot.Count == 0 && ShowItemDestruction(itemSlot, () => Game.ScreenHandler.PopScreen()))
                    return;
            }

            Game.ScreenHandler.PopScreen();
            return;
        }

        // items with a spell
        if (item.SpellCharges != 0)
        {
            if (!item.UsableClasses.HasFlag((ClassFlags)(1 << (int)user.Class)))
            {
                ShowMessage(Message.WrongClass);
                return;
            }

            // In battles the spell becomes the action of the item's owner.
            if (Game.CurrentBattle != null)
            {
                Game.UseItemInBattle(user, itemSlot, equipped);
                return;
            }

            var location = Game.ScreenHandler.FindScreen(ScreenType.Camp) != null ? SpellLocation.Camp : Game.GetMapSpellLocation();
            Game.CastItemSpell(user, itemSlot, equipped, location, () => ItemActionDone(null));
            return;
        }

        switch (item.Type)
        {
            case ItemType.SpecialItem:
            {
                var specialItem = (SpecialItems)(1 << (item.SpecialIndex - 1));

                if (Game.State.SpecialItems.HasFlag(specialItem))
                {
                    ShowMessage(Message.SameItemAlreadyInUse);
                    return;
                }

                Game.State.SpecialItems |= specialItem;
                Game.DestroyUsedItem(user, itemSlot, equipped, itemSlot.Count);
                ShowItemDestruction(itemSlot);
                ItemActionDone(Message.ItemFulfillsSpecialPurposeNow);
                break;
            }
            case ItemType.TextScroll:
            {
                // The text file of CODETXT.AMB and the text index are stored in the item.
                var texts = Game.AssetProvider.TextLoader.LoadText(new AssetIdentifier(AssetType.ItemText, item.SpecialIndex));
                Game.ShowTextMessage(texts.GetTextBlock(item.TextIndex));
                break;
            }
            case ItemType.Transportation: // magic disc
                if (Game.CurrentBattle != null || Game.ScreenHandler.FindMapScreen() is not Map2DScreen { IsWilderness: true })
                {
                    ShowMessage(Message.FlyingDiscNotUsableHere);
                    return;
                }

                Game.State.TravelType = TravelType.MagicDisc;
                Game.ScreenHandler.PopScreen();
                break;
        }
    }

    /// <summary>
    /// Drops items of a backpack slot (Do_DropItem in the original).
    /// </summary>
    private void DropItem(int slotIndex)
    {
        var owner = partyMember!;
        var itemSlot = owner.Inventory[slotIndex];
        var item = itemSlot.Item!;

        if (!item.CanThrowAway())
        {
            ShowMessage(Message.ItemCannotBeDropped);
            return;
        }

        AskForItemAmount(item, itemSlot.Count, Message.HowManyItemsToDrop, amount =>
            Game.AskForConfirmation(Game.LoadMessageText(Message.ReallyDropItem), () =>
            {
                Game.RemoveInventoryItem(owner, slotIndex, amount);
                ItemActionDone(null);
            }));
    }

    /// <summary>
    /// Gives items of a backpack slot to another party member (Do_TransferItem in the original).
    /// </summary>
    private void GiveItem(int slotIndex)
    {
        var giver = partyMember!;
        var itemSlot = giver.Inventory[slotIndex];
        var item = itemSlot.Item!;

        AskForItemAmount(item, itemSlot.Count, Message.GiveHowMuch, amount => StartTransfer(
            receiver => Game.CanReceiveItems(receiver, item, amount),
            receiver => Game.TransferItems(giver, slotIndex, receiver, amount), CursorType.Sword, item));
    }

    private void AskForItemAmount(IItem item, int count, Message message, Action<int> entered)
    {
        if (!item.IsStackable() || count <= 1)
        {
            entered(1);
            return;
        }

        MerchantScreen.ItemAmountScreen.Setup(Game, item, message);
        AskForAmount(count, ScreenType.PlaceItemAmount, entered);
    }

    private void AskForAmount(int maxAmount, ScreenType screenType, Action<int> entered)
    {
        amountEntered = entered;
        Game.CurrentAmount = 0;
        Game.CurrentMaxAmount = maxAmount;
        Game.ScreenHandler.PushScreen(screenType);
    }

    /// <summary>
    /// Lets the player choose the receiver by clicking a portrait (TIVS module in the original).
    /// </summary>
    private void StartTransfer(Func<IPartyMember, bool> canReceive, Action<IPartyMember> transfer, CursorType cursorType, IItem? item = null)
    {
        if (Game.SetHandIcons(partyMember!, canReceive) == 0)
        {
            Game.ResetStatusIcons();
            ShowMessage(Message.NoMemberHasRoomForItem);
            return;
        }

        transferCheck = canReceive;
        transferAction = transfer;
        ShowMessage(Message.WhomToTransferTo, false);
        Game.TrapMouseInPortraitArea();
        Game.Cursor.CursorType = cursorType;
        // the cursor shows the given item
        Game.Cursor.ShowItem(item);
    }

    private void EndTransfer(IPartyMember? receiver)
    {
        var canReceive = transferCheck;
        var transfer = transferAction;
        transferCheck = null;
        transferAction = null;

        HideMessage();
        Game.Cursor.CursorType = CursorType.Sword;
        Game.Cursor.ShowItem(null);
        Game.UntrapMouse();
        Game.ResetStatusIcons();

        if (receiver != null && receiver != partyMember && canReceive!(receiver))
        {
            transfer!(receiver);
            ItemActionDone(null);
        }
    }

    internal abstract class InventoryItemPickerScreen : ItemPickerScreen
    {
        public override Rect MouseTrapArea => BackpackOnly ? backpackArea : itemArea;

        public override Rect ItemTooltipArea { get; } = itemTooltipArea;

        public override bool HideItemsAfterPicking { get; } = false;
    }

    internal class DropItemScreen : InventoryItemPickerScreen
    {
        public override ScreenType Type { get; } = ScreenType.InventoryDropItem;

        public override Message Message { get; } = Message.DropWhichItem;

        public override bool BackpackOnly { get; } = true;
    }

    internal class UseItemScreen : InventoryItemPickerScreen
    {
        public override ScreenType Type { get; } = ScreenType.InventoryUseItem;

        public override Message Message { get; } = Message.UseWhichItem;
    }

    internal class ExamineItemScreen : InventoryItemPickerScreen
    {
        public override ScreenType Type { get; } = ScreenType.InventoryExamineItem;

        public override Message Message { get; } = Message.ExamineWhichItem;
    }

    /// <summary>
    /// Chooses an item for a spell like identification.
    /// </summary>
    internal class SpellItemScreen : InventoryItemPickerScreen
    {
        public override ScreenType Type { get; } = ScreenType.InventorySpellItem;

        public override Message Message => Game.SpellItemMessage ?? Message.WhichItem;

        public override bool EquipmentOnly => Game.SpellItemEquipmentOnly;
    }

    /// <summary>
    /// Shows the items and values after they were changed (e.g. by a spell).
    /// </summary>
    internal void Refresh() => SwitchToPartyMember(Game.State.CurrentInventoryIndex!.Value, true);

    /// <summary>
    /// Destroys equipped items one after another with the curse animation (RemCurse_DisInit in
    /// PLACES.ASM of the original, used by healers). The mouse is disabled meanwhile and there is
    /// a pause of 1 second before and after the destruction.
    /// </summary>
    void DestroyEquipment(List<EquipmentSlot> equipmentSlots, Action finishedHandler)
    {
        var partyMember = Game.State.CurrentInventory!;
        long pauseTicks = Game.TicksPerSecond;

        Game.EnableInput(false);
        Game.AddDelayedAction(pauseTicks, () => DestroyNext(0));

        void DestroyNext(int index)
        {
            if (index == equipmentSlots.Count)
            {
                Game.AddDelayedAction(pauseTicks, () =>
                {
                    Game.EnableInput(true);
                    finishedHandler();
                });
                return;
            }

            var equipmentSlot = equipmentSlots[index];
            int count = partyMember.Equipment[equipmentSlot].Count;

            Game.RemoveEquipment(partyMember, equipmentSlot, count);
            equippedItemSlots[equipmentSlot].ReduceItemCount(count, true, () =>
            {
                Refresh(); // e.g. the second slot of a two-handed weapon
                DestroyNext(index + 1);
            });
        }
    }

    internal class GiveItemScreen : InventoryItemPickerScreen
    {
        public override ScreenType Type { get; } = ScreenType.InventoryGiveItem;

        public override Message Message { get; } = Message.TransferWhichItem;

        public override bool BackpackOnly { get; } = true;
    }
}