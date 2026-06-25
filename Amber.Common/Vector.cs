namespace Amber.Common;

public readonly struct Vector : IEquatable<Vector>
{
    public static readonly Vector Zero = new(0, 0);

    public readonly float X;
	public readonly float Y;

	public Vector()
	{

	}

	public Vector(Vector other)
	{
		X = other.X;
		Y = other.Y;
	}

	public Vector(Position other)
	{
		X = other.X;
		Y = other.Y;
	}

	public Vector(float x, float y)
	{
		X = x;
		Y = y;
	}

	public bool Equals(Vector other)
	{
		return X == other.X && Y == other.Y;
	}

	public override bool Equals(object? obj)
	{
		return obj is Vector position && Equals(position);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(X, Y);
	}

	public static bool operator ==(Vector left, Vector right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(Vector left, Vector right)
	{
		return !(left == right);
	}

    public static Vector operator +(Vector left, Vector right) => new(left.X + right.X, left.Y + right.Y);

    public static Vector operator -(Vector left, Vector right) => new(left.X - right.X, left.Y - right.Y);

    public static Vector operator *(float factor, Vector vector) => new(factor * vector.X, factor * vector.Y);

    public static Vector operator *(Vector vector, float factor) => new(factor * vector.X, factor * vector.Y);

    public override readonly string ToString() => $"({X:0.00}, {Y:0.00})";

    public void Deconstruct(out float x, out float y)
    {
        x = X;
        y = Y;
    }

	public float Length()
	{
		return (float)Math.Sqrt(X * X + Y * Y);
	}

	public float InverseLength()
	{
		var length = Length();

        return length == 0 ? float.PositiveInfinity : 1.0f / length;
    }

	public Vector Normalized()
	{
		var inverseLength = InverseLength();

		return new(X * inverseLength, Y * inverseLength);
	}

	public Position Round() => new(MathUtil.Round(X), MathUtil.Round(Y));
}

public delegate Vector PositionTransformation(Vector vector);