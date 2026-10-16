using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Events
{
	internal class SpinnerEvent(ISpinnerEvent @event, int eventIndex) : Event(@event, eventIndex), ISpinnerEvent, ITextEvent
	{
		public Direction Direction => @event.Direction;

		public byte TextIndex => @event.TextIndex;

		public override bool Handle(EventTrigger trigger, Game game, IMapEventProvider eventProvider)
		{
			if (trigger != EventTrigger.Move)
				return false;

			void Spin()
			{
				var direction = Direction;

				// A random spin never ends in the same direction.
				if (direction >= Direction.Keep)
				{
					do
					{
						direction = (Direction)Game.Random(0, 3);
					} while (direction == game.State.PartyDirection);
				}

				game.TurnParty(direction);
			}

			if (this.HasText())
				game.ShowText(Spin);
			else
				Spin();

			return true;
		}
	}
}
