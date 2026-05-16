using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Events
{
    internal class DoorEvent(IDoorEvent @event, int eventIndex) : Event(@event, eventIndex), IDoorEvent
	{
        public byte LockpickReduction => @event.LockpickReduction;

        public TrapType TrapType => @event.TrapType;

        public byte TrapDamage => @event.TrapDamage;

        public ushort ItemIndex => @event.ItemIndex;

        // We need to save it only if the door was opened!
        public override bool AutoSave => false;

        public override bool Handle(EventTrigger trigger, Game game, IEventProvider eventProvider)
		{
            if (trigger == EventTrigger.Move)
                game.ShowDoor();
                
            return true;
		}
	}
}
