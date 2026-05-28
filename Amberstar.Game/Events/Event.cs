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

	internal abstract class Event(IEvent @event, int eventIndex) : IEvent
	{
		public int Index { get; } = eventIndex;

		public EventType Type => @event.Type;

		public bool SaveEvent => @event.SaveEvent;

		public virtual bool AutoSave => true;

        public abstract bool Handle(EventTrigger trigger, Game game, IMapEventProvider eventProvider);

		public static Event CreateEvent(IEvent @event, int eventIndex)
		{
			return @event switch
			{
				IMapExitEvent mapExitEvent => new MapExitEvent(mapExitEvent, eventIndex),
                IDoorExitEvent doorExitEvent => new DoorEvent(doorExitEvent, eventIndex),
                IDoorEvent doorEvent => new DoorEvent(doorEvent, eventIndex),
                IChestEvent chestEvent => new ChestEvent(chestEvent, eventIndex),
                IShowPictureTextEvent showPictureTextEvent => new ShowPictureTextEvent(showPictureTextEvent, eventIndex),
				ITeleporterEvent teleportEvent => new TeleporterEvent(teleportEvent, eventIndex),
				ITravelExitEvent travelExitEvent => new TravelExitEvent(travelExitEvent, eventIndex),
				IWindGateEvent windGateEvent => new WindGateEvent(windGateEvent, eventIndex),
				IPlaceEvent placeEvent => new PlaceEvent(placeEvent, eventIndex),
                IExecuteTrapEvent executeTrapEvent => new ExecuteTrapEvent(executeTrapEvent, eventIndex),
                IHPRegenerationEvent hpRegenerationEvent => new HPRegenerationEvent(hpRegenerationEvent, eventIndex),
                ISPRegenerationEvent spRegenerationEvent => new SPRegenerationEvent(spRegenerationEvent, eventIndex),
                IDamageFieldEvent damageFieldEvent => new DamageFieldEvent(damageFieldEvent, eventIndex),
                _ => throw new NotImplementedException()
			};
		}
	}
}
