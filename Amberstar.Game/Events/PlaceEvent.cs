using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Events
{
	internal class PlaceEvent(IPlaceEvent @event, int eventIndex) : Event(@event, eventIndex), IPlaceEvent
	{
		public byte OpeningHour => @event.OpeningHour;

		public byte ClosingHour => @event.ClosingHour;

		public PlaceType PlaceType => @event.PlaceType;

		public byte ClosedTextIndex => @event.ClosedTextIndex;

		public word PlaceIndex => @event.PlaceIndex;

		public word WaresIndex => @event.WaresIndex;

		public bool AlwaysOpen => @event.AlwaysOpen;

		public override bool Handle(EventTrigger trigger, Game game, IMapEventProvider eventProvider)
		{
            if (trigger != EventTrigger.Move)
                return false;

            game.OpenPlace(this);

			return true;
		}
	}
}
