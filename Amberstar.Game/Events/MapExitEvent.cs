using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Events
{
	internal class MapExitEvent(IMapExitEvent @event, int eventIndex) : Event(@event, eventIndex), IMapExitEvent, ITeleportEvent
	{
		public byte X => @event.X;

		public byte Y => @event.Y;

		public Direction Direction => @event.Direction;

		public word MapIndex => @event.MapIndex;

		public override bool Handle(EventTrigger trigger, Game game, IMapEventProvider eventProvider)
		{
            if (trigger != EventTrigger.Move)
                return false;

            game.Teleport(X, Y, Direction, MapIndex, true);
			return true;
		}
	}
}
