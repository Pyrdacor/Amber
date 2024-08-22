using Amberstar.GameData;

namespace Amberstar.Game.Events
{
    internal class EventHandler(Game game)
    {
		internal IEvent? CurrentEvent { get; private set; }

		public bool HandleEvent(EventTrigger trigger, Event @event, IEventProvider eventProvider)
        {
			CurrentEvent = @event;

			bool result = @event.Handle(trigger, game, eventProvider);

			CurrentEvent = null;

			return result;
		}
    }
}
