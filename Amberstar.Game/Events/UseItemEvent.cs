using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Events
{
	/// <summary>
	/// Stepping onto the tile only shows the text. If the right item is used
	/// on this tile (<see cref="Game.CurrentItem"/>), the tile is changed instead.
	/// </summary>
	internal class UseItemEvent(IUseItemEvent @event, int eventIndex) : Event(@event, eventIndex), IUseItemEvent, ITextEvent
	{
		public byte X => @event.X;

		public byte Y => @event.Y;

		public byte TextIndex => @event.TextIndex;

		public word ItemIndex => @event.ItemIndex;

		public word IconIndex => @event.IconIndex;

		// The event is only saved if the item was used.
		public override bool AutoSave => false;

		public override bool Handle(EventTrigger trigger, Game game, IMapEventProvider eventProvider)
		{
			if (trigger == EventTrigger.Move)
			{
				if (!this.HasText())
					return false;

				game.ShowText();
				return true;
			}

			if (trigger != EventTrigger.UseItem || game.CurrentItem?.Index != ItemIndex)
				return false;

			game.ChangeMapTile(X, Y, IconIndex, SaveEvent);
			game.SaveEvent(this);

			return true;
		}
	}
}
