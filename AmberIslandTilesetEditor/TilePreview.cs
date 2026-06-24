using System.Drawing.Drawing2D;
using AmberIsland.GameData;

namespace AmberIslandTilesetEditor;

/// <summary>Shows the currently edited tile, animated, scaled to fill the control.</summary>
internal sealed class TilePreview : Control
{
    private Tile? tile;
    private Atlas? atlas;
    private int step;

    public TilePreview()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(30, 30, 30);
    }

    public void SetTile(Tile? tile, Atlas? atlas)
    {
        this.tile = tile;
        this.atlas = atlas;
        Invalidate();
    }

    public int AnimationStep
    {
        set { step = value; Invalidate(); }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (tile is null || atlas is null)
            return;

        int frame = TileAnimation.CurrentFrame(tile.Value, step);
        if (!atlas.IsValidFrame(frame))
            return;

        var g = e.Graphics;
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;

        int zoom = Math.Max(1, Math.Min(ClientSize.Width, ClientSize.Height) / Atlas.TileSize);
        int size = Atlas.TileSize * zoom;
        int x = (ClientSize.Width - size) / 2;
        int y = (ClientSize.Height - size) / 2;

        g.DrawImage(atlas.Bitmap, new Rectangle(x, y, size, size), atlas.GetFrameRect(frame), GraphicsUnit.Pixel);
    }
}
