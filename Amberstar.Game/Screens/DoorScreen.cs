using Amberstar.Game.Events;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

internal class DoorScreen : LockedScreen<DoorEvent>
{
    protected override Layout Layout { get; } = Layout.Door;

    public override ScreenType Type { get; } = ScreenType.Door;

    protected override uint? AllowedUnlockItemIndex => LockedEvent.ItemIndex;

    protected override void Unlocked(Message message)
    {
        Game.SaveEvent(LockedEvent.Index);
        ShowMessage(message, true, true);
    }

    public override void Open(Game game, Action? closeAction)
    {
        Image = Image80x80.LockedDoor;

        base.Open(game, closeAction);

        // If this is a DoorExit event and the door was already opened, just close the screen.
        // The close handler will trigger the follow up event automatically.
        if (LockedEvent.OpenedEventIndex != null && LockedEvent.OpenedEventIndex != 0 && game.IsCurrentEventSaved())
        {
            LockOpened = true;
            game!.ScreenHandler.PopScreen();
            return;
        }
    }

    public override void Close(Game game)
    {
        base.Close(game);

        if (LockOpened)
        {
            var extraEvent = LockedEvent.OpenedEventIndex;

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
}