namespace Amber.Common;

public readonly struct Position : IEquatable<Position>
{
	public static readonly Position Zero = new(0, 0);

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

	public static Position operator +(Position left, Position right) => new(left.X + right.X, left.Y + right.Y);

    public static Position operator -(Position left, Position right) => new(left.X - right.X, left.Y - right.Y);

    public static Position operator *(int factor, Position position) => new(factor * position.X, factor * position.Y);

    public static Position operator *(Position position, int factor) => new(factor * position.X, factor * position.Y);

    public override readonly string ToString() => $"({X}, {Y})";

    public void Deconstruct(out int x, out int y)
    {
		x = X;
		y = Y;
    }
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

    public static FloatPosition operator +(FloatPosition left, FloatPosition right) => new(left.X + right.X, left.Y + right.Y);

    public static FloatPosition operator -(FloatPosition left, FloatPosition right) => new(left.X - right.X, left.Y - right.Y);

    public override readonly string ToString() => $"({X:0.00}, {Y:0.00})";

    public void Deconstruct(out float x, out float y)
    {
        x = X;
        y = Y;
    }
}

public delegate FloatPosition PositionTransformation(FloatPosition position);
