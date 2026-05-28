using Amberstar.GameData;

namespace Amberstar.Game.Events
{
    internal class EventHandler(Game game)
    {
        internal IMapEventProvider? CurrentEventProvider { get; set; }
        internal IEvent? CurrentEvent { get; set; }
        internal IMap? CurrentEventMap => CurrentEventProvider?.Map;
        internal int CurrentEventMapIndex => CurrentEventMap?.Index ?? game.State.GetIndexOfMapWithPlayer();

        public bool HandleEvent(EventTrigger trigger, Event @event, IMapEventProvider eventProvider)
        {
            CurrentEventProvider = eventProvider;
            CurrentEvent = @event;

			bool result = @event.Handle(trigger, game, eventProvider);

			if (result && @event.SaveEvent && @event.AutoSave)
			{
				game.SaveEvent(@event.Index);
            }

			return result;
		}
    }
}
