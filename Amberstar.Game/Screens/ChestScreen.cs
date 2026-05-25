using Amber.Common;
using Amberstar.Game.Events;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

internal class ChestScreen : LockedScreen<ChestEvent>
{
    readonly static Rect goldDisplayArea = new(112, 37 + 76, 64, 16);
    bool chestHasItems = false;
    int chestGold = 0;
    bool allowDragAndDrop = false;
    Label? goldLabel;
    Label? goldDisplay;
    int? currentChestSlot = null; // where the item was dragged from
    bool goldDragging = false;

    protected override Layout Layout { get; } = Layout.Chest;

    public override ScreenType Type { get; } = ScreenType.Chest;

    protected override void Unlocked(Message message)
    {
        AfterWaitClickAction = ShowOpenChest;

        Game.SaveEvent(LockedEvent.Index);
        ShowMessage(message, true);
    }

    public override void Init(Game game)
    {
        base.Init(game);

        var goldLabelText = game.AssetProvider.TextLoader.LoadText(new(AssetType.UIText, (int)UIText.Gold));

        var (x, y, width, height) = goldDisplayArea;
        var goldLabel = AddLabel(x, y, goldLabelText, width, height);
        goldLabel.Alignment = TextAlignment.Center;
        goldLabel.PaletteIndex = ButtonGridPaletteIndex;
        goldLabel.Visible = false;
        this.goldLabel = goldLabel;

        var goldDisplay = AddLabel(x, y + 7, "0", width, height);
        goldDisplay.Alignment = TextAlignment.Center;
        goldDisplay.PaletteIndex = ButtonGridPaletteIndex;
        goldDisplay.Visible = false;
        this.goldDisplay = goldDisplay;
    }

    public override void Open(Game game, Action? closeAction)
    {
        var chestEvent = (game.EventHandler.CurrentEvent as ChestEvent)!;

        if (chestEvent.Hidden)
        {
            int searchSkill = game.State.ActivePartyMember!.Skills[Skill.Search].TotalCurrent;

            if (!game.Probe(searchSkill))
            {
                game.ScreenHandler.PopScreen();
                return;
            }
        }
        
        // TODO: If you have the Amberstar, every chest will be open (there is a bit in the savegame [Special_item_flags bit 1])
        bool lockOpened = chestEvent.LockpickReduction == 0 || game.IsCurrentEventSaved();

        Image = lockOpened ? Image80x80.OpenChest : Image80x80.LockedChest;
        goldLabel!.Visible = lockOpened;
        goldDisplay!.Visible = lockOpened;

        base.Open(game, closeAction);

        allowDragAndDrop = false;

        if (lockOpened)
        {
            LockOpened = true;
            ShowOpenChest();
        }
    }

    public override void ScreenPopped(Game game, Screen screen)
    {
        base.ScreenPopped(game, screen);

        if (screen is GiveGoldScreen && Game.CurrentAmount > 0)
        {
            if (Game.SetHandIconsByGold(Game.CurrentAmount) > 0)
            {
                goldDragging = true;
                Game.TrapMouseInPortraitArea();
                Game.Cursor.CursorType = CursorType.Gold;
            }
            else
            {
                Game.ResetStatusIcons();
                ShowMessage(Message.NoMemberCanCarryThatMuchGold);
            }
        }
    }

    private void ShowOpenChest()
    {
        Image = Image80x80.OpenChest;

        int itemCount = UpdateChestItems();
        int goldAmount = UpdateChestGold();

        if (itemCount == 0 && goldAmount == 0)
        {
            // If empty, just close
            Game.ScreenHandler.PopScreen();
            return;
        }

        allowDragAndDrop = Game.IsOptionSet(GameOptions.AdvancedItemPickup);

        RequestButtonSetup();

        if (LockedEvent.TextIndex < 26) // TODO: Maybe check for != 255 instead?
        {
            int mapIndex = Game.EventHandler.CurrentEventMapIndex;
            var text = Game.AssetProvider.TextLoader.LoadText(new(AssetType.MapText, mapIndex));
            text = text.GetTextBlock(LockedEvent.TextIndex);
            ShowText(text, true);
        }
    }

