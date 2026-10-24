using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Events
{
	/// <summary>
	/// Shows the outro (not part of the released original source code, see <see cref="Game.StartOutro"/>).
	/// </summary>
	internal class OutroEvent(IOutroEvent @event, int eventIndex) : Event(@event, eventIndex), IOutroEvent
	{
		public override bool AutoSave => false;

		public override bool Handle(EventTrigger trigger, Game game, IMapEventProvider eventProvider)
		{
			if (trigger != EventTrigger.Move)
				return false;

			game.StartOutro();

			return true;
		}
	}
}
