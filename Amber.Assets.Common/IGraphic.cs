using Amber.Common;

namespace Amber.Assets.Common;

public enum GraphicFormat
{
    PaletteIndices,
    Alpha, // Fonts etc
    RGBA,
}

public static class GraphicFormatExtensions
{
    public static int BytesPerPixel(this GraphicFormat format) => format switch
    {
        GraphicFormat.RGBA => 4,
	    _ => 1
    };

    public static bool UsesPalette(this GraphicFormat format) => format switch
    {
	    GraphicFormat.PaletteIndices => true,
	    _ => false
    };
}

public interface IGraphic
{
    int Width { get; }
    int Height { get; }
    GraphicFormat Format { get; }

    byte[] GetData();
    Color GetColorAt(int x, int y);
}

// Those directly contain and provide a palette
public interface IPaletteGraphic : IGraphic
{
    IGraphic Palette { get; }
}
