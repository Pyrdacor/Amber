namespace Amber.Common;

public readonly struct Position : IEquatable<Position>
{
	public readonly int X;
	public readonly int Y;

	public Position()
	{

	}

	public Position(Position other)
	{
		X = other.X;
		Y = other.Y;
	}

	public Position(FloatPosition other)
	{
		X = MathUtil.Round(other.X);
		Y = MathUtil.Round(other.Y);
	}

	public Position(int x, int y)
	{
		X = x;
		Y = y;
	}

	public bool Equals(Position other)
	{
		return X == other.X && Y == other.Y;
	}

	public override bool Equals(object? obj)
	{
		return obj is Position position && Equals(position);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(X, Y);
	}

	public static bool operator ==(Position left, Position right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(Position left, Position right)
	{
		return !(left == right);
	}

	public override readonly string ToString() => $"({X}, {Y})";
}

public readonly struct FloatPosition : IEquatable<FloatPosition>
{
	public readonly float X;
	public readonly float Y;

	public FloatPosition()
	{

	}

	public FloatPosition(FloatPosition other)
	{
		X = other.X;
		Y = other.Y;
	}

	public FloatPosition(Position other)
	{
		X = other.X;
		Y = other.Y;
	}

	public FloatPosition(float x, float y)
	{
		X = x;
		Y = y;
	}

	public bool Equals(FloatPosition other)
	{
		return X == other.X && Y == other.Y;
	}

	public override bool Equals(object? obj)
	{
		return obj is FloatPosition position && Equals(position);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(X, Y);
	}

	public static bool operator ==(FloatPosition left, FloatPosition right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(FloatPosition left, FloatPosition right)
	{
		return !(left == right);
	}

	public override readonly string ToString() => $"({X:0.00}, {Y:0.00})";
}

public delegate FloatPosition PositionTransformation(FloatPosition position);
