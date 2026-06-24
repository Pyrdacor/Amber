namespace AmberIslandMapEditor;

/// <summary>
/// Shows the active layer's tileset as a grid of selectable tiles (cell 0 is the
/// "no tile" eraser). The selected tile is highlighted. Width is driven by the
/// host panel; height grows to fit so the host can scroll vertically.
/// </summary>
internal sealed class TilesetView : Control
{
    private const int Cell = 34; // 32px tile preview + 2px gap

    private LayerTileset? tileset;
    private int selected;

    public event Action<int>? SelectedChanged;

    public TilesetView()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(48, 48, 52);
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

    private int Columns => Math.Max(1, Width / Cell);

    private int CellCount => (tileset?.TileCount ?? 0) + 1; // +1 for the "no tile" cell

    public void Relayout()
    {
        int rows = (CellCount + Columns - 1) / Columns;
        Height = Math.Max(rows * Cell, Cell);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Relayout();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

        int cols = Columns;
        for (int value = 0; value < CellCount; value++)
        {
            int c = value % cols;
            int r = value / cols;
            var rect = new Rectangle(c * Cell, r * Cell, Cell, Cell);
            var inner = new Rectangle(rect.X + 1, rect.Y + 1, Cell - 3, Cell - 3);

            if (value == 0)
            {
                // "No tile" eraser cell: checkerboard so it reads as transparent.
                DrawChecker(g, inner);
            }
            else
            {
                var src = tileset?.GetTileSourceRect(value);
                if (src != null)
                    g.DrawImage(tileset!.Atlas, inner, src.Value, GraphicsUnit.Pixel);
            }

            if (value == selected)
            {
                using var pen = new Pen(Color.Yellow, 2);
                g.DrawRectangle(pen, rect.X + 1, rect.Y + 1, Cell - 3, Cell - 3);
            }
        }
    }

    private static void DrawChecker(Graphics g, Rectangle r)
    {
        const int s = 8;
        using var light = new SolidBrush(Color.FromArgb(110, 110, 116));
        using var dark = new SolidBrush(Color.FromArgb(80, 80, 86));
        for (int y = 0; y < r.Height; y += s)
        {
            for (int x = 0; x < r.Width; x += s)
            {
                var brush = ((x / s + y / s) % 2 == 0) ? light : dark;
                g.FillRectangle(brush, r.X + x, r.Y + y, Math.Min(s, r.Width - x), Math.Min(s, r.Height - y));
            }
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        int cols = Columns;
        int c = e.X / Cell;
        int r = e.Y / Cell;
        if (c < 0 || c >= cols)
            return;
        int value = r * cols + c;
        if (value < 0 || value >= CellCount)
            return;

        selected = value;
        Invalidate();
        SelectedChanged?.Invoke(value);
    }
}
