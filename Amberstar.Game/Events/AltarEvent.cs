using Amberstar.GameData;
using Amberstar.Game.Screens;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Events
{
	// The altar is not part of the released original source code.
	// It is used to assemble the Amberstar from its 13 pieces.
	internal class AltarEvent(IAltarEvent @event, int eventIndex) : Event(@event, eventIndex), IAltarEvent
	{
		const int RequiredPieces = 13;

		public override bool AutoSave => false;

		public override bool Handle(EventTrigger trigger, Game game, IMapEventProvider eventProvider)
		{
			if (trigger != EventTrigger.Move)
				return false;

			// Note: The party position was already reset by the map screen (pushed away from the altar).
			if (game.State.SpecialItems.HasFlag(SpecialItems.Amberstar))
				return false;

			if (game.CountPartyItems(Game.PieceOfAmberstarItemIndex) < RequiredPieces)
				game.ShowTextMessage(Message.AltarRequires13);
			else if (game.AssetProvider.AltarPuzzle == null)
				game.ShowNotImplementedMessage("Assembling the Amberstar without the puzzle data");
			else
				game.ScreenHandler.PushScreen(ScreenType.Altar);

			return true;
		}
	}
}
