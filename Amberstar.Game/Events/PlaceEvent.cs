using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Events
{
	internal class PlaceEvent(IPlaceEvent @event) : Event(@event), IPlaceEvent
	{
		public byte OpeningHour => @event.OpeningHour;

		public byte ClosingHour => @event.ClosingHour;

		public PlaceType PlaceType => @event.PlaceType;

		public byte ClosedTextIndex => @event.ClosedTextIndex;

		public ushort PlaceIndex => @event.PlaceIndex;

		public ushort WaresIndex => @event.WaresIndex;

		public bool AlwaysOpen => @event.AlwaysOpen;

		public override bool Handle(EventTrigger trigger, Game game, IEventProvider eventProvider)
		{
			game.OpenPlace(this);

			return true;
		}
	}
}
