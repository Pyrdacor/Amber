using Amber.Common;
using Amber.Renderer;
using Amberstar.Game.Events;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Events;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

internal class DoorScreen : ItemGridScreen
{
    Game? game;
    bool trapFound = false;
    bool trapDisarmed = false;
    bool waitForClick = false;
    bool closeAfterClick = false;
    bool doorOpened = false;
    int lockpickReduction = 0;
    readonly ItemContainer[] inventoryItemSlots = new ItemContainer[ICharacter.InventorySlotCount];
    readonly static Position[] InventorySlotPositions = new Position[ICharacter.InventorySlotCount];
    readonly static Rect MessageDisplayArea = new(112, 49, 192, 48);
    readonly static Rect ItemArea;
    ISprite? image;
    IRenderText? message;
    DoorEvent? doorEvent;

    static DoorScreen()
    {
        const int slotsPerRow = 6;

        for (int i = 0; i < InventorySlotPositions.Length; i++)
        {
            int column = i % slotsPerRow;
            int row = i / slotsPerRow;
            var position = new Position(16 + column * 32, 37 + 109 + row * 32);

            InventorySlotPositions[i] = position;
        }

        var firstSlot = InventorySlotPositions[0];
        var lastSlot = InventorySlotPositions[^1];

        ItemArea = new(firstSlot.X, firstSlot.Y, lastSlot.X + 16 - firstSlot.X, lastSlot.Y + 16 - firstSlot.Y);
    }

    public override ScreenType Type { get; } = ScreenType.Door;

    protected override byte ButtonGridPaletteIndex => game?.PaletteIndexProvider.Get80x80ImagePaletteIndex(Image80x80.LockedDoor) ?? 0;

    internal override ItemContainer[] ItemContainers => inventoryItemSlots;

    protected override void SetupButtons(ButtonGrid buttonGrid)
    {
        var partyMember = game?.State.ActivePartyMember;

        if (partyMember == null)
            return;

        // Upper row
        buttonGrid.SetButton(0, ButtonType.PickLock);
        buttonGrid.SetButton(1, ButtonType.UseItem);
        buttonGrid.SetButton(2, ButtonType.Exit);
        // Middle row
        buttonGrid.SetButton(3, ButtonType.FindTrap);
        buttonGrid.SetButton(4, ButtonType.Empty);
        buttonGrid.SetButton(5, ButtonType.Empty);
        // Lower row
        buttonGrid.SetButton(6, ButtonType.DisarmTrap);
        buttonGrid.SetButton(7, ButtonType.Empty);
        buttonGrid.SetButton(8, ButtonType.Empty);

        bool hasInventoryItems = partyMember!.Inventory.Any(itemSlot => itemSlot.Count > 0);
        buttonGrid.EnableButton(1, hasInventoryItems);
        buttonGrid.EnableButton(3, !trapFound && !partyMember.MentalConditions.HasFlag(MentalCondition.Blind));
        buttonGrid.EnableButton(6, trapFound && !trapDisarmed && !partyMember.MentalConditions.HasFlag(MentalCondition.Blind));
    }

    public override void Init(Game game)
    {
        base.Init(game);

        this.game = game;

        for (int i = 0; i < inventoryItemSlots.Length; i++)
        {
            int index = i; // important to capture this for the click handler
            inventoryItemSlots[i] = new ItemContainer(game, InventorySlotPositions[i], 0, null, 10) { Draggable = true };
            inventoryItemSlots[i].Clicked += (mouseButtons, keyModifiers) => InventorySlotClicked(index, mouseButtons, keyModifiers);
        }
    }

    public override void Open(Game game, Action? closeAction)
    {
        base.Open(game, closeAction);

        doorEvent = (game.EventHandler.CurrentEvent as DoorEvent)!;
        lockpickReduction = doorEvent.LockpickReduction;

        var palette = game.PaletteIndexProvider.Get80x80ImagePaletteIndex(Image80x80.LockedDoor);

        game.SetLayout(Layout.Door, palette);
        game.Cursor.CursorType = CursorType.Sword;

        var layer = game.GetRenderLayer(Layer.UI);
        image = layer.SpriteFactory!.Create();
        var textureAtlas = layer.Config.Texture!;
        image.Position = new(16, 49);
        image.Size = new(80, 80);
        image.Opaque = true;
        image.TextureOffset = textureAtlas.GetOffset(game.GraphicIndexProvider.Get80x80ImageIndex(Image80x80.LockedDoor));
        image.PaletteIndex = palette;
        image.Visible = true;

        trapFound = false;
        trapDisarmed = false;
        waitForClick = false;
        closeAfterClick = false;
        doorOpened = false;
        lockpickReduction = 0;
    }

