using Amber.IO.Common.Serialization;

namespace AmberIsland.GameData;

/// <summary>
/// References a single sprite from a source container, together with
/// the indices of the palettes it may use (0-based into <see cref="SpriteSheet.Palettes"/>).
/// </summary>
public readonly record struct SpriteSheetEntry
(
    uint SpriteIndex,
    byte[] PaletteIndices
)
{
    public void Write(IDataWriter writer)
    {
        writer.Write((ushort)SpriteIndex);
        writer.Write((byte)PaletteIndices.Length);

        foreach (var index in PaletteIndices)
            writer.Write(index);
    }

    public static SpriteSheetEntry Read(IDataReader reader)
    {
        uint spriteIndex = reader.ReadWord();
        int paletteCount = reader.ReadByte();
        var paletteIndices = new byte[paletteCount];

        for (int i = 0; i < paletteCount; i++)
            paletteIndices[i] = reader.ReadByte();

        return new(spriteIndex, paletteIndices);
    }
}

/// <summary>
/// A lightweight, map-specific selection of sprites and their palettes.
/// Contains all unique palettes and, for each selected sprite, the
/// original container index plus which palettes it may use.
///
/// Does not contain pixel data — the source sprite container is needed
/// to build the final <see cref="MapSpriteAtlas"/>.
/// </summary>
public record SpriteSheet
(
    PaletteRgb[] Palettes,
    SpriteSheetEntry[] Entries
)
{
    public void Write(IDataWriter writer)
    {
        writer.Write((ushort)Palettes.Length);

        foreach (var palette in Palettes)
            palette.Write(writer);

        writer.Write((ushort)Entries.Length);

        foreach (var entry in Entries)
            entry.Write(writer);
    }

    public static SpriteSheet Read(IDataReader reader)
    {
        int paletteCount = reader.ReadWord();
        var palettes = new PaletteRgb[paletteCount];

        for (int i = 0; i < paletteCount; i++)
            palettes[i] = PaletteRgb.Read(reader);

        int entryCount = reader.ReadWord();
        var entries = new SpriteSheetEntry[entryCount];

        for (int i = 0; i < entryCount; i++)
            entries[i] = SpriteSheetEntry.Read(reader);

        return new(palettes, entries);
    }
}
