namespace Amber.Assets.Common;

public enum GraphicFormat
{
    PaletteIndices,
    RGBA,
}

public static class GraphicFormatExtensions
{
    public static int BytesPerPixel(this GraphicFormat format) => format switch
    {
	GraphicFormat.PaletteIndices => 1,
	_ => 4
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
}
