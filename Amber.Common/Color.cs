namespace Amber.Common;

public readonly struct Color : IEquatable<Color>
{
	public readonly byte R;
	public readonly byte G;
	public readonly byte B;
	public readonly byte A;
	public bool IsTransparent => A == 0;

	public Color()
	{

	}

	public Color(byte r, byte g, byte b, byte a = 255)
	{
		R = r;
		G = g;
		B = b;
		A = a;
	}

	public static readonly Color Black = new(0, 0, 0);
	public static readonly Color White = new(255, 255, 255);
	public static readonly Color Transparent = new(0, 0, 0, 0);
	public static readonly Color Red = new(255, 0, 0);
	public static readonly Color Green = new(0, 255, 0);
	public static readonly Color Blue = new(0, 0, 255);
	public static readonly Color Yellow = new(255, 255, 0);
	public static readonly Color Azure = new(0, 255, 255);
	public static readonly Color Magenta = new(255, 0, 255);

	public bool Equals(Color other)
	{
		return R == other.R && G == other.G && B == other.B && A == other.A;
	}

	public override bool Equals(object? obj)
	{
		return obj is Color color && Equals(color);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(R, G, B, A);
	}

	public static bool operator ==(Color left, Color right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(Color left, Color right)
	{
		return !(left == right);
	}

	public override readonly string ToString() => $"RGBA({R},{G},{B},{A})";
}
