using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Events
{
	// TODO: The outro is not part of the released original source code.
	internal class OutroEvent(IOutroEvent @event, int eventIndex) : Event(@event, eventIndex), IOutroEvent
	{
		public override bool AutoSave => false;

		public override bool Handle(EventTrigger trigger, Game game, IMapEventProvider eventProvider)
		{
			if (trigger != EventTrigger.Move)
				return false;

			game.ShowNotImplementedMessage("The outro");

			return true;
		}
	}
}
