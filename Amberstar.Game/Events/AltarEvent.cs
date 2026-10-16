using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Events
{
	// TODO: The altar is not part of the released original source code.
	// It should check if the party has all Amberstar pieces and assemble the Amberstar.
	internal class AltarEvent(IAltarEvent @event, int eventIndex) : Event(@event, eventIndex), IAltarEvent
	{
		public override bool AutoSave => false;

		public override bool Handle(EventTrigger trigger, Game game, IMapEventProvider eventProvider)
		{
			if (trigger != EventTrigger.Move)
				return false;

			game.ShowNotImplementedMessage("The altar (assembling the Amberstar)");

			return true;
		}
	}
}
