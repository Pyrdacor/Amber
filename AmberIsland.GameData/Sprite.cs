using Amber.IO.Common.Serialization;
using Amber.IO.FileFormats.Compression;

namespace AmberIsland.GameData;

public readonly record struct Sprite
(
    ushort Width,
    ushort Height,
    ColorRgb[] Colors, // Only for embedded (fixed) palettes!
    byte[] ColorIndices
)
{
    public void Write(IDataWriter writer)
    {
        writer.Write(Width);
        writer.Write(Height);
        writer.Write((ushort)Colors.Length);

        foreach (var color in Colors)
            color.Write(writer);

        writer.Write(Deflate.Compress(ColorIndices));
    }

    public static Sprite Read(IDataReader reader)
    {
        ushort width = reader.ReadWord();
        ushort height = reader.ReadWord();
        int colorCount = reader.ReadWord();
        var colors = new ColorRgb[colorCount];

        for (int i = 0; i < colorCount; i++)
            colors[i] = ColorRgb.Read(reader);

        var indices = Deflate.Decompress(reader.ReadToEnd());

        return new Sprite(width, height, colors, indices);
    }
}

public readonly record struct SpriteWithPalettes
(
    ushort Width,
    ushort Height,
    PaletteRgb[] Palettes,
    byte[] ColorIndices
)
{
    public void Write(IDataWriter writer)
    {
        writer.Write(Width);
        writer.Write(Height);
        writer.Write((byte)Palettes.Length);
        writer.Write((byte)Palettes.First().Colors.Length);

        foreach (var palette in Palettes)
        {
            foreach (var color in palette.Colors)
                color.Write(writer);
        }

        writer.Write(Deflate.Compress(ColorIndices));
    }

    public static SpriteWithPalettes Read(IDataReader reader)
    {
        ushort width = reader.ReadWord();
        ushort height = reader.ReadWord();
        int paletteCount = reader.ReadByte();
        var palettes = new PaletteRgb[paletteCount];
        int colorCount = reader.ReadByte();

        for (int i = 0; i < paletteCount; i++)
        {
            var colors = new ColorRgb[colorCount];

            for (int c = 0; c < colorCount; c++)
                colors[c] = ColorRgb.Read(reader);

            palettes[i] = new PaletteRgb(colors);
        }

        var indices = Deflate.Decompress(reader.ReadToEnd());

        return new SpriteWithPalettes(width, height, palettes, indices);
    }

    public static SpriteWithPalettes FromSpriteAndPalettes(Sprite sprite, params PaletteRgb[] palettes)
    {
        if (palettes == null || palettes.Length == 0)
            throw new InvalidOperationException("No palettes were given");

        int colorCount = palettes[0].Colors.Length;

        if (palettes.Skip(1).Any(palette => palette.Colors.Length != colorCount))
            throw new InvalidOperationException("All palettes must have the same amount of colors");

        return new SpriteWithPalettes(sprite.Width, sprite.Height, palettes, sprite.ColorIndices);
    }
}
