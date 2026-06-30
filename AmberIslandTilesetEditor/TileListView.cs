using System.Drawing.Drawing2D;
using AmberIsland.GameData;
using Font = System.Drawing.Font;

namespace AmberIslandTilesetEditor;

/// <summary>
/// A single-select, vertically scrolling list of tiles. Each row shows an
/// animated 16×16 preview (animated when FrameCount &gt; 1), the tile type and
/// the frame count. Width is driven by the host; height grows to fit.
/// </summary>
internal sealed class TileListView : Control
{
    private const int RowHeight = 44;
    private const int PreviewSize = 32;
    private static readonly Font TypeFont = new(SystemFonts.DefaultFont!, FontStyle.Bold);

    private List<Tile> tiles = [];
    private Atlas? atlas;
    private int selectedIndex = -1;
    private int step;

    public event Action<int>? SelectedIndexChanged;

    public TileListView()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(48, 48, 52);
        ForeColor = Color.White;
    }

    public void SetData(List<Tile> tiles, Atlas? atlas)
    {
        this.tiles = tiles;
        this.atlas = atlas;
        if (selectedIndex >= tiles.Count)
            selectedIndex = tiles.Count - 1;
        Relayout();
        Invalidate();
    }

    public void RefreshTiles()
    {
        Relayout();
        Invalidate();
    }

    public int AnimationStep
    {
        set { step = value; Invalidate(); }
    }

    public int SelectedIndex
    {
        get => selectedIndex;
        set
        {
            selectedIndex = value;
            Invalidate();
        }
    }

    public void Relayout() => Height = Math.Max(tiles.Count * RowHeight, RowHeight);

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;

        var clip = e.ClipRectangle;
        int first = Math.Max(0, clip.Top / RowHeight);
        int last = Math.Min(tiles.Count - 1, clip.Bottom / RowHeight);

        for (int i = first; i <= last; i++)
        {
            int y = i * RowHeight;
            var tile = tiles[i];

            if (i == selectedIndex)
            {
                using var sel = new SolidBrush(Color.FromArgb(70, 90, 140));
                g.FillRectangle(sel, 0, y, Width, RowHeight);
            }

            // Preview
            var dest = new Rectangle(6, y + (RowHeight - PreviewSize) / 2, PreviewSize, PreviewSize);
            using (var border = new Pen(Color.FromArgb(90, 90, 96)))
                g.DrawRectangle(border, dest.X - 1, dest.Y - 1, dest.Width + 1, dest.Height + 1);

            if (atlas != null)
            {
                int frame = TileAnimation.CurrentFrame(tile, step);
                if (atlas.IsValidFrame(frame))
                    g.DrawImage(atlas.Bitmap, dest, atlas.GetFrameRect(frame), GraphicsUnit.Pixel);
            }

            // Text
            int tx = dest.Right + 10;
            g.DrawString($"{i}: {tile.Type}", TypeFont, Brushes.White, tx, y + 5);
            string frames = tile.FrameCount > 1 ? $"{tile.FrameCount} frames" : "1 frame";
            g.DrawString($"image {tile.ImageIndex} • {frames}", Font, Brushes.Silver, tx, y + 23);

            using var line = new Pen(Color.FromArgb(60, 60, 64));
            g.DrawLine(line, 0, y + RowHeight - 1, Width, y + RowHeight - 1);
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        int index = e.Y / RowHeight;
        if (index < 0 || index >= tiles.Count)
            return;
        selectedIndex = index;
        Invalidate();
        SelectedIndexChanged?.Invoke(index);
    }
}
