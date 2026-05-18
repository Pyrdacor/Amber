using Amberstar.Game.Events;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

internal class ChestScreen : LockedScreen<ChestEvent>
{
    // TODO
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
        var chestEvent = (game.EventHandler.CurrentEvent as ChestEvent)!;
        LockOpened = chestEvent.LockpickReduction == 0 || game.IsCurrentEventSaved();

        Image = LockOpened ? Image80x80.OpenChest : Image80x80.LockedChest;

        base.Open(game, closeAction);

        if (LockOpened)
            ShowOpenChest();
    }

    private void ShowOpenChest()
    {
        Image = Image80x80.OpenChest;
        // TODO: Show items, etc
    }

    protected override void CenterButtonClicked()
    {
        if (!LockOpened || !chestHasItems)
            return;

        // TODO: examine item
    }

    protected override void RightButtonClicked()
    {
        if (!LockOpened || !chestHasItems)
            return;

        // TODO: give item
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
}