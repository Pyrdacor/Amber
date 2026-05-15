using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Events
{
	internal enum EventTrigger
	{
		// Note: Keep Eye as 0 and Move as 1 as it must match the event data!
		Eye,
		Move,
		Ear,
		Mouth,
		UseItem
	}

	public static class EventTriggerExtensions
	{
		internal static EventTrigger ToEventTrigger(this CursorType cursorType)
		{
			return cursorType switch
			{
				CursorType.Eye => EventTrigger.Eye,
				CursorType.Ear => EventTrigger.Ear,
				CursorType.Mouth => EventTrigger.Mouth,
				_ => throw new InvalidOperationException($"Cursor type '{cursorType}' does not have a corresponding event trigger.")
			};
		}
    }

	internal abstract class Event(IEvent @event) : IEvent
	{
		public EventType Type => @event.Type;

		public bool SaveEvent => @event.SaveEvent;

		public abstract bool Handle(EventTrigger trigger, Game game, IEventProvider eventProvider);

		public static Event CreateEvent(IEvent @event)
		{
			return @event switch
			{
				IMapExitEvent mapExitEvent => new MapExitEvent(mapExitEvent),
				IShowPictureTextEvent showPictureTextEvent => new ShowPictureTextEvent(showPictureTextEvent),
				ITeleporterEvent teleportEvent => new TeleporterEvent(teleportEvent),
				ITravelExitEvent travelExitEvent => new TravelExitEvent(travelExitEvent),
				IWindGateEvent windGateEvent => new WindGateEvent(windGateEvent),
				IPlaceEvent placeEvent => new PlaceEvent(placeEvent),
				IHPRegenerationEvent hpRegenerationEvent => new HPRegenerationEvent(hpRegenerationEvent),
                ISPRegenerationEvent spRegenerationEvent => new SPRegenerationEvent(spRegenerationEvent),
                _ => throw new NotImplementedException()
			};
		}
	}
}
