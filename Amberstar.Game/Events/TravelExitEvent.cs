using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Events
{
	internal class TravelExitEvent(ITravelExitEvent @event) : Event(@event), ITravelExitEvent
	{
		public byte X => @event.X;

		public byte Y => @event.Y;

		public Direction Direction => @event.Direction;

		public ushort MapIndex => @event.MapIndex;

		public override bool Handle(EventTrigger trigger, Game game, IEventProvider eventProvider)
		{
			game.State.TravelType = TravelType.Walk;
			game.Teleport(X, Y, Direction, MapIndex, true);

			return true;
		}
	}
}
