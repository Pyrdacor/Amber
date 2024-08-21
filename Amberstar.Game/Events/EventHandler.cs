using Amberstar.GameData;

namespace Amberstar.Game.Events
{
    internal class EventHandler(Game game)
    {
        public bool HandleEvent(EventTrigger trigger, Event @event, IEventProvider eventProvider)
        {
            return @event.Handle(trigger, game, eventProvider);
		}
    }
}
