using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Events
{
    internal class ChestEvent(IChestEvent @event, int eventIndex) : Event(@event, eventIndex), IChestEvent
    {
        public byte LockpickReduction => @event.LockpickReduction;

        public TrapType TrapType => @event.TrapType;

        public byte TrapDamage => @event.TrapDamage;

        public word TextIndex => @event.TextIndex;

        public bool Hidden => @event.Hidden;

        public word ChestIndex => @event.ChestIndex;

        // We need to save it only if the chest was opened!
        public override bool AutoSave => false;        

        public override bool Handle(EventTrigger trigger, Game game, IEventProvider eventProvider)
		{
            if (trigger == EventTrigger.Move || trigger == EventTrigger.Eye)
                game.ShowChest();
                
            return true;
		}
	}
}
