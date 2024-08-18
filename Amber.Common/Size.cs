namespace Amber.Common;

public readonly struct Size
{
	public readonly int Width;
	public readonly int Height;
	public bool Empty => Width <= 0 || Height <= 0;

	public Size()
	{

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

	public override readonly string ToString() => $"[{Width}x{Height}]";
}
