using Amber.Common;
using AmberIsland.GameData;

namespace AmberIsland.Game;

public sealed class GameState(Savegame savegame)
{
    // Remove later
    public GameState() : this(default)
    {

    }

    public Position PlayerPosition { get; set; } = new(savegame.X, savegame.Y);
    public Direction PlayerDirection { get; set; } = savegame.Direction;
    public PlayerData PlayerData { get; set; } = savegame.Player;
}

partial class Game
{
    public GameState State { get; } = new();
}