    private void CleanUpItems()
    {
        for (int i = 0; i < inventoryItemSlots.Length; i++)
            inventoryItemSlots[i].ClearItem();
    }

    public override void Close(Game game)
    {
        HideMessage();
        CleanUpItems();

        if (image != null)
        {
            image.Visible = false;
            image = null;
        }

        if (!doorOpened)
        {
            // Note: At this point, ActiveScreen is already the previous one!
            if (game!.ScreenHandler.ActiveScreen is Map2DScreen map2dScreen)
                map2dScreen.ResetPartyPosition();
            else
                game.State.ResetPartyPosition();
        }

        base.Close(game);

        if (doorOpened)
        {
            var extraEvent = doorEvent?.OpenedEventIndex;

            if (extraEvent != null && extraEvent != 0)
            {
                IEventProvider eventProvider;

                if (game!.ScreenHandler.ActiveScreen is Map2DScreen map2dScreen)
                    eventProvider = map2dScreen.Map;
                else if (game!.ScreenHandler.ActiveScreen is Map3DScreen map3dScreen)
                    eventProvider = map3dScreen.Map;
                else
                    return;

                var @event = eventProvider.Events[extraEvent.Value - 1];
                game.EventHandler.HandleEvent(EventTrigger.Move, Event.CreateEvent(@event, extraEvent.Value), eventProvider);
            }
        }
    }

    public override void ScreenPushed(Game game, Screen screen)
    {
        if (!screen.Transparent)
        {
            if (message != null)
                message.Visible = false;
            if (image != null)
                image.Visible = false;

            inventoryItemSlots.ToList().ForEach(slot => slot.Visible = false);
        }

        if (screen is UseItemScreen)
            UpdateItems();

        base.ScreenPushed(game, screen);
    }

    public override void ScreenPopped(Game game, Screen screen)
    {
        base.ScreenPopped(game, screen);

        if (!screen.Transparent)
        {
            if (message != null)
                message.Visible = true;
            if (image != null)
                image.Visible = true;

            inventoryItemSlots.ToList().ForEach(slot => slot.Visible = true);
        }
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

        base.MouseDown(position, buttons, keyModifiers);
    }

    public void UpdateItems()
    {
        CleanUpItems();

        var partyMember = game!.State.ActivePartyMember;

        if (partyMember == null)
            return;

        for (int i = 0; i < partyMember.Inventory.Length; i++)
        {
            var itemSlot = partyMember.Inventory[i];

            if (itemSlot?.Item == null || itemSlot.Count <= 0)
                continue;

            inventoryItemSlots[i].SetItem(itemSlot.Count, itemSlot.Item);
        }

        RequestButtonSetup();
    }

    protected override void ButtonClicked(int index)
    {
        switch (index)
        {
            case 0: // Pick lock
                TryPickLock();
                break;
            case 1: // Use item
                game?.ScreenHandler.PushScreen(ScreenType.DoorUseItem);
                break;
            case 2: // Exit
                game?.ScreenHandler.PopScreen();
                break;
            case 3: // Find trap
                TryFindTrap();
                break;
            case 6: // Disarm trap
                TryDisarmTrap();
                break;
        }
    }

    private void InventorySlotClicked(int index, MouseButtons mouseButtons, KeyModifiers keyModifiers)
    {
        var slot = inventoryItemSlots[index];

        if (slot.ItemCount <= 0)
            return;

        var item = slot.Item!;

        // TODO
    }

