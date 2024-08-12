namespace Amber.Common;

public readonly struct Size
{
	public readonly uint Width;
	public readonly uint Height;
	public bool Empty => Width == 0 || Height == 0;

	public Size()
	{

	}

	public Size(uint width, uint height)
	{
		Width = width;
		Height = height;
	}

	public override readonly string ToString() => $"[{Width}x{Height}]";
}
