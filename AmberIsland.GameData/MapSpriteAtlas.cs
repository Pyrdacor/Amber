using Amber.Common;
using Amber.IO.Common.Serialization;
using Amber.IO.FileFormats.Serialization;

namespace AmberIsland.GameData;

/// <summary>
/// Each map sprite contains all frames for a specific map
/// objects like monster, NPC, item or object.
/// </summary>
/// <param name="Position">Position of the sprite texture inside the atlas</param>
/// <param name="Size">Size of the sprite texture</param>
/// <param name="PaletteIndices">Potential palette indices to use</param>
public readonly record struct MapSprite
(
    Position Position,
    Size Size,
    params byte[] PaletteIndices
)
{
    public void Write(IDataWriter writer)
    {
        writer.Write((ushort)Position.X);
        writer.Write((ushort)Position.Y);
        writer.Write((ushort)Size.Width);
        writer.Write((ushort)Size.Height);
        writer.Write((byte)PaletteIndices.Length);

        foreach (var paletteIndex in PaletteIndices)
            writer.Write(paletteIndex);
    }

    public static MapSprite Read(IDataReader reader)
    {
        ushort x = reader.ReadWord();
        ushort y = reader.ReadWord();
        ushort width = reader.ReadWord();
        ushort height = reader.ReadWord();
        int paletteIndexCount = reader.ReadByte();
        var paletteIndices = new byte[paletteIndexCount];

        for (int i = 0; i < paletteIndexCount; i++)
            paletteIndices[i] = reader.ReadByte();

        return new MapSprite(new(x, y), new(width, height), paletteIndices);
    }
}

/// <summary>
/// This is used for monsters, NPCs, objects or items on a map.
/// 
/// For example all monsters on a map share one atlas.
/// </summary>
public readonly record struct MapSpriteAtlas
(
    Sprite Atlas,
    Dictionary<uint, MapSprite> Sprites,
    params PaletteRgb[] Palettes
)
{
    public static MapSpriteAtlas FromSpriteSheet(SpriteSheet spriteSheet, Dictionary<uint, Sprite> sprites)
    {
        var mapSprites = new Dictionary<uint, MapSprite>(sprites.Count);
        var palettes = spriteSheet.Palettes;
        int x = 0;
        int y = 0;
        ushort width = sprites.Max(sprite => sprite.Value.Width);
        ushort height = (ushort)sprites.Sum(sprite => sprite.Value.Height);
        var colorIndices = new byte[width * height];

        foreach (var spriteEntry in spriteSheet.Entries)
        {
            var (index, paletteIndices) = spriteEntry;
            var sprite = sprites[index];

            mapSprites.Add(index, new MapSprite
            (
                Position: new(x, y),
                Size: new(sprite.Width, sprite.Height),
                paletteIndices
            ));

            for (int sy = 0; sy < sprite.Height; sy++)
            {
                Buffer.BlockCopy(sprite.ColorIndices, sy * sprite.Width, colorIndices, (y + sy) * width, sprite.Width);
            }

            y += sprite.Height;
        }

        var atlas = new Sprite(width, height, [], colorIndices);

        return new MapSpriteAtlas(atlas, mapSprites, palettes);
    }

    public static MapSpriteAtlas FromSprites(Dictionary<uint, Sprite> sprites)
    {
        var mapSprites = new Dictionary<uint, MapSprite>(sprites.Count);
        var palettes = new PaletteRgb[sprites.Count];
        int x = 0;
        int y = 0;
        ushort width = sprites.Max(sprite  => sprite.Value.Width);
        ushort height = (ushort)sprites.Sum(sprite => sprite.Value.Height);
        var colorIndices = new byte[width * height];
        byte paletteIndex = 0;

        foreach (var spriteEntry in sprites)
        {
            var (index, sprite) = spriteEntry;

            mapSprites.Add(index, new MapSprite
            (
                Position: new(x, y),
                Size: new(sprite.Width, sprite.Height),
                paletteIndex
            ));
            palettes[paletteIndex++] = new PaletteRgb(sprite.Colors);

            for (int sy = 0; sy < sprite.Height; sy++)
            {
                Buffer.BlockCopy(sprite.ColorIndices, sy * sprite.Width, colorIndices, (y + sy) * width, sprite.Width);
            }

            y += sprite.Height;
        }

        var atlas = new Sprite(width, height, [], colorIndices);

        return new MapSpriteAtlas(atlas, mapSprites, palettes);
    }

    public void Write(IDataWriter writer)
    {
        var atlasWriter = new DataWriter();
        Atlas.Write(atlasWriter);

        writer.Write((uint)atlasWriter.Size);
        writer.Write(atlasWriter.ToArray());

        writer.Write((ushort)Sprites.Count);

        foreach (var spriteEntry in Sprites)
        {
            var (index, sprite) = spriteEntry;

            writer.Write((ushort)index);
            sprite.Write(writer);
        }

        writer.Write((byte)Palettes.Length);

        foreach (var palette in Palettes)
            palette.Write(writer);
    }

    public static MapSpriteAtlas Read(IDataReader reader)
    {
        var atlasDataSize = reader.ReadDword();
        var atlas = Sprite.Read(new DataReader(reader.ReadBytes((int)atlasDataSize)));
        int spriteCount = reader.ReadWord();
        var sprites = new Dictionary<uint, MapSprite>(spriteCount);

        for (int i = 0; i < spriteCount; i++)
        {
            var index = reader.ReadWord();
            var sprite = MapSprite.Read(reader);

            sprites.Add(index, sprite);
        }

        int paletteCount = reader.ReadByte();
        var palettes = new PaletteRgb[paletteCount];

        for (int i = 0; i < paletteCount; i++)
            palettes[i] = PaletteRgb.Read(reader);

        return new MapSpriteAtlas(atlas, sprites, palettes);
    }
}
