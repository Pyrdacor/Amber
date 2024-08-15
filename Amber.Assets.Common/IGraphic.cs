namespace Amber.Assets.Common;

public interface IGraphic
{
	int Width { get; }
	int Height { get; }
	bool UsePalette { get; }
	byte[] GetPixelData();
}
