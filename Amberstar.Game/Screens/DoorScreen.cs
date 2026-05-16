using Amber.Common;
using Amber.Renderer;
using Amberstar.Game.Events;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Events;
using Amberstar.GameData.Serialization;
using static System.Net.Mime.MediaTypeNames;

namespace Amberstar.Game.Screens;

internal class DoorScreen : ButtonGridScreen
{
    Game? game;
    bool trapFound = false;
    bool trapDisarmed = false;
    bool itemDragged = false;
    bool pickItem = false;
    bool waitForClick = false;
    bool closeAfterClick = false;
    int lockpickReduction = 0;
    readonly Action draggingStartedHandler;
    readonly Action draggingEndedHandler;
    readonly ItemContainer[] inventoryItemSlots = new ItemContainer[ICharacter.InventorySlotCount];
    readonly static Position[] InventorySlotPositions = new Position[ICharacter.InventorySlotCount];
    readonly static Rect MessageDisplayArea = new(112, 49, 192, 48);
    ISprite? image;
    IRenderText? message;
    DoorEvent? doorEvent;

    static DoorScreen()
    {
        for (int i = 0; i < InventorySlotPositions.Length; i++)
        {
            int column = i % 3;
            int row = i / 3;
            var position = new Position(112 + column * 32, 37 + 44 + row * 32);

            InventorySlotPositions[i] = position;
        }
    }

    public DoorScreen()
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

    public override ScreenType Type { get; } = ScreenType.Door;

    protected override byte ButtonGridPaletteIndex => game?.PaletteIndexProvider.Get80x80ImagePaletteIndex(Image80x80.LockedDoor) ?? 0;

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

    private void SetupEventHandlers()
    {
        ItemContainer.DraggingStarted += draggingStartedHandler;
        ItemContainer.DraggingEnded += draggingEndedHandler;
        game!.State.ActivePartyMemberChanged += UpdateItems;
    }

    private void CleanUpEventHandlers()
    {
        ItemContainer.DraggingStarted -= draggingStartedHandler;
        ItemContainer.DraggingEnded -= draggingEndedHandler;
        game!.State.ActivePartyMemberChanged -= UpdateItems;
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
        itemDragged = false;
        pickItem = false;

        UpdateItems();
        SetupEventHandlers();
    }

    private void CleanUpItems()
    {
        for (int i = 0; i < inventoryItemSlots.Length; i++)
            inventoryItemSlots[i].ClearItem();
    }

    public override void Close(Game game)
    {
        HideMessage();

        CleanUpEventHandlers();

        CleanUpItems();

        if (image != null)
        {
            image.Visible = false;
            image = null;
        }

        base.Close(game);
    }

    public override void ScreenPushed(Game game, Screen screen)
    {
        CleanUpEventHandlers();

        if (!screen.Transparent)
        {
            if (message != null)
                message.Visible = false;
            if (image != null)
                image.Visible = false;

            inventoryItemSlots.ToList().ForEach(slot => slot.Visible = false);
        }

        base.ScreenPushed(game, screen);
    }

    public override void ScreenPopped(Game game, Screen screen)
    {
        base.ScreenPopped(game, screen);

        SetupEventHandlers();

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

        base.MouseDown(position, buttons, keyModifiers);
    }

    public override void MouseMove(Position position, MouseButtons buttons)
    {
        base.MouseMove(position, buttons);

        ItemContainer.UpdateDragPosition(game!, position);
    }

    public void UpdateItems()
    {
        CleanUpItems();

        if (!pickItem)
            return;

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
                // TODO
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

    private void ShowMessage(Message messageIndex, bool waitForClick = true, bool closeAfterClick = false)
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

    private void HideMessage()
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
}