namespace Amber.Assets.Common;

public enum GraphicFormat
{
    Planar1Bit,
    Planar3Bit,
    Planar4Bit,
    Planar5Bit,
    R8G8B8A8,
    X4R4G4B4, // Palettes
}

public static class GraphicFormatExtensions
{
    public static int BitsPerPixel(this GraphicFormat format) => format switch
    {
	GraphicFormat.Planar1Bit => 1,
	GraphicFormat.Planar3Bit => 3,
	GraphicFormat.Planar4Bit => 4,
	GraphicFormat.Planar5Bit => 5,
	GraphicFormat.X4R4G4B4 => 16,
	_ => 32
    };

    public static bool UsesPalette(this GraphicFormat format) => format switch
    {
	GraphicFormat.X4R4G4B4 => false,
	GraphicFormat.R8G8B8A8 => false,
	_ => true
    };
}

public interface IGraphic
{
    int Width { get; }
    int Height { get; }
    GraphicFormat Format { get; }

    byte[] GetPixelData();
}
