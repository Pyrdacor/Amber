using Amber.IO.Common.Serialization;

namespace AmberIsland.GameData;

public readonly record struct PaletteRgb(
    ColorRgb[] Colors
)
{
    public void Write(IDataWriter writer)
    {
        writer.Write((byte)Colors.Length);

        foreach (var color in Colors)
            color.Write(writer);
    }

    public static PaletteRgb Read(IDataReader reader)
    {
        int colorCount = reader.ReadByte();
        var colors = new ColorRgb[colorCount];

        for (int i = 0; i < colorCount; i++)
            colors[i] = ColorRgb.Read(reader);

        return new PaletteRgb(colors);
    }

    public byte[] ToBytes(bool withTransparentColor = true, bool withAlphaComponent = true)
    {
        int colorCount = withTransparentColor ? 1 + Colors.Length : Colors.Length;
        int byteCount = withAlphaComponent ? colorCount * 4 : colorCount * 3;
        var bytes = new byte[byteCount];
        int index = 0;

        void WriteColor(ColorRgb color)
        {
            bytes[index++] = color.R;
            bytes[index++] = color.G;
            bytes[index++] = color.B;

            if (withAlphaComponent)
                bytes[index++] = 255;
        }

        if (withTransparentColor)
        {
            bytes[index++] = 0;
            bytes[index++] = 0;
            bytes[index++] = 0;

            if (withAlphaComponent)
                bytes[index++] = 0;
        }

        foreach (var color in Colors)
            WriteColor(color);

        return bytes;
    }
}

public readonly record struct PaletteRgba(
    ColorRgba[] Colors
)
{
    public void Write(IDataWriter writer)
    {
        writer.Write((byte)Colors.Length);

        foreach (var color in Colors)
            color.Write(writer);
    }

    public static PaletteRgba Read(IDataReader reader)
    {
        int colorCount = reader.ReadByte();
        var colors = new ColorRgba[colorCount];

        for (int i = 0; i < colorCount; i++)
            colors[i] = ColorRgba.Read(reader);

        return new PaletteRgba(colors);
    }

    public byte[] ToBytes(bool withTransparentColor = true)
    {
        int colorCount = withTransparentColor ? 1 + Colors.Length : Colors.Length;
        int byteCount = colorCount * 4;
        var bytes = new byte[byteCount];
        int index = 0;

        void WriteColor(ColorRgba color)
        {
            bytes[index++] = color.R;
            bytes[index++] = color.G;
            bytes[index++] = color.B;
            bytes[index++] = color.A;
        }

        if (withTransparentColor)
        {
            bytes[index++] = 0;
            bytes[index++] = 0;
            bytes[index++] = 0;
            bytes[index++] = 0;
        }

        foreach (var color in Colors)
            WriteColor(color);

        return bytes;
    }
}