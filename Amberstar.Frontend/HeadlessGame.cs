using Amber.Common;
using Amberstar.Frontend.Scripting;
using Amberstar.Game;
using G = Amberstar.Game.Game;
using HeadlessRenderer = Amber.Renderer.Headless.Renderer;

namespace Amberstar.Frontend;

/// <summary>
/// Result of a scenario script run.
/// </summary>
public sealed record ScenarioResult(int Checks, int Failures, bool TimedOut, double GameSeconds)
{
	public bool Success => Failures == 0 && !TimedOut;
}

/// <summary>
/// Runs the game without a window, audio or user input.
///
/// The game is updated in fixed steps of 1/60 second as fast as possible,
/// so scenario scripts are reproducible and run faster than real time.
/// </summary>
public sealed class HeadlessGame : IScenarioScriptHost, IDisposable
{
	public const double FrameTime = 1.0 / 60.0;
	readonly HashSet<Key> heldKeys = [];
	readonly HeadlessRenderer renderer = new(new Size(320, 200));
	bool disposed = false;

	/// <param name="dataPath">Path to the game data (see <see cref="GameSetup.FindDataPath"/>).</param>
	/// <param name="randomSeed">Seed for random numbers to make runs reproducible.</param>
	public HeadlessGame(string dataPath, int randomSeed = 0)
	{
		G.SetRandomSeed(randomSeed);
		Game = GameSetup.CreateGame(renderer, dataPath, new NullAudioOutput(), new Configuration(),
			() => [.. heldKeys], _ => { });
		Game.QuitRequested += () => ExitRequested = true;
		// every headless game has its own savegame
		Game.SavegamePath = Path.Combine(Path.GetTempPath(), $"Amberstar.net-{Guid.NewGuid():N}.sav");
	}

	public G Game { get; }

	/// <summary>
	/// Total game time in seconds.
	/// </summary>
	public double Time { get; private set; } = 0.0;

	public bool ExitRequested { get; private set; } = false;

	public void Step(int frames = 1)
	{
		for (int i = 0; i < frames && !ExitRequested; i++)
		{
			Game.Update(FrameTime);
			Game.Render(FrameTime);
			renderer.Render();
			Time += FrameTime;
		}
	}

	/// <summary>
	/// Steps until the condition is met. Returns false if it was not met within the given number of frames.
	/// </summary>
	public bool StepUntil(Func<bool> condition, int maxFrames = 600)
	{
		for (int i = 0; i < maxFrames; i++)
		{
			if (condition())
				return true;

			Step();
		}

		return condition();
	}

	/// <summary>
	/// Runs a scenario script until it finishes or the given game time elapsed.
	/// </summary>
	public ScenarioResult RunScript(ScenarioScript script, double maxGameSeconds = 600.0, Action<string>? output = null)
	{
		double endTime = Time + maxGameSeconds;
		double startTime = Time;
		bool timedOut = false;

		while (true)
		{
			script.Update(Game, this, FrameTime);

			if (script.Finished || ExitRequested)
				break;

			if (Time >= endTime)
			{
				script.Abort($"the script did not finish within {maxGameSeconds} seconds of game time", Game);
				timedOut = true;
				break;
			}

			try
			{
				Step();
			}
			catch (Exception ex)
			{
				// report the crash like a failed check, so all scripts can run
				script.Abort($"the game crashed: {ex}", Game);
				break;
			}
		}

		(output ?? Console.WriteLine)(script.Summary);

		return new(script.Checks, script.Failures, timedOut, Time - startTime);
	}

	void IScenarioScriptHost.SetKeyHeld(Key key, bool held)
	{
		if (held)
			heldKeys.Add(key);
		else
			heldKeys.Remove(key);
	}

	void IScenarioScriptHost.Exit() => ExitRequested = true;

	public void Dispose()
	{
		if (!disposed)
		{
			if (Game.SavegamePath != null && File.Exists(Game.SavegamePath))
				File.Delete(Game.SavegamePath);

			Game.Dispose();
			disposed = true;
		}
	}
}