    /// <summary>
    /// Checks and displays the chest's items.
    /// 
    /// Returns the number of existing items.
    /// </summary>
    private int UpdateChestItems()
    {
        CleanUpItems();

        var chestIndex = LockedEvent.ChestIndex;
        var chestSlotBits = Game.State.GetChestSlotBits(chestIndex);
        chestHasItems = chestSlotBits != 0;

        if (chestHasItems)
        {
            int itemCount = 0;
            var chest = Game.AssetProvider.ChestLoader.LoadChest(chestIndex);
            var items = new List<IItem?>(chest.Items);

            for (int i = 0; i < IChest.SlotCount; i++)
            {
                if (items[i] != null)
                {
                    if ((chestSlotBits & (1 << i)) == 0)
                        items[i] = null;
                    else
                        itemCount++;
                }                
            }

            if (itemCount == 0)
            {
                chestHasItems = false;
                return 0;
            }

            for (int i = 0; i < IChest.SlotCount; i++)
            {
                SetItem(i, 1, items[i]);
            }

            return itemCount;
        }
        else
        {
            return 0;
        }
    }

    /// <summary>
    /// Checks and displays the chest's gold.
    /// 
    /// Returns the amount of gold.
    /// </summary>
    private int UpdateChestGold()
    {
        var chestIndex = LockedEvent.ChestIndex;
        chestGold = Game.State.GetChestGold(chestIndex);

        goldDisplay!.SetText($"{chestGold:00000}", 15, TextManager.TransparentPaper, ButtonGridPaletteIndex);

        goldLabel!.Visible = true;
        goldDisplay.Visible = true;

        return chestGold;
    }

    public override void MouseMove(Position position, MouseButtons buttons)
    {
        ItemContainer.UpdateDragPosition(position);

        base.MouseMove(position, buttons);
    }

    public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
    {
        if (buttons == MouseButtons.Right)
        {
            if (ItemContainer.IsDragging)
            {
                AbortItemDrag();
                return true;
            }

            if (goldDragging)
            {
                AbortGoldDrag();
                return true;
            }

            int? characterSlotIndex = Game.TestPartyPortraitHit(position);

            if (characterSlotIndex != null)
            {
                Game.OpenInventory(characterSlotIndex.Value);
                return true;
            }
        }
        else if (buttons == MouseButtons.Left)
        {
            if (ItemContainer.IsDragging)
            {
                int? characterSlotIndex = Game.TestPartyPortraitHit(position);

                if (characterSlotIndex != null)
                {
                    // TODO: allow many stacked items? use count param
                    if (Game.TryAddItem(characterSlotIndex.Value, Game.CurrentItem!))
                    {
                        Game.State.SetChestSlotBit(LockedEvent.ChestIndex, currentChestSlot!.Value, false);
                        ItemContainer.ConsumeDragged(Game);
                        Game.UntrapMouse();
                        Game.ResetStatusIcons();

                        RequestButtonSetup();

                        // TODO: If chest is empty, close screen
                    }
                    else
                    {
                        AbortItemDrag();
                    }

                    return true;
                }
            }
            else if (goldDragging)
            {
                var gold = Game.CurrentAmount;

                AbortGoldDrag();

                int? characterSlotIndex = Game.TestPartyPortraitHit(position);

                if (characterSlotIndex != null)
                {
                    if (Game.TryAddGold(characterSlotIndex.Value, gold))
                    {
                        chestGold -= gold;

                        Game.State.SetChestGold(LockedEvent.ChestIndex, chestGold);
                        goldDisplay!.SetText($"{chestGold:00000}", 15, TextManager.TransparentPaper, ButtonGridPaletteIndex);

                        if (chestGold == 0)
                            RequestButtonSetup();

                        // TODO: If chest is empty, close screen
                    }
                }

                return true;
            }
        }

        return base.MouseDown(position, buttons, keyModifiers);
    }

    public override bool KeyDown(Key key, KeyModifiers keyModifiers)
    {
        if (key == Key.Escape)
        {
            if (ItemContainer.IsDragging)
            {
                AbortItemDrag();
                return true;
            }
            else if (goldDragging)
            {
                AbortGoldDrag();
                return true;
            }
        }

        return base.KeyDown(key, keyModifiers);
    }

    private void AbortItemDrag()
    {
        Game.UntrapMouse();
        ItemContainer.AbortDrag(Game);
        Game.ResetStatusIcons();
    }

    private void AbortGoldDrag()
    {
        goldDragging = false;
        Game.CurrentAmount = 0;
        Game.Cursor.CursorType = CursorType.Sword;
        Game.UntrapMouse();
        Game.ResetStatusIcons();
    }

