using Amberstar.Game;

namespace Amberstar.Frontend.Scripting;

/// <summary>
/// Functionality of the frontend (window or headless) needed by scenario scripts.
/// </summary>
public interface IScenarioScriptHost
{
	/// <summary>
	/// Holds or releases a key. Held keys are reported as pressed to the game (e.g. for movement).
	/// </summary>
	void SetKeyHeld(Key key, bool held);
	/// <summary>
	/// Closes the game.
	/// </summary>
	void Exit();
}
