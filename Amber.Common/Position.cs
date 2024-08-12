namespace Amber.Common;

public readonly struct Position
{
	public readonly int X;
	public readonly int Y;

	public Position()
	{

	}

	public Position(int x, int y)
	{
		X = x;
		Y = y;
	}

	public override readonly string ToString() => $"({X}, {Y})";
}
