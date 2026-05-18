using Amber.Common;
using Amber.Renderer;
using Amberstar.Game.Events;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Events;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

internal abstract class LockedScreen<TEvent> : ItemGridScreen
    where TEvent : Event, ILockedEvent
{
    Game? game;
    bool trapFound = false;
    bool trapDisarmed = false;
    bool waitForClick = false;
    bool closeAfterClick = false;
    bool lockOpened = false;
    int lockpickReduction = 0;
    readonly ItemContainer[] inventoryItemSlots = new ItemContainer[ICharacter.InventorySlotCount];
    readonly static Position[] inventorySlotPositions = new Position[ICharacter.InventorySlotCount];
    readonly static Rect messageDisplayArea = new(112, 49, 192, 48);
    protected readonly static Rect itemArea;
    protected readonly static Rect itemTooltipArea;
    ISprite? image;
    IRenderText? message;
    TEvent? lockedEvent;
    Image80x80 image80x80 = Image80x80.LockedDoor;

    static LockedScreen()
    {
        const int slotsPerRow = 6;

        for (int i = 0; i < inventorySlotPositions.Length; i++)
        {
            int column = i % slotsPerRow;
            int row = i / slotsPerRow;
            var position = new Position(16 + column * 32, 37 + 109 + row * 32);

            inventorySlotPositions[i] = position;
        }

        var firstSlot = inventorySlotPositions[0];
        var lastSlot = inventorySlotPositions[^1];

        itemArea = new(firstSlot.X, firstSlot.Y, lastSlot.X + 16 - firstSlot.X, lastSlot.Y + 16 - firstSlot.Y);
        itemTooltipArea = new(messageDisplayArea.Position.X, messageDisplayArea.Position.Y + 7, messageDisplayArea.Size.Width, 7);
    }

    protected Game Game => game!;

    protected TEvent LockedEvent => lockedEvent!;

    protected bool LockOpened
    {
        get => lockOpened;
        set => lockOpened = value;
    }

    protected Image80x80 Image
    {
        private get => image80x80;
        set
        {
            image80x80 = value;

            if (image != null)
            {
                var textureAtlas = game!.GetRenderLayer(Layer.UI).Config.Texture!;

                image.TextureOffset = textureAtlas.GetOffset(game.GraphicIndexProvider.Get80x80ImageIndex(image80x80));
                image.PaletteIndex = game.PaletteIndexProvider.Get80x80ImagePaletteIndex(image80x80); ;
            }
        }
    }

    protected abstract Layout Layout { get; }

    protected virtual uint? AllowedUnlockItemIndex { get; } = null;

    public sealed override bool Transparent { get; } = false;

    internal sealed override byte ButtonGridPaletteIndex => game?.PaletteIndexProvider.Get80x80ImagePaletteIndex(Image) ?? 0;

    internal sealed override ItemContainer[] ItemContainers => inventoryItemSlots;

    protected Action? AfterWaitClickAction { get; set; } = null;

    protected virtual (ButtonType Type, bool Enabled) ProvideCenterButton() => (ButtonType.Empty, false);
    protected virtual (ButtonType Type, bool Enabled) ProvideRightButton() => (ButtonType.Empty, false);
    protected virtual (ButtonType Type, bool Enabled) ProvideLowerButton() => (ButtonType.Empty, false);
    protected virtual (ButtonType Type, bool Enabled) ProvideLowerRightButton() => (ButtonType.Empty, false);

    protected sealed override void SetupButtons(ButtonGrid buttonGrid)
    {
        var partyMember = game?.State.ActivePartyMember;

        if (partyMember == null)
            return;

        var centerButton = ProvideCenterButton();
        var rightButton = ProvideRightButton();
        var lowerButton = ProvideLowerButton();
        var lowerRightButton = ProvideLowerRightButton();

        // Upper row
        buttonGrid.SetButton(0, ButtonType.PickLock);
        buttonGrid.SetButton(1, ButtonType.UseItem);
        buttonGrid.SetButton(2, ButtonType.Exit);
        // Middle row
        buttonGrid.SetButton(3, ButtonType.FindTrap);
        buttonGrid.SetButton(4, centerButton.Type);
        buttonGrid.SetButton(5, rightButton.Type);
        // Lower row
        buttonGrid.SetButton(6, ButtonType.DisarmTrap);
        buttonGrid.SetButton(7, lowerButton.Type);
        buttonGrid.SetButton(8, lowerRightButton.Type);

        buttonGrid.EnableButton(0, !lockOpened);
        buttonGrid.EnableButton(1, !lockOpened && partyMember!.Inventory.Any(itemSlot => itemSlot.Count > 0));
        buttonGrid.EnableButton(3, !lockOpened && !trapFound && !partyMember.MentalConditions.HasFlag(MentalCondition.Blind));
        buttonGrid.EnableButton(6, !lockOpened && trapFound && !trapDisarmed && !partyMember.MentalConditions.HasFlag(MentalCondition.Blind));

        if (centerButton.Type != ButtonType.Empty)
            buttonGrid.EnableButton(4, centerButton.Enabled);
        if (rightButton.Type != ButtonType.Empty)
            buttonGrid.EnableButton(5, rightButton.Enabled);
        if (lowerButton.Type != ButtonType.Empty)
            buttonGrid.EnableButton(7, lowerButton.Enabled);
        if (lowerRightButton.Type != ButtonType.Empty)
            buttonGrid.EnableButton(8, lowerRightButton.Enabled);
    }

    public override void Init(Game game)
    {
        base.Init(game);

        this.game = game;

        for (int i = 0; i < inventoryItemSlots.Length; i++)
        {
            inventoryItemSlots[i] = new ItemContainer(game, inventorySlotPositions[i], 0, null, 10);
        }
    }

    public override void Open(Game game, Action? closeAction)
    {
        base.Open(game, closeAction);

        lockedEvent = (game.EventHandler.CurrentEvent as TEvent)!;
        lockpickReduction = lockedEvent.LockpickReduction;

        var palette = game.PaletteIndexProvider.Get80x80ImagePaletteIndex(Image);

        game.SetLayout(Layout, palette);
        game.Cursor.CursorType = CursorType.Sword;

        var layer = game.GetRenderLayer(Layer.UI);
        image = layer.SpriteFactory!.Create();
        var textureAtlas = layer.Config.Texture!;
        image.Position = new(16, 49);
        image.Size = new(80, 80);
        image.Opaque = true;
        image.TextureOffset = textureAtlas.GetOffset(game.GraphicIndexProvider.Get80x80ImageIndex(Image));
        image.PaletteIndex = palette;
        image.Visible = true;

        trapFound = false;
        trapDisarmed = false;
        waitForClick = false;
        closeAfterClick = false;
        lockOpened = false;
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

        if (!lockOpened)
        {
            // Note: At this point, ActiveScreen is already the previous one!
            if (game!.ScreenHandler.ActiveScreen is Map2DScreen map2dScreen)
                map2dScreen.ResetPartyPosition();
            else if (game!.ScreenHandler.ActiveScreen is Map3DScreen map3dScreen)
                map3dScreen.ResetPartyPosition();
            else
                game.State.ResetPartyPosition();
        }

        base.Close(game);
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

        AfterWaitClickAction?.Invoke();
        AfterWaitClickAction = null;

        if (closeAfterClick)
            game!.ScreenHandler.PopScreen();
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

        return base.MouseDown(position, buttons, keyModifiers);
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

    protected virtual void CenterButtonClicked()
    {
        // do nothing
    }

    protected virtual void RightButtonClicked()
    {
        // do nothing
    }

    protected virtual void LowerButtonClicked()
    {
        // do nothing
    }

    protected virtual void LowerRightButtonClicked()
    {
        // do nothing
    }

    protected sealed override void ButtonClicked(int index)
    {
        switch (index)
        {
            case 0: // Pick lock
                TryPickLock();
                break;
            case 1: // Use item
                game?.ScreenHandler.PushScreen(ScreenType.LockedUseItem);
                break;
            case 2: // Exit
                game?.ScreenHandler.PopScreen();
                break;
            case 3: // Find trap
                TryFindTrap();
                break;
            case 4:
                CenterButtonClicked();
                break;
            case 5:
                RightButtonClicked();
                break;
            case 6: // Disarm trap
                TryDisarmTrap();
                break;
            case 7:
                LowerButtonClicked();
                break;
            case 8:
                LowerRightButtonClicked();
                break;
        }
    }

    internal sealed override void ShowMessage(Message messageIndex, bool waitForClick = true, bool closeAfterClick = false)
    {
        var text = game!.AssetProvider.TextLoader.LoadText(new AssetIdentifier(AssetType.Message, (int)messageIndex));

        ShowText(text, waitForClick, closeAfterClick);
    }

    protected void ShowText(IText text, bool waitForClick = true, bool closeAfterClick = false)
    {
        message?.Delete();

        message = game!.TextManager.Create(text, messageDisplayArea.Size.Width, 15, 0, ButtonGridPaletteIndex);
        message.ShowInArea(messageDisplayArea, 20, TextAlignment.Left);

        // TODO: Scrolling

        this.waitForClick = waitForClick;
        this.closeAfterClick = closeAfterClick;

        if (waitForClick)
        {
            game.Cursor.CursorType = CursorType.Zzz;
            game.TrapMouse(messageDisplayArea);
        }
    }

    internal sealed override void HideMessage()
    {
        message?.Delete();
        message = null;
    }

    protected abstract void Unlocked(Message message);

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
            lockOpened = true;
            Unlocked(Message.LockOpened);
        }
        else
        {
            // Reduce chance of lockpicking by 10% for each failed attempt, up to a maximum of 99%.
            lockpickReduction = Math.Min(lockpickReduction + 10, 99);

            if (trapDisarmed || lockedEvent!.TrapType == TrapType.None)
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
        if (lockedEvent!.TrapType == TrapType.None)
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
        game!.TriggerTrap(lockedEvent!.TrapType, lockedEvent!.TrapDamage);
    }

    internal override void PickItem(ScreenType sourceScreen, int? index)
    {
        if (index == null)
        {
            CleanUpItems();
            return;
        }

        var item = inventoryItemSlots[index.Value];

        if (item.Item != null && AllowedUnlockItemIndex != null && item.Item.Index == AllowedUnlockItemIndex)
        {
            // Is it the right key/item?

            if (item.Item.Flags.HasFlag(ItemFlags.DestroyAfterUsage) && lockedEvent!.SaveEvent)
            {
                // We show the previous message again until the destroy animation is done.
                ShowMessage(Message.UseWhichItem, false);
                game!.EnableInput(false);

                // TODO: Move this weight update logic (and similar logic) to party functions
                game.State.ActivePartyMember!.TotalWeight = (uint)Math.Max(0, (long)game.State.ActivePartyMember.TotalWeight - item.Item.Weight);
                item.ReduceItemCount(1, true, () =>
                {
                    lockOpened = true;
                    game.EnableInput(true);
                    Unlocked(Message.ItemOpensDoor);
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

                if (lockedEvent!.LockpickReduction >= 100) // Fully locked?
                {
                    CleanUpItems();
                    RequestButtonSetup(); // "use item" button might be disabled now
                    ShowMessage(Message.LockpickBreaks);
                }
                else
                {
                    lockOpened = true;
                    Unlocked(Message.LockpickOpensLock);
                }
            });
        }
        else
        {
            // Reopen item selection
            game?.ScreenHandler.PushScreen(ScreenType.LockedUseItem);
        }
    }

    internal class UseItemScreen : ItemPickerScreen
    {
        public override ScreenType Type { get; } = ScreenType.LockedUseItem;

        public override Rect MouseTrapArea { get; } = itemArea;

        public override Message Message { get; } = Message.UseWhichItem;

        public override Rect ItemTooltipArea { get; } = itemTooltipArea;
    }
}