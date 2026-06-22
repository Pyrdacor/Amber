using Amber.Renderer.Common;

namespace AmberIsland.Game;

public partial class Game
{
    const long TicksPerSecond = 60;
    const long DefaultFadeTime = 1000;
    double totalTime = 0.0;
    long lastGameTicks = 0;
    long gameTicks = 0;
    readonly GameData.GameData gameData;
    readonly Player player;

    internal IRenderer Renderer { get; }
    private ScreenHandler ScreenHandler { get; }

    public Game(GameData.GameData gameData, IRenderer renderer)
    {
        this.gameData = gameData;
        Renderer = renderer;
        ScreenHandler = new(this);
        //Cursor = new(this);

        SetupLayers();

        player = new(this);
    }

    public void Render(double delta)
    {

    }

    public void Update(double delta)
    {
        totalTime += delta;
        gameTicks = (long)Math.Round(totalTime * TicksPerSecond);

        // Execute timed actions which are ready.
        var readyTimedAction = timedActions.Pop(gameTicks);

        if (readyTimedAction != null)
        {
            readyTimedAction.Action();
            return;
        }

        long elapsed = Paused ? 0 : gameTicks - lastGameTicks;
        lastGameTicks = gameTicks;

        //Time.Update(elapsed);
        ScreenHandler.ActiveScreen?.Update(this, elapsed);
        pressedKeys = null; // reset
    }
}