    protected override void CenterButtonClicked()
    {
        if (!LockOpened || !chestHasItems)
            return;

        Game.ScreenHandler.PushScreen(ScreenType.ChestExamineItem);
    }

    protected override void RightButtonClicked()
    {
        if (!LockOpened || !chestHasItems)
            return;

        if (allowDragAndDrop)
        {
            // TODO: distribute items
        }
        else
        {
            Game.ScreenHandler.PushScreen(ScreenType.ChestGiveItem);
        }
    }

    protected override void LowerButtonClicked()
    {
        if (!LockOpened || chestGold == 0)
            return;

        Game.CurrentMaxAmount = chestGold;
        Game.ScreenHandler.PushScreen(ScreenType.ChestGiveGold);
    }

    protected override void LowerRightButtonClicked()
    {
        if (!LockOpened || chestGold == 0)
            return;

        chestGold = Game.DistributeGold(chestGold);

        Game.State.SetChestGold(LockedEvent.ChestIndex, chestGold);
        goldDisplay!.SetText($"{chestGold:00000}", 15, TextManager.TransparentPaper, ButtonGridPaletteIndex);

        if (chestGold == 0)
            RequestButtonSetup();
    }

    protected override (ButtonType Type, bool Enabled) ProvideCenterButton()
    {
        return (ButtonType.ExamineItem, LockOpened && chestHasItems);
    }

    protected override (ButtonType Type, bool Enabled) ProvideRightButton()
    {
        return (allowDragAndDrop ? ButtonType.DistributeItems : ButtonType.GiveItem, LockOpened && chestHasItems);
    }

    protected override (ButtonType Type, bool Enabled) ProvideLowerButton()
    {
        return (ButtonType.GiveGold, LockOpened && chestGold != 0);
    }

    protected override (ButtonType Type, bool Enabled) ProvideLowerRightButton()
    {
        return (ButtonType.DistributeGold, LockOpened && chestGold != 0);
    }

    internal override void PickItem(ScreenType sourceScreen, int? index)
    {
        if (index == null)
        {
            CleanUpItems();
            return;
        }

        switch (sourceScreen)
        {
            case ScreenType.LockedUseItem:
                base.PickItem(sourceScreen, index);
                break;
            case ScreenType.ChestExamineItem:
                Game.CurrentItem = ItemContainers[index.Value].Item;
                Game.ScreenHandler.PushScreen(ScreenType.ItemView);
                break;
            case ScreenType.ChestGiveItem:
                Game.CurrentItem = ItemContainers[index.Value].Item;
                if (Game.SetHandIconsByItem(Game.CurrentItem!) > 0) // TODO: item count
                {
                    currentChestSlot = index;
                    Game.CurrentItem = ItemContainers[index.Value].Item;
                    ItemContainers[index.Value].StartDragging();
                    Game.TrapMouseInPortraitArea();
                }
                else
                {
                    Game.ResetStatusIcons();
                    ShowMessage(Message.NoMemberHasRoomForItem);
                }
                break;
        }
    }

    internal abstract class ChestItemScreen(ScreenType screenType, Message message) : ItemPickerScreen
    {
        public override ScreenType Type { get; } = screenType;

        public override Rect MouseTrapArea { get; } = itemArea;

        public override Message Message { get; } = message;

        public override Rect ItemTooltipArea { get; } = itemTooltipArea;

        public override bool HideItemsAfterPicking { get; } = false;
    }

    internal class GiveItemScreen() : ChestItemScreen(ScreenType.ChestGiveItem, Message.TransferWhichItem);

    internal class ExamineItemScreen() : ChestItemScreen(ScreenType.ChestExamineItem, Message.ExamineWhichItem)
    {
        public override CursorType HoverCursorType { get; } = CursorType.Eye;
    }

    internal class GiveGoldScreen : InputAmountScreen
    {
        IText? inputLabelText;

        public override ScreenType Type { get; } = ScreenType.ChestGiveGold;
        protected override ItemGraphic Graphic { get; } = ItemGraphic.WishingCoins;
        protected override IText InputLabelText => inputLabelText!;
        protected override Message Message { get; } = Message.GiveHowMuch;

        public override void Init(Game game)
        {
            inputLabelText = game.AssetProvider.TextLoader.LoadText(new(AssetType.UIText, (int)UIText.Gold));

            base.Init(game);
        }
    }
}