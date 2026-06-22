using Amber.Common;

namespace AmberIsland.Game;

public enum Direction
{
    // Same order as in the sprite sheets
    Down, Up, Right, Left 
}

public sealed class GameState
{
    public Position PlayerPosition { get; set; }
    public Direction PlayerDirection { get; set; }
}

partial class Game
{
    public GameState State { get; } = new();
}
