using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Events
{
	internal class WindGateEvent(IWindGateEvent @event) : Event(@event), IWindGateEvent, ITeleportEvent
	{
		public byte X => @event.X;

		public byte Y => @event.Y;

		public Direction Direction => @event.Direction;

		public ushort MapIndex => @event.MapIndex;

		public byte TextIndex => @event.TextIndex;

		public override bool Handle(EventTrigger trigger, Game game, IEventProvider eventProvider)
		{
			if (!game.State.SpecialItems.HasFlag(SpecialItems.WindChain))
				return false;

			if (TextIndex != 0)
				game.ShowText(() => game.Teleport(X, Y, Direction, MapIndex, false));
			else
				game.Teleport(X, Y, Direction, MapIndex, false);

			return true;
		}
	}
}
