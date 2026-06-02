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

    public override void Open(Action? closeAction)
    {
        // If this is a DoorExit event and the door was already opened, just close the screen.
        // The close handler will trigger the follow up event automatically.
        if (IsOpenDoorWithExit(Game))
        {
            // Important!
            SetCloseAction(closeAction);

            LockOpened = true;
            Game.ScreenHandler.PopScreen();
            return;
        }

        Image = Image80x80.LockedDoor;

        base.Open(closeAction);
    }

    public override void Close()
    {
        base.Close();

        if (LockOpened && Game.EventHandler.CurrentEvent is DoorEvent doorEvent)
        {
            var extraEvent = doorEvent.OpenedEventIndex;

            if (extraEvent != null && extraEvent != 0)
            {
                IMapEventProvider eventProvider;

                void ExecuteFollowUpEvent(Screen _, Screen __)
                {
                    Game.ScreenHandler.ScreenChanged -= ExecuteFollowUpEvent;

                    var @event = eventProvider.Events[extraEvent.Value - 1];

                    Game.EventHandler.HandleEvent(EventTrigger.Move, Event.CreateEvent(@event, extraEvent.Value), eventProvider);
                }

                if (Game.ScreenHandler.ActiveScreen is Map2DScreen map2dScreen)
                {
                    eventProvider = map2dScreen.Map;
                    Game.ScreenHandler.ScreenChanged += ExecuteFollowUpEvent;
                }
                else if (Game.ScreenHandler.ActiveScreen is Map3DScreen map3dScreen)
                {
                    eventProvider = map3dScreen.Map;
                    Game.ScreenHandler.ScreenChanged += ExecuteFollowUpEvent;
                }
            }
        }
    }
}