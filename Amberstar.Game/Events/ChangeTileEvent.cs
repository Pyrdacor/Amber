using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Events
{
	internal class ChangeTileEvent(IChangeTileEvent @event, int eventIndex) : Event(@event, eventIndex), IChangeTileEvent, ITextEvent
	{
		public byte X => @event.X;

		public byte Y => @event.Y;

		public byte TextIndex => @event.TextIndex;

		public word IconIndex => @event.IconIndex;

		public override bool Handle(EventTrigger trigger, Game game, IMapEventProvider eventProvider)
		{
			if (trigger != EventTrigger.Move)
				return false;

			// Like in the original the change is only kept after leaving the map if the event is saved.
			void ChangeTile() => game.ChangeMapTile(X, Y, IconIndex, SaveEvent);

			if (this.HasText())
				game.ShowText(ChangeTile);
			else
				ChangeTile();

			return true;
		}
	}
}
