using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

internal class InventoryScreen : ItemGridScreen
{
	Game? game;
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
    readonly static Rect MessageDisplayArea = new(16, 50, 176, 14);
    PersonInfoView? personInfoView;
    IRenderText? message;
    IRenderText? weightLabel;
    IRenderText? weightText;
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
            game!.Cursor.Visible = false;
        };
        draggingEndedHandler = () =>
        {
            itemDragged = false;
            game!.Cursor.Visible = true;
        };
    }

    public override ScreenType Type { get; } = ScreenType.Inventory;

    protected override byte ButtonGridPaletteIndex => game?.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI] ?? 0;

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

    public override void Init(Game game)
    {
        base.Init(game);

        this.game = game;

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
            inventoryItemSlots[i] = new ItemContainer(game, InventorySlotPositions[i], 0, null, 10) { Draggable = true };
            inventoryItemSlots[i].Clicked += (mouseButtons, keyModifiers) => InventorySlotClicked(index, mouseButtons, keyModifiers);
            inventoryItemSlots[i].SlotChanged += () => UpdateInventoryItem(index);
        }

        foreach (var equipmentSlot in Enum.GetValues<EquipmentSlot>())
        {
            var targetSlot = equipmentSlot; // important to capture this for the click handler
            var slot = new ItemContainer(game, EquipmentSlotPositions[equipmentSlot], 0, null, 10) { Draggable = true };
            equippedItemSlots.Add(equipmentSlot, slot);
            slot.Clicked += (mouseButtons, keyModifiers) => EquipmentSlotClicked(targetSlot, mouseButtons, keyModifiers);
            slot.SlotChanged += () => UpdateEquipment(targetSlot);
        }
    }

    public override void Open(Game game, Action? closeAction)
	{
		base.Open(game, closeAction);

        var palette = game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI];

        game.SetLayout(Layout.Inventory, palette);
        game.Cursor.CursorType = CursorType.Sword;

        SwitchToPartyMember(game.State.CurrentInventoryIndex!.Value, true);

        var weightLabelName = game.AssetProvider.TextLoader.LoadText(new AssetIdentifier(AssetType.UIText, (int)UIText.Weight));
        weightLabel?.Delete();
        weightLabel = game.TextManager.Create(weightLabelName, 80, 15);
        weightLabel.ShowInArea(16, 178, 80, 10, 2, TextAlignment.Center);

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

    public override void Close(Game game)
    {
        HideMessage();

        CleanUpEventHandlers();

        CleanUpItems();

        personInfoView?.Destroy();
        personInfoView = null;
        weightLabel?.Delete();
        weightLabel = null;
        weightText?.Delete();
        weightText = null;

        base.Close(game);
    }

    public override void ScreenPushed(Game game, Screen screen)
    {
        CleanUpEventHandlers();

        if (!screen.Transparent)
        {
            if (personInfoView != null)
                personInfoView.Visible = false;
            if (message != null)
                message.Visible = false;
            if (weightLabel != null)
                weightLabel.Visible = false;
            if (weightText != null)
                weightText.Visible = false;
        }

        base.ScreenPushed(game, screen);
    }

    public override void ScreenPopped(Game game, Screen screen)
    {
        base.ScreenPopped(game, screen);

        SetupEventHandlers();

        if (!screen.Transparent)
        {
            if (personInfoView != null)
                personInfoView.Visible = true;
            if (message != null)
                message.Visible = true;
            if (weightLabel != null)
                weightLabel.Visible = true;
            if (weightText != null)
                weightText.Visible = true;
        }
    }

    public override void KeyDown(Key key, KeyModifiers keyModifiers)
	{
        if (waitForClick)
        {
            if (key == Key.Space || key == Key.Escape)
                EndClickWait();

            return;
        }

        if (key == Key.Escape)
            game!.ScreenHandler.PopScreen();

        base.KeyDown(key, keyModifiers);
	}

	public override void MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
        if (waitForClick)
        {
            EndClickWait();
            return;
        }

        if (itemDragged && buttons == MouseButtons.Right)
        {
            ItemContainer.AbortDrag();
            return;
        }

        foreach (var inventorySlot in inventoryItemSlots)
        {
            if (inventorySlot.MouseClick(position, buttons, keyModifiers))
                return;
        }

        foreach (var equippedItemSlot in equippedItemSlots)
        {
            if (equippedItemSlot.Value.MouseClick(position, buttons, keyModifiers))
                return;
        }

        base.MouseDown(position, buttons, keyModifiers);
    }

    public override void MouseMove(Position position, MouseButtons buttons)
    {
        base.MouseMove(position, buttons);

        ItemContainer.UpdateDragPosition(game!, position);
    }

    public void SwitchToPartyMember(int index, bool force)
    {
        if (!force && game!.State.CurrentInventoryIndex == index)
            return;

        CleanUpItems();

        game!.State.SetCurrentInventory(index);
        partyMember = game.State.CurrentInventory!;
        var graphicLoader = game!.AssetProvider.GraphicLoader;
        var uiPaletteIndex = game!.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI];

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
        personInfoView = new(game, partyMember, index, uiPaletteIndex);

        var weightString = game.AssetProvider.TextLoader.LoadText(new AssetIdentifier(AssetType.UIText, (int)UIText.WeightTwoValues)).GetString();
        weightString = Game.InsertNumberIntoString(weightString, " KG", false, partyMember.TotalWeight / 1000, 3, '0');
        var maxWeight = partyMember.Attributes[GameData.Attribute.Strength].CurrentValue; // TODO: bonus value?
        weightString = Game.InsertNumberIntoString(weightString, "/", true, maxWeight, 3, '0');
        int colorIndex = partyMember.MentalConditions.HasFlag(MentalCondition.Overloaded) ? 1 : 15; // TODO: is the mental condition correct?
        if (weightString[0] == 1) // The weight string might contain a SetInk command. We overwrite it to match the color.
            weightString = $"\x1{(char)colorIndex}{weightString[2..]}";
        weightText?.Delete();
        weightText = game.TextManager.Create(weightString, colorIndex);
        weightText.ShowInArea(16, 186, 80, 10, 2, TextAlignment.Center);

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
                game?.ScreenHandler.PopScreen();
                game?.ScreenHandler.PushScreen(ScreenType.CharacterStats);
                break;
            case 1: // Drop item
                game?.ScreenHandler.PushScreen(ScreenType.InventoryDropItem);
                break;
            case 2:
                game?.ScreenHandler.PopScreen();
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
        game!.Cursor.CursorType = CursorType.Sword;
        HideMessage();
        game.UntrapMouse();

        if (closeAfterClick)
            game!.ScreenHandler.PopScreen();
    }

    internal override void ShowMessage(Message messageIndex, bool waitForClick = true, bool closeAfterClick = false)
    {
        message?.Delete();

        message = game!.TextManager.Create(game.AssetProvider.TextLoader.LoadText(new AssetIdentifier(AssetType.Message, (int)messageIndex)), MessageDisplayArea.Size.Width, 15);
        message.ShowInArea(MessageDisplayArea, 20, TextAlignment.Left);

        // TODO: Scrolling

        this.waitForClick = waitForClick;
        this.closeAfterClick = closeAfterClick;

        if (waitForClick)
        {
            game.Cursor.CursorType = CursorType.Zzz;
            game.TrapMouse(MessageDisplayArea);
        }
    }

    internal override void HideMessage()
    {
        message?.Delete();
        message = null;
    }

    internal override void PickItem(int? index)
    {
        // TODO
    }

    internal class DropItemScreen : ItemPickerScreen<InventoryScreen>
    {
        public override ScreenType Type { get; } = ScreenType.InventoryDropItem;

        public override Message Message { get; } = Message.DropWhichItem;

        public override Rect MouseTrapArea { get; } = MessageDisplayArea;
    }
}