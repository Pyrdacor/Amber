namespace Amberstar.Game.Screens;

/// <summary>
/// Map screen functionality used by scenario scripts.
/// </summary>
internal interface IScriptMapScreen : IMapScreen
{
	/// <summary>
	/// Moves the party by the given offset like the player does but without
	/// waiting for the movement delay. Map events at the target are triggered.
	/// Returns false if the movement was blocked.
	/// </summary>
	bool ScriptMove(int x, int y);
}
