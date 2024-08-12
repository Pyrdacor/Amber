namespace Amber.Common;

public readonly struct Rect
{
	public readonly Position Position;
	public readonly Size Size;
	public bool Empty => Size.Empty;
	public int Left => Position.X;
	public int Top => Position.Y;
	public int Right => (int)Math.Min(int.MaxValue, Left + Size.Width);
	public int Bottom => (int)Math.Min(int.MaxValue, Top + Size.Height);
	public Position UpperLeft => new(Left, Top);
	public Position UpperRight => new(Right, Top);
	public Position LowerLeft => new(Left, Bottom);
	public Position LowerRight => new(Right, Bottom);
	public Position Center => new((int)Math.Min(int.MaxValue, Left + Size.Width / 2), (int)Math.Min(int.MaxValue, Top + Size.Height / 2));

	public Rect()
	{

	}

	public Rect(Position position, Size size)
	{
		Position = position;
		Size = size;
	}

	public Rect(int x, int y, uint width, uint height)
	{
		Position = new(x, y);
		Size = new(width, height);
	}

	public static Rect FromBoundaries(int left, int top, int right, int bottom)
	{
		return new(left, top, (uint)Math.Max(0, right - left), (uint)Math.Max(0, bottom - top));
	}

	public bool Contains(Position position)
	{
		return position.X >= Left && position.X < Right &&
			   position.Y >= Top && position.Y < Bottom;
	}

	public Rect Clip(Rect rect)
	{
		return FromBoundaries
		(
			Math.Max(Left, rect.Left),
			Math.Max(Top, rect.Top),
			Math.Min(Right, rect.Right),
			Math.Min(Bottom, rect.Bottom)
		);
	}

	public override readonly string ToString() => $"{Position} {Size}";
}
