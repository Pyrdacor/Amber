using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Events
{
	internal class TeleporterEvent(ITeleporterEvent @event, int eventIndex) : Event(@event, eventIndex), ITeleporterEvent, ITeleportEvent, ITextEvent
	{
		public byte X => @event.X;

		public byte Y => @event.Y;

		public Direction Direction => @event.Direction;

		public word MapIndex => @event.MapIndex;

		public byte TextIndex => @event.TextIndex;

		public override bool Handle(EventTrigger trigger, Game game, IEventProvider eventProvider)
		{
			if (trigger != EventTrigger.Move)
				return false;

			void Teleport() => game.Teleport(X, Y, Direction, MapIndex, false);

            if (TextIndex != 0)
				game.ShowText(Teleport);
			else
				Teleport();

			return true;
		}
	}
}
