using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Events
{
    internal class ExecuteTrapEvent(IExecuteTrapEvent @event, int eventIndex) : Event(@event, eventIndex), IExecuteTrapEvent
	{
        public TrapType TrapType => @event.TrapType;

        public byte Damage => @event.Damage;

        public byte TextIndex => @event.Damage;

        public override bool Handle(EventTrigger trigger, Game game, IMapEventProvider eventProvider)
		{
            void ExecuteTrap() => game.TriggerTrap(TrapType, Damage);

            if (TextIndex != 0)
                game.ShowText(ExecuteTrap);
            else
                ExecuteTrap();

            return false;
		}
	}
}
