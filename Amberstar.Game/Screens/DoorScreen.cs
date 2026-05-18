using Amberstar.Game.Events;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

internal class DoorScreen : LockedScreen<DoorEvent>
{
    protected override Layout Layout { get; } = Layout.Door;

    public override ScreenType Type { get; } = ScreenType.Door;

    protected override uint? AllowedUnlockItemIndex => LockedEvent.ItemIndex;

    private static bool IsOpenDoorWithExit(Game game)
    {
        return game.EventHandler.CurrentEvent is DoorEvent doorEvent &&
            doorEvent.OpenedEventIndex != null && doorEvent.OpenedEventIndex != 0 &&
            game.IsCurrentEventSaved();
    }

    protected override void Unlocked(Message message)
    {
        Game.SaveEvent(LockedEvent.Index);
        ShowMessage(message, true, true);
    }

    public override void Open(Game game, Action? closeAction)
    {
        // If this is a DoorExit event and the door was already opened, just close the screen.
        // The close handler will trigger the follow up event automatically.
        if (IsOpenDoorWithExit(game))
        {
            // Important!
            SetCloseAction(closeAction);

            LockOpened = true;
            game!.ScreenHandler.PopScreen();
            return;
        }

        Image = Image80x80.LockedDoor;

        base.Open(game, closeAction);
    }

    public override void Close(Game game)
    {
        base.Close(game);

        if (LockOpened && game.EventHandler.CurrentEvent is DoorEvent doorEvent)
        {
            var extraEvent = doorEvent.OpenedEventIndex;

            if (extraEvent != null && extraEvent != 0)
            {
                IEventProvider eventProvider;

                void ExecuteFollowUpEvent(Screen _)
                {
                    game.ScreenHandler.ScreenChanged -= ExecuteFollowUpEvent;

                    var @event = eventProvider.Events[extraEvent.Value - 1];

                    game.EventHandler.HandleEvent(EventTrigger.Move, Event.CreateEvent(@event, extraEvent.Value), eventProvider);
                }

                if (game!.ScreenHandler.ActiveScreen is Map2DScreen map2dScreen)
                {
                    eventProvider = map2dScreen.Map;
                    game.ScreenHandler.ScreenChanged += ExecuteFollowUpEvent;
                }
                else if (game!.ScreenHandler.ActiveScreen is Map3DScreen map3dScreen)
                {
                    eventProvider = map3dScreen.Map;
                    game.ScreenHandler.ScreenChanged += ExecuteFollowUpEvent;
                }
            }
        }
    }
}