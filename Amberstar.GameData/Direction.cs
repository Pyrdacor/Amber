using Amber.Common;

namespace Amberstar.GameData;

public enum Direction : byte
{
	Up,
	Right,
	Down,
	Left,
	Keep,
	Random = Keep,
	North = Up,
	East = Right,
	South = Down,
	West = Left
}

public static class DirectionExtensions
{
	public static Position Offset(this Direction direction) => direction switch
	{
		Direction.North => new(0, -1),
		Direction.East => new(1, 0),
		Direction.South => new(0, 1),
		Direction.West => new(-1, 0),
		_ => new(0, 0),
	};
}
