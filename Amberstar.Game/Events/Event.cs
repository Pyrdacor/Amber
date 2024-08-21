using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Events
{
	internal enum EventTrigger
	{
		Move,
		Eye,
		Ear,
		Mouth,
		UseItem
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
				_ => throw new NotImplementedException()
			};
		}
	}
}
