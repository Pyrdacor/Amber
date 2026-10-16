using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Events
{
    internal class ExecuteTrapEvent(IExecuteTrapEvent @event, int eventIndex) : Event(@event, eventIndex), IExecuteTrapEvent, ITextEvent
	{
        public TrapType TrapType => @event.TrapType;

        public byte Damage => @event.Damage;

        public byte TextIndex => @event.TextIndex;

        public override bool Handle(EventTrigger trigger, Game game, IMapEventProvider eventProvider)
		{
            if (trigger != EventTrigger.Move)
                return false;

            void ExecuteTrap() => game.TriggerTrap(TrapType, Damage);

            if (this.HasText())
                game.ShowText(ExecuteTrap);
            else
                ExecuteTrap();

            return true;
		}
	}
}