    internal override void ShowMessage(Message messageIndex, bool waitForClick = true, bool closeAfterClick = false)
    {
        message?.Delete();

        message = game!.TextManager.Create(game.AssetProvider.TextLoader.LoadText(new AssetIdentifier(AssetType.Message, (int)messageIndex)), MessageDisplayArea.Size.Width, 15, 0, ButtonGridPaletteIndex);
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

    private void TryPickLock()
    {
        if (lockpickReduction == 100)
        {
            ShowMessage(Message.LockCannotBeOpened);
            return;
        }

        int lockpickSkill = game!.State.ActivePartyMember!.Skills[Skill.PickLocks].TotalCurrent;
        lockpickSkill -= lockpickReduction;

        if (game.Probe(lockpickSkill))
        {
            doorOpened = true;
            game.SaveEvent(doorEvent!.Index);
            ShowMessage(Message.LockOpened, true, true);
        }
        else
        {
            // Reduce chance of lockpicking by 10% for each failed attempt, up to a maximum of 99%.
            lockpickReduction = Math.Min(lockpickReduction + 10, 99);

            if (trapDisarmed || doorEvent!.TrapType == TrapType.None)
            {
                ShowMessage(Message.LockCannotBeOpened);
            }
            else
            {
                int dexterity = game.State.ActivePartyMember!.Attributes[GameData.Attribute.Dexterity].TotalCurrent;

                if (game.Probe(dexterity))
                {
                    ShowMessage(Message.HeardStrangeNoise);
                }
                else
                {
                    TriggerTrap();
                }
            }
        }
    }

    private void TryFindTrap()
    {
        if (doorEvent!.TrapType == TrapType.None)
        {
            ShowMessage(Message.NoTrapDiscovered);
            return;
        }

        int findTrapSkill = game!.State.ActivePartyMember!.Skills[Skill.FindTraps].TotalCurrent;

        if (game.Probe(findTrapSkill))
        {
            ShowMessage(Message.TrapDiscovered);
            trapFound = true;
            RequestButtonSetup();
        }
        else
        {
            ShowMessage(Message.NoTrapDiscovered);
        }
    }

    private void TryDisarmTrap()
    {
        int disarmTrapSkill = game!.State.ActivePartyMember!.Skills[Skill.DisarmTraps].TotalCurrent;

        if (game.Probe(disarmTrapSkill))
        {
            ShowMessage(Message.TrapDisarmed);
            trapDisarmed = true;
            RequestButtonSetup();
        }
        else
        {
            int dexterity = game.State.ActivePartyMember!.Attributes[GameData.Attribute.Dexterity].TotalCurrent;

            if (game.Probe(dexterity))
            {
                ShowMessage(Message.HeardStrangeNoise);
            }
            else
            {
                TriggerTrap();
            }

            ShowMessage(Message.NoTrapDiscovered);
        }
    }

    private void TriggerTrap()
    {
        game!.TriggerTrap(doorEvent!.TrapType, doorEvent!.TrapDamage);
    }

    internal override void PickItem(int? index)
    {
        if (index == null)
        {
            CleanUpItems();
            return;
        }

        var item = inventoryItemSlots[index.Value];

        if (item.Item != null && item.Item.Index == doorEvent!.ItemIndex)
        {
            // Is it the right key/item?

            if (item.Item.Flags.HasFlag(ItemFlags.DestroyAfterUsage) && doorEvent.SaveEvent)
            {
                // We show the previous message again until the destroy animation is done.
                ShowMessage(Message.UseWhichItem, false);
                game!.EnableInput(false);

                // TODO: Move this weight update logic (and similar logic) to party functions
                game.State.ActivePartyMember!.TotalWeight = (uint)Math.Max(0, (long)game.State.ActivePartyMember.TotalWeight - item.Item.Weight);
                item.ReduceItemCount(1, true, () =>
                {
                    doorOpened = true;
                    game.SaveEvent(doorEvent!.Index);
                    game.EnableInput(true);
                    ShowMessage(Message.LockOpened, true, true);
                });
            }
        }
        else if (item.Item != null && item.Item.SpellSchool == SpellSchool.Special && item.Item.SpellIndex == (byte)SpecialSpell.PickLock)
        {
            // Is it a lockpick?

            // We show the previous message again until the destroy animation is done.
            ShowMessage(Message.UseWhichItem, false);
            game!.EnableInput(false);

            // TODO: Move this weight update logic (and similar logic) to party functions
            game.State.ActivePartyMember!.TotalWeight = (uint)Math.Max(0, (long)game.State.ActivePartyMember.TotalWeight - item.Item.Weight);
            item.ReduceItemCount(1, true, () =>
            {
                game.EnableInput(true);

                if (doorEvent!.LockpickReduction >= 100) // Fully locked?
                {
                    CleanUpItems();
                    RequestButtonSetup(); // "use item" button might be disabled now
                    ShowMessage(Message.LockpickBreaks);
                }
                else
                {
                    doorOpened = true;
                    game.SaveEvent(doorEvent!.Index);
                    ShowMessage(Message.LockpickOpensLock, true, true);
                }
            });
        }
        else
        {
            // Reopen item selection
            game?.ScreenHandler.PushScreen(ScreenType.DoorUseItem);
        }
    }

    internal class UseItemScreen : ItemPickerScreen<DoorScreen>
    {
        public override ScreenType Type { get; } = ScreenType.DoorUseItem;

        public override Rect MouseTrapArea { get; } = ItemArea;

        public override Message Message { get; } = Message.UseWhichItem;
    }
}