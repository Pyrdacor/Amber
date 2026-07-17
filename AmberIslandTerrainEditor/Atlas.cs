using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using AmberIsland.GameData;

namespace AmberIslandTerrainEditor;

/// <summary>
/// A sprite from a loaded container, rendered to a bitmap and sliced into 16×16 frames,
/// mirroring <c>AmberIslandTilesetEditor.Atlas</c>. The 0-based frame index is what a
/// <see cref="TerrainTextureRef"/> references via AtlasFrame.
/// </summary>
internal sealed class Atlas : IDisposable
{
    public const int TileSize = 16;

    public Bitmap Bitmap { get; }

    /// <summary>The container file index this atlas was loaded from.</summary>
    public uint Index { get; }

    public int FramesPerRow { get; }
    public int FrameRows { get; }
    public int FrameCount => FramesPerRow * FrameRows;

    private Atlas(Bitmap bitmap, uint index)
    {
        Bitmap = bitmap;
        Index = index;
        FramesPerRow = Math.Max(1, bitmap.Width / TileSize);
        FrameRows = Math.Max(1, bitmap.Height / TileSize);
    }

    public Rectangle GetFrameRect(int frame)
    {
        int col = frame % FramesPerRow;
        int row = frame / FramesPerRow;
        return new Rectangle(col * TileSize, row * TileSize, TileSize, TileSize);
    }

    public bool IsValidFrame(int frame) => frame >= 0 && frame < FrameCount;

    public static Atlas FromSprite(Sprite sprite, uint index)
    {
        if (sprite.Colors.Length == 0)
            throw new InvalidDataException("The atlas sprite has no embedded palette and cannot be rendered.");

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
        return new Atlas(bmp, index);
    }

    public void Dispose() => Bitmap.Dispose();
}
