using Amber.Common;
using Amberstar.Game.Events;
using Amberstar.GameData;

namespace Amberstar.Game.Screens;

/// <summary>
/// Functionality of the 2D and 3D map screens used by map events.
/// </summary>
internal interface IMapScreen
{
	/// <summary>
	/// Changes a tile of the shown map at a 1-based position and redraws the map.
	/// Nothing happens if the given map is not shown.
	/// </summary>
	void ChangeTile(int mapIndex, int x, int y, int tileIndex);
	/// <summary>
	/// Changes the party direction and redraws the map.
	/// </summary>
	void Turn(Direction direction);
	/// <summary>
	/// Triggers the map event at the given 0-based map position (e.g. look at it).
	/// With the mouth trigger a person at that position is talked to instead.
	/// Returns false if there is nothing to trigger.
	/// </summary>
	bool TriggerEvent(EventTrigger trigger, int x, int y);
	/// <summary>
	/// Gets the active map event at the given 0-based map position if any.
	/// </summary>
	IEvent? GetActiveEvent(int x, int y);
	/// <summary>
	/// Gets the 0-based positions of the persons and monsters which are on the map now.
	/// </summary>
	IEnumerable<Position> GetCharacterPositions();
}
