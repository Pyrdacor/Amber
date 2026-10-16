using Amberstar.Game.Events;
using Amberstar.GameData;

namespace Amberstar.Game.Screens;

/// <summary>
/// Map screen functionality used by scenario scripts.
/// </summary>
internal interface IScriptMapScreen
{
	/// <summary>
	/// Moves the party by the given offset like the player does but without
	/// waiting for the movement delay. Map events at the target are triggered.
	/// Returns false if the movement was blocked.
	/// </summary>
	bool ScriptMove(int x, int y);
	/// <summary>
	/// Changes the party direction.
	/// </summary>
	void ScriptTurn(Direction direction);
	/// <summary>
	/// Triggers the map event at the given 0-based map position (e.g. look at it).
	/// With the mouth trigger a person at that position is talked to instead.
	/// Returns false if there is nothing to trigger.
	/// </summary>
	bool ScriptTrigger(EventTrigger trigger, int x, int y);
}
