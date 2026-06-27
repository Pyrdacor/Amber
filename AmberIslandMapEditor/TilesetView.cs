using System.Drawing.Drawing2D;

namespace AmberIslandMapEditor;

/// <summary>
/// A single-select grid of tiles. Each cell shows a 32×32 preview.
/// The grid has a fixed number of columns (<see cref="TilesPerRow"/>);
/// width and height are computed from that. The host scroll-panel handles
/// scrolling when the view is larger than the visible area.
/// Tile values are 1-based (0 = no tile / eraser, handled externally).
/// </summary>
internal sealed class TilesetView : Control
{
    private const int Cell = 34; // 32px tile preview + 2px gap

    private LayerTileset? tileset;
    private int selected;
    private int tilesPerRow = 32;

    public event Action<int>? SelectedChanged;

    public TilesetView()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(48, 48, 52);
    }

    public int TilesPerRow
    {
        get => tilesPerRow;
        set
        {
            tilesPerRow = Math.Max(1, value);
            Relayout();
            Invalidate();
        }
    }

    public void SetTileset(LayerTileset? value)
    {
        tileset = value;
        if (selected > (value?.TileCount ?? 0))
            selected = 0;
        Relayout();
        Invalidate();
    }

    public int SelectedValue
    {
        get => selected;
        set
        {
            if (selected == value)
                return;
            selected = value;
            Invalidate();
        }
    }

    private int TileCount => tileset?.TileCount ?? 0;

    public void Relayout()
    {
        Width = tilesPerRow * Cell;
        int rows = TileCount > 0 ? (TileCount + tilesPerRow - 1) / tilesPerRow : 1;
        Height = Math.Max(rows * Cell, Cell);
    }

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

        int cols = tilesPerRow;
        var clip = e.ClipRectangle;
        int firstRow = Math.Max(0, clip.Top / Cell);
        int lastRow = clip.Bottom / Cell;

        for (int r = firstRow; r <= lastRow; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int index = r * cols + c;
                if (index >= TileCount)
                    return;

                int value = index + 1;
                var rect = new Rectangle(c * Cell, r * Cell, Cell, Cell);
                var inner = new Rectangle(rect.X + 1, rect.Y + 1, Cell - 3, Cell - 3);

                var src = tileset?.GetTileSourceRect(value);
                if (src != null)
                    g.DrawImage(tileset!.Atlas, inner, src.Value, GraphicsUnit.Pixel);

                if (value == selected)
                {
                    using var pen = new Pen(Color.Yellow, 2);
                    g.DrawRectangle(pen, rect.X + 1, rect.Y + 1, Cell - 3, Cell - 3);
                }
            }
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        int cols = tilesPerRow;
        int c = e.X / Cell;
        int r = e.Y / Cell;
        if (c < 0 || c >= cols)
            return;
        int index = r * cols + c;
        if (index < 0 || index >= TileCount)
            return;

        int value = index + 1;
        selected = value;
        Invalidate();
        SelectedChanged?.Invoke(value);
    }
}
