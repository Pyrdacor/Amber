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

    /// <summary>
    /// Raw pixel data, laid out according to <see cref="Format"/>.
    /// </summary>
    byte[] GetData();
    /// <summary>
    /// Resolves the color of the pixel at the given coordinates (resolving the palette for <see cref="GraphicFormat.PaletteIndices"/>).
    /// </summary>
    Color GetColorAt(int x, int y);
}

// Those directly contain and provide a palette
public interface IPaletteGraphic : IGraphic
{
    IGraphic Palette { get; }
}
