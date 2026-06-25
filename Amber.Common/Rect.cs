namespace Amber.Common;

public readonly struct Rect : IEquatable<Rect>
{
	public readonly Position Position;
	public readonly Size Size;
	public bool Empty => Size.Empty;
	public int Left => Position.X;
	public int Top => Position.Y;
	public int Right => Math.Min(int.MaxValue, Left + Size.Width);
	public int Bottom => Math.Min(int.MaxValue, Top + Size.Height);
	public Position UpperLeft => new(Left, Top);
	public Position UpperRight => new(Right, Top);
	public Position LowerLeft => new(Left, Bottom);
	public Position LowerRight => new(Right, Bottom);
	public Position Center => new(Math.Min(int.MaxValue, Left + Size.Width / 2), Math.Min(int.MaxValue, Top + Size.Height / 2));

	public Rect()
	{

	}

	public Rect(Position position, Size size)
	{
		Position = position;
		Size = size;
	}

	public Rect(int x, int y, int width, int height)
	{
		Position = new(x, y);
		Size = new(width, height);
	}

	public Rect(int x, int y, uint width, uint height)
	{
		Position = new(x, y);
		Size = new(width, height);
	}

	public static Rect FromBoundaries(int left, int top, int right, int bottom)
	{
		return new(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
	}

	public bool Contains(Position position)
	{
		return position.X >= Left && position.X < Right &&
			   position.Y >= Top && position.Y < Bottom;
	}

    public bool Overlaps(Rect rect)
    {
		var ul1 = Position;
		var lr1 = Position + Size;
		var ul2 = rect.Position;
		var lr2 = rect.Position + rect.Size;

		if (ul1.X > lr2.X || ul2.X > lr1.X)
			return false;

        if (ul1.Y > lr2.Y || ul2.Y > lr1.Y)
            return false;

        return true;
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

	public void Clip(ref Position position, ref Size size)
	{
		int right = position.X + size.Width;
		int bottom = position.Y + size.Height;
		position = new(Math.Max(Left, position.X), Math.Max(Top, position.Y));
		size = new(MathUtil.Limit(0, right - position.X, size.Width), MathUtil.Limit(0, bottom - position.Y, size.Height));
	}

	public void Clip(ref Vector position, ref FloatSize size)
	{
		float right = position.X + size.Width;
		float bottom = position.Y + size.Height;
		position = new(Math.Max(Left, position.X), Math.Max(Top, position.Y));
		size = new(MathUtil.Limit(0, right - position.X, size.Width), MathUtil.Limit(0, bottom - position.Y, size.Height));
	}

	public void Clip(ref Vector position, ref Size size)
	{
		float right = position.X + size.Width;
		float bottom = position.Y + size.Height;
		position = new(Math.Max(Left, position.X), Math.Max(Top, position.Y));
		size = new(MathUtil.Limit(0, MathUtil.Round(right - position.X), size.Width), MathUtil.Limit(0, MathUtil.Round(bottom - position.Y), size.Height));
	}

	public bool Equals(Rect other)
	{
		return Position == other.Position && Size == other.Size;
	}

	public override bool Equals(object? obj)
	{
		return obj is Rect rect && Equals(rect);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Position, Size);
	}

	public static bool operator ==(Rect left, Rect right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(Rect left, Rect right)
	{
		return !(left == right);
	}

	public override readonly string ToString() => $"{Position} {Size}";

    public void Deconstruct(out int x, out int y, out int width, out int height)
    {
        (x, y) = Position;
		(width, height) = Size;
    }

	public static Rect operator+(Rect rect, Position offset)
	{
		return new(rect.Position + offset, rect.Size);
	}

    public static Rect operator-(Rect rect, Position offset)
    {
        return new(rect.Position - offset, rect.Size);
    }
}
