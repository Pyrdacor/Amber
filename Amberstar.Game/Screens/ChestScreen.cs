using Amber.Common;
using Amberstar.Game.Events;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

internal class ChestScreen : LockedScreen<ChestEvent>
{
    bool chestHasItems = false;
    bool chestHasGold = false;

    protected override Layout Layout { get; } = Layout.Chest;

    public override ScreenType Type { get; } = ScreenType.Chest;

    protected override void Unlocked(Message message)
    {
        AfterWaitClickAction = ShowOpenChest;

        Game.SaveEvent(LockedEvent.Index);
        ShowMessage(message, true);
    }

    public override void Open(Game game, Action? closeAction)
    {
        // TODO: If hidden, add search skill check

        var chestEvent = (game.EventHandler.CurrentEvent as ChestEvent)!;
        // TODO: If you have the Amberstar, every chest will be open (there is a bit in the savegame [Special_item_flags bit 1])
        bool lockOpened = chestEvent.LockpickReduction == 0 || game.IsCurrentEventSaved();

        Image = lockOpened ? Image80x80.OpenChest : Image80x80.LockedChest;

        base.Open(game, closeAction);

        if (lockOpened)
        {
            LockOpened = true;
            ShowOpenChest();
        }
    }

    private void ShowOpenChest()
    {
        Image = Image80x80.OpenChest;

        if (UpdateChestItems() == 0 && UpdateChestGold() == 0)
        {
            // If empty, just close
            Game.ScreenHandler.PopScreen();
            return;
        }

        RequestButtonSetup();

        if (LockedEvent.TextIndex < 26) // TODO: Maybe check for != 255 instead?
        {
            // Note: Don't use game.State.MapIndex as on world maps this could be another piece of the map!
            // TODO: Still this could be wrong, if the text event is on another world map than the player (rare case).
            // TODO: See TextBoxScreen.
            int mapIndex = Game.State.GetIndexOfMapWithPlayer();
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

            // TODO: Show items, etc

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
        var chestGold = Game.State.GetChestGold(chestIndex);

        chestHasGold = chestGold != 0;

        // TODO: update display

        return chestGold;
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

        Game.ScreenHandler.PushScreen(ScreenType.ChestGiveItem);
    }

    protected override void LowerButtonClicked()
    {
        if (!LockOpened || !chestHasGold)
            return;

        // TODO: give gold
    }

    protected override void LowerRightButtonClicked()
    {
        if (!LockOpened || !chestHasGold)
            return;

        // TODO: distribute gold
    }

    protected override (ButtonType Type, bool Enabled) ProvideCenterButton()
    {
        return (ButtonType.ExamineItem, LockOpened && chestHasItems);
    }

    protected override (ButtonType Type, bool Enabled) ProvideRightButton()
    {
        return (ButtonType.GiveItem, LockOpened && chestHasItems);
    }

    protected override (ButtonType Type, bool Enabled) ProvideLowerButton()
    {
        return (ButtonType.GiveGold, LockOpened && chestHasGold);
    }

    protected override (ButtonType Type, bool Enabled) ProvideLowerRightButton()
    {
        return (ButtonType.DistributeGold, LockOpened && chestHasGold);
    }

    internal override void PickItem(ScreenType sourceScreen, int? index)
    {
        switch (sourceScreen)
        {
            case ScreenType.LockedUseItem:
                base.PickItem(sourceScreen, index);
                break;
            case ScreenType.ChestExamineItem:
                // TODO
                break;
            case ScreenType.ChestGiveItem:
                // TODO
                break;
        }
    }

    internal abstract class ChestItemScreen(ScreenType screenType, Message message) : ItemPickerScreen
    {
        public override ScreenType Type { get; } = screenType;

        public override Rect MouseTrapArea { get; } = itemArea;

        public override Message Message { get; } = message;

        public override Rect ItemTooltipArea { get; } = itemTooltipArea;
    }

    internal class GiveItemScreen() : ChestItemScreen(ScreenType.ChestGiveItem, Message.TransferWhichItem);

    internal class ExamineItemScreen() : ChestItemScreen(ScreenType.ChestExamineItem, Message.ExamineWhichItem)
    {
        public override CursorType HoverCursorType { get; } = CursorType.Eye;
    }

    // TODO
    //internal class GiveGoldScreen : InputNumberScreen
}