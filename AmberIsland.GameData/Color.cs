using Amber.IO.Common.Serialization;

namespace AmberIsland.GameData;

public readonly record struct ColorRgb(
    byte R,
    byte G,
    byte B
)
{
    public void Write(IDataWriter writer)
    {
        writer.Write(R);
        writer.Write(G);
        writer.Write(B);
    }

    public static ColorRgb Read(IDataReader reader)
    {
        byte r = reader.ReadByte();
        byte g = reader.ReadByte();
        byte b = reader.ReadByte();

        return new ColorRgb(r, g, b);
    }
}

public readonly record struct ColorRgba(
    byte R,
    byte G,
    byte B,
    byte A
)
{
    public static readonly ColorRgba Transparent = new(0, 0, 0, 0);

    public void Write(IDataWriter writer)
    {
        writer.Write(R);
        writer.Write(G);
        writer.Write(B);
        writer.Write(A);
    }

    public static ColorRgba Read(IDataReader reader)
    {
        byte r = reader.ReadByte();
        byte g = reader.ReadByte();
        byte b = reader.ReadByte();
        byte a = reader.ReadByte();

        return new ColorRgba(r, g, b, a);
    }

    public static ColorRgba FromColorRgb(ColorRgb colorRgb)
    {
        return new ColorRgba(colorRgb.R, colorRgb.G, colorRgb.B, 255);
    }
}