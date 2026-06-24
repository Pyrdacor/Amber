using System.Drawing.Drawing2D;

namespace AmberIslandTilesetEditor;

/// <summary>
/// Scrollable, zoomable view of the atlas with a 16×16 grid, optional 0-based
/// frame indices, and a highlight around the selected tile's image frame.
/// Clicking a cell raises <see cref="FrameClicked"/>.
/// </summary>
internal sealed class AtlasCanvas : Panel
{
    private static readonly Font NumberFont = new("Consolas", 7f, FontStyle.Bold);

    private Atlas? atlas;
    private int zoom = 3;
    private int highlightFrame = -1;

    public event Action<int>? FrameClicked;

    public AtlasCanvas()
    {
        DoubleBuffered = true;
        AutoScroll = true;
        BackColor = Color.FromArgb(45, 45, 48);
    }

    public Atlas? Atlas
    {
        get => atlas;
        set { atlas = value; UpdateScrollSize(); Invalidate(); }
    }

    public int Zoom
    {
        get => zoom;
        set { zoom = Math.Clamp(value, 1, 16); UpdateScrollSize(); Invalidate(); }
    }

    public bool ShowGrid { get; set; } = true;
    public bool ShowIndices { get; set; } = true;

    public int HighlightFrame
    {
        get => highlightFrame;
        set { highlightFrame = value; Invalidate(); }
    }

    private void UpdateScrollSize()
    {
        AutoScrollMinSize = atlas is null
            ? Size.Empty
            : new Size(atlas.Bitmap.Width * zoom, atlas.Bitmap.Height * zoom);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (atlas is null)
            return;

        var g = e.Graphics;
        g.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;

        int w = atlas.Bitmap.Width;
        int h = atlas.Bitmap.Height;
        const int ts = Atlas.TileSize;

        g.DrawImage(atlas.Bitmap, new Rectangle(0, 0, w * zoom, h * zoom), new Rectangle(0, 0, w, h), GraphicsUnit.Pixel);

        if (ShowGrid)
        {
            using var pen = new Pen(Color.FromArgb(160, Color.Red));
            for (int x = 0; x <= w; x += ts)
                g.DrawLine(pen, x * zoom, 0, x * zoom, h * zoom);
            for (int y = 0; y <= h; y += ts)
                g.DrawLine(pen, 0, y * zoom, w * zoom, y * zoom);
        }

        if (ShowIndices)
        {
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
            for (int frame = 0; frame < atlas.FrameCount; frame++)
            {
                var r = atlas.GetFrameRect(frame);
                float tx = r.X * zoom + 2;
                float ty = r.Y * zoom + 1;
                string text = frame.ToString();
                g.DrawString(text, NumberFont, Brushes.Black, tx + 1, ty + 1);
                g.DrawString(text, NumberFont, Brushes.Yellow, tx, ty);
            }
        }

        if (highlightFrame >= 0 && atlas.IsValidFrame(highlightFrame))
        {
            var r = atlas.GetFrameRect(highlightFrame);
            using var pen = new Pen(Color.Lime, 2);
            g.DrawRectangle(pen, r.X * zoom + 1, r.Y * zoom + 1, ts * zoom - 2, ts * zoom - 2);
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (atlas is null)
            return;

        int imgX = (e.X - AutoScrollPosition.X) / zoom;
        int imgY = (e.Y - AutoScrollPosition.Y) / zoom;
        int col = imgX / Atlas.TileSize;
        int row = imgY / Atlas.TileSize;
        if (col < 0 || col >= atlas.FramesPerRow || row < 0 || row >= atlas.FrameRows)
            return;

        int frame = row * atlas.FramesPerRow + col;
        if (atlas.IsValidFrame(frame))
            FrameClicked?.Invoke(frame);
    }
}
