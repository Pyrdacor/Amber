using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Events
{
	internal class MapExitEvent(IMapExitEvent @event) : Event(@event), IMapExitEvent
	{
		public byte X => @event.X;

		public byte Y => @event.Y;

		public Direction Direction => @event.Direction;

		public ushort MapIndex => @event.MapIndex;

		public override bool Handle(EventTrigger trigger, Game game, IEventProvider eventProvider)
		{
			game.Teleport(X, Y, Direction, MapIndex, true);
			return true;
		}
	}
}
