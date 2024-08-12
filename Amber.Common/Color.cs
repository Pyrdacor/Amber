namespace Amber.Common;

public readonly struct Color
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

	public static Color Black = new(0, 0, 0);
	public static Color White = new(255, 255, 255);
	public static Color Transparent = new(0, 0, 0, 0);
	public static Color Red = new(255, 0, 0);
	public static Color Green = new(0, 255, 0);
	public static Color Blue = new(0, 0, 255);
	public static Color Yellow = new(255, 255, 0);
	public static Color Azure = new(0, 255, 255);
	public static Color Magenta = new(255, 0, 255);

	public override readonly string ToString() => $"RGBA({R},{G},{B},{A})";
}
