using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Events
{
    internal class DoorEvent(IDoorEvent @event, int eventIndex) : Event(@event, eventIndex), IDoorEvent
	{
        public byte LockpickReduction => @event.LockpickReduction;

        public TrapType TrapType => @event.TrapType;

        public byte TrapDamage => @event.TrapDamage;

        public word ItemIndex => @event.ItemIndex;

        public byte? OpenedEventIndex => @event is IDoorExitEvent doorExitEvent ? doorExitEvent.OpenedEventIndex : null;

        // We need to save it only if the door was opened!
        public override bool AutoSave => false;

        public override bool Handle(EventTrigger trigger, Game game, IMapEventProvider eventProvider)
		{
            if (trigger != EventTrigger.Move)
                return true;

            // With the Amberstar every door is open. Only the follow-up event of the door is executed.
            if (game.State.SpecialItems.HasFlag(SpecialItems.Amberstar))
            {
                if (OpenedEventIndex is byte extraEvent && extraEvent != 0)
                    game.EventHandler.HandleEvent(EventTrigger.Move, CreateEvent(eventProvider.Events[extraEvent - 1], extraEvent), eventProvider);

                return true;
            }

            game.ShowDoor();
                
            return true;
		}
	}
}
