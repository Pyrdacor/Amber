using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

internal class InventoryScreen : ItemGridScreen
{
    IPartyMember? partyMember;
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
    PersonInfoView? personInfoView;
    Label? message;
    Label? weightText;
    bool ignoreItemChangeEvents = false;

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

    internal override byte ButtonGridPaletteIndex => Game?.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI] ?? 0;

    internal override ItemContainer[] ItemContainers => [.. equippedItemSlots.Values, .. inventoryItemSlots];

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

        bool hasInventoryItems = partyMember!.Inventory.Any(itemSlot => itemSlot.Count > 0);
        buttonGrid.EnableButton(1, hasInventoryItems);
        buttonGrid.EnableButton(6, hasInventoryItems);

        bool hasAnyItems = hasInventoryItems && partyMember.Equipment.Any(itemSlot => itemSlot.Value.Count > 0);
        buttonGrid.EnableButton(3, hasInventoryItems);
        buttonGrid.EnableButton(5, hasAnyItems);

        buttonGrid.EnableButton(7, partyMember.Gold > 0);
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

    public override void Init()
    {
        base.Init();

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
    }

    public override bool KeyDown(Key key, KeyModifiers keyModifiers)
	{
        if (waitForClick)
        {
            if (key == Key.Space || key == Key.Escape)
                EndClickWait();

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
        personInfoView = new(Game, partyMember, index, uiPaletteIndex);

        var weightString = Game.AssetProvider.TextLoader.LoadText(new AssetIdentifier(AssetType.UIText, (int)UIText.WeightTwoValues)).GetString();
        weightString = Game.InsertNumberIntoString(weightString, " KG", false, partyMember.TotalWeight / 1000, 3, '0');
        var maxWeight = partyMember.Attributes[GameData.Attribute.Strength].CurrentValue; // TODO: bonus value?
        weightString = Game.InsertNumberIntoString(weightString, "/", true, maxWeight, 3, '0');
        int colorIndex = partyMember.MentalConditions.HasFlag(MentalCondition.Overloaded) ? 1 : 15; // TODO: is the mental condition correct?
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
                // TODO
                break;
            case 4: // Gather gold?
                // TODO
                break;
            case 5: // Examine item
                // TODO
                break;
            case 6: // Give item
                // TODO
                break;
            case 7: // Give gold
                // TODO
                break;
            case 8: // Give food
                // TODO
                break;
        }
    }

    private void InventorySlotClicked(int index, MouseButtons mouseButtons, KeyModifiers keyModifiers)
    {
        var slot = inventoryItemSlots[index];

        if (slot.ItemCount <= 0)
            return;

        var item = slot.Item!;

        if (item.Type == ItemType.MonsterItem)
            return;

        // TODO: If in battle, check if can be equipped during battle here

        var targetSlot = item.EquipmentSlot;

        if (targetSlot == null) // Not equipable
        {
            ShowMessage(Message.ItemNotEquippable);
            return;
        }

        if (targetSlot == EquipmentSlot.RightFinger && partyMember!.Equipment[targetSlot.Value].Count > 0)
            targetSlot = EquipmentSlot.LeftFinger;

        var targetItemSlot = equippedItemSlots[targetSlot.Value];

        if (targetItemSlot.ItemCount > 0)
        {
            ShowMessage(Message.ItemNotEquippable);
            return;
        }

        if (!item.UsableClasses.HasFlag((ClassFlags)(1 << (int)partyMember!.Class)))
        {
            ShowMessage(Message.WrongClass);
            return;
        }

        if (item.Genders != GenderFlags.Both && !item.Genders.HasFlag((GenderFlags)(1 << (int)partyMember.Gender)))
        {
            ShowMessage(Message.WrongGender);
            return;
        }

        if (item.Hands > 2 - partyMember.UsedHands)
        {
            ShowMessage(Message.NotEnoughFreeHands);
            return;
        }

        if (item.Fingers > 2 - partyMember.UsedFingers)
        {
            ShowMessage(Message.NotEnoughFreeFingers);
            return;
        }

        targetItemSlot.AddItem(1, item);
        slot.ReduceItemCount(1);

        // TODO: update values of party, etc
    }

    private void EquipmentSlotClicked(EquipmentSlot equipmentSlot, MouseButtons mouseButtons, KeyModifiers keyModifiers)
    {
        var slot = equippedItemSlots[equipmentSlot];

        if (slot.ItemCount == 0)
            return;

        var item = slot.Item!;

        if (item.Type == ItemType.MonsterItem)
            return;

        // TODO: If in battle, check if can be unequipped during battle here

        if (item.Flags.HasFlag(ItemFlags.Cursed))
        {
            ShowMessage(Message.ItemIsCursed);
            return;
        }

        int targetSlotIndex = -1;
        var inventorySlots = inventoryItemSlots.ToList();

        if (item.Flags.HasFlag(ItemFlags.Stackable))
        {
            targetSlotIndex = inventorySlots.FindIndex(slot => slot.Item?.Index == item.Index && slot.ItemCount < 99);
        }

        if (targetSlotIndex == -1)
            targetSlotIndex = inventorySlots.FindIndex(slot => slot.Empty);

        if (targetSlotIndex == -1)
        {
            ShowMessage(Message.NoRoomForItem);
            return;
        }

        var targetSlot = inventoryItemSlots[targetSlotIndex];

        targetSlot.AddItem(1, item);
        slot.ReduceItemCount(1);
    }

    private void EndClickWait()
    {
        waitForClick = false;
        Game.Cursor.CursorType = CursorType.Sword;
        HideMessage();
        Game.UntrapMouse();

        if (closeAfterClick)
            Game.ScreenHandler.PopScreen();
    }

    internal override void ShowMessage(Message messageIndex, bool waitForClick = true, bool closeAfterClick = false)
    {
        var messageText = Game.AssetProvider.TextLoader.LoadText(new AssetIdentifier(AssetType.Message, (int)messageIndex));
        message!.SetText(messageText);
        message.Visible = true;

        // TODO: Scrolling

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
        // TODO
    }

    internal class DropItemScreen : ItemPickerScreen
    {
        public override ScreenType Type { get; } = ScreenType.InventoryDropItem;

        public override Message Message { get; } = Message.DropWhichItem;

        public override Rect MouseTrapArea { get; } = messageDisplayArea;

        public override Rect ItemTooltipArea { get; } = itemTooltipArea;
    }
}