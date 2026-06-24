using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Amber.IO.Common.Serialization;
using Amber.IO.FileFormats.Serialization;
using AmberIsland.GameData;

namespace AmberIslandMapEditor;

/// <summary>
/// A tileset loaded for one map layer: the tileset definition (tile list) plus
/// the rendered graphic atlas it draws from. A map cell stores a tile value
/// (0 = no tile, otherwise 1-based into <see cref="Tileset"/>.Tiles); each tile
/// references an <c>ImageIndex</c> frame in the atlas.
/// </summary>
internal sealed class LayerTileset : IDisposable
{
    public const int TileSize = 16;

    public Tileset Tileset { get; }
    public Bitmap Atlas { get; }
    public int FramesPerRow { get; }

    /// <summary>Highest selectable tile value (Tiles are addressed 1-based; 0 means empty).</summary>
    public int TileCount => Tileset.Tiles.Length;

    private LayerTileset(Tileset tileset, Bitmap atlas)
    {
        Tileset = tileset;
        Atlas = atlas;
        FramesPerRow = Math.Max(1, atlas.Width / TileSize);
    }

    public static LayerTileset Load(string tilesetPath, string atlasPath)
    {
        var tileset = ReadFirst(tilesetPath, Tileset.Read);
        var atlasSprite = ReadFirst(atlasPath, Sprite.Read);

        if (atlasSprite.Colors.Length == 0)
            throw new InvalidDataException("The tileset atlas sprite has no embedded palette and cannot be rendered.");

        return new LayerTileset(tileset, SpriteToBitmap(atlasSprite));
    }

    /// <summary>The source rectangle in the atlas for a map cell value, or null for "no tile" / invalid.</summary>
    public Rectangle? GetTileSourceRect(int value)
    {
        if (value <= 0 || value > Tileset.Tiles.Length)
            return null;

        int frame = Tileset.Tiles[value - 1].ImageIndex;
        int col = frame % FramesPerRow;
        int row = frame / FramesPerRow;
        return new Rectangle(col * TileSize, row * TileSize, TileSize, TileSize);
    }

    private static T ReadFirst<T>(string path, Func<IDataReader, T> read)
    {
        using var stream = File.OpenRead(path);
        var files = FileContainer.ReadAllFiles(stream);

        if (files.Count == 0)
            throw new InvalidDataException($"The container \"{Path.GetFileName(path)}\" is empty.");

        // Use the lowest file index (game containers store a single entry at index 1).
        var data = files.OrderBy(f => f.Key).First().Value;
        return read(new DataReader(data));
    }

    private static Bitmap SpriteToBitmap(Sprite sprite)
    {
        var graphic = Graphic.FromSpriteWithEmbeddedPalette(sprite);
        int w = graphic.Width;
        int h = graphic.Height;

        var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

        var buffer = new byte[w * h * 4];
        for (int i = 0; i < graphic.Pixels.Length; i++)
        {
            var c = graphic.Pixels[i];
            int o = i * 4;
            buffer[o] = c.B;
            buffer[o + 1] = c.G;
            buffer[o + 2] = c.R;
            buffer[o + 3] = c.A;
        }

        Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
        bmp.UnlockBits(data);
        return bmp;
    }

    public void Dispose() => Atlas.Dispose();
}
