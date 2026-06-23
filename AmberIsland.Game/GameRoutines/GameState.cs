using Amber.Common;
using AmberIsland.GameData;

namespace AmberIsland.Game;

public sealed class GameState
{
    public Position PlayerPosition { get; set; }
    public Direction PlayerDirection { get; set; }
}

partial class Game
{
    public GameState State { get; } = new();
}
