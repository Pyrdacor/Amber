using Amber.Common;
using Amberstar.GameData;

namespace Amberstar.Game
{
	// Will basically have the savegame data and some additional temporary stuff
	internal class GameState
	{
		public int MapIndex { get; set; } = 65; // 65 is the Twinlake graveyard
		public Position PartyPosition { get; set; } = new(7, 10);
		public Direction PartyDirection { get; set; } = Direction.Right;
		public TravelType TravelType { get; set; }
	}
}
