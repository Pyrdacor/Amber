namespace Amber.Common;

public readonly struct Size : IEquatable<Size>
{
	public readonly int Width;
	public readonly int Height;
	public bool Empty => Width <= 0 || Height <= 0;

	public Size()
	{

	}

	public Size(Size other)
	{
		Width = other.Width;
		Height = other.Height;
	}

	public Size(FloatSize other)
	{
		Width = MathUtil.Round(other.Width);
		Height = MathUtil.Round(other.Height);
	}

	public Size(int width, int height)
	{
		if (width < 0 || height < 0)
			throw new AmberException(ExceptionScope.Application, "Invalid size dimensions.");

		Width = width;
		Height = height;
	}

	public Size(uint width, uint height)
	{
		if (width > int.MaxValue || height > int.MaxValue)
			throw new AmberException(ExceptionScope.Application, "Invalid size dimensions.");

		Width = (int)width;
		Height = (int)height;
	}

	public bool Equals(Size other)
	{
		return Width == other.Width && Height == other.Height;
	}

	public override bool Equals(object? obj)
	{
		return obj is Size size && Equals(size);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Width, Height);
	}

	public static bool operator ==(Size left, Size right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(Size left, Size right)
	{
		return !(left == right);
	}

	public override readonly string ToString() => $"[{Width}x{Height}]";
}

public readonly struct FloatSize : IEquatable<FloatSize>
{
	public readonly float Width;
	public readonly float Height;
	public bool Empty => Width <= 0 || Height <= 0;

	public FloatSize()
	{

	}

	public FloatSize(FloatSize other)
	{
		Width = other.Width;
		Height = other.Height;
	}

	public FloatSize(Size other)
	{
		Width = other.Width;
		Height = other.Height;
	}	

	public FloatSize(float width, float height)
	{
		if (width < 0 || height < 0)
			throw new AmberException(ExceptionScope.Application, "Invalid size dimensions.");

		Width = width;
		Height = height;
	}

	public bool Equals(FloatSize other)
	{
		return Width == other.Width && Height == other.Height;
	}

	public override bool Equals(object? obj)
	{
		return obj is FloatSize size && Equals(size);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Width, Height);
	}

	public static bool operator ==(FloatSize left, FloatSize right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(FloatSize left, FloatSize right)
	{
		return !(left == right);
	}

	public override readonly string ToString() => $"[{Width:0.00}x{Height:0.00}]";
}

public delegate FloatSize SizeTransformation(FloatSize size);
