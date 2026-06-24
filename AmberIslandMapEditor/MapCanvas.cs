using AmberIsland.GameData;

namespace AmberIslandMapEditor;

internal enum MapZoom { Out, Normal, In }

internal enum MapTool { Pen, Block2x2, Block2x3, Block3x2, Block3x3, Fill }

/// <summary>
/// The scrollable, zoomable map drawing surface. Owns the three layers and the
/// per-layer tilesets, renders them stacked, and implements the drawing tools.
/// Hosted inside an AutoScroll panel; its size equals the full scaled map.
/// </summary>
internal sealed class MapCanvas : Control
{
    public const int LayerCount = 3;

    private byte[][] layers = [[], [], []];
    private readonly LayerTileset?[] tilesets = new LayerTileset?[LayerCount];
    private readonly ushort[] tilesetIndices = [1, 1, 1];
    private readonly bool[] visible = [true, true, true];

    // Preserved on load so saving round-trips the non-tile map data unchanged.
    private MapEventTrigger[] eventTriggers = [];
    private MapEventCondition[] eventConditions = [];
    private MapEventAction[] eventActions = [];
    private MapEvent[] events = [];
    private MapMonster[] monsters = [];
    private MapNPC[] npcs = [];

    private MapZoom zoom = MapZoom.Normal;
    private Point hoverCell = new(-1, -1);

    private bool painting;
    private MouseButtons paintButton;
    private Point lastPaintCell = new(-1, -1);
    private bool strokeChanged;

    public int MapWidth { get; private set; }
    public int MapHeight { get; private set; }

    public int ActiveLayer { get; set; }
    public MapTool Tool { get; set; } = MapTool.Pen;
    public int SelectedTile { get; set; }
    public bool ShowGrid { get; set; } = true;

    /// <summary>Raised once tiles have actually changed (mark document dirty).</summary>
    public event Action? MapChanged;

    /// <summary>Raised when the eyedropper (right-click pen) picks a tile value off the map.</summary>
    public event Action<int>? TilePicked;

    /// <summary>Raised on hover with the cell under the cursor (-1,-1 when outside the map).</summary>
    public event Action<Point>? HoverChanged;

    public MapCanvas()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(60, 60, 64);
        SetStyle(ControlStyles.ResizeRedraw, false);
    }

    public MapZoom Zoom
    {
        get => zoom;
        set
        {
            if (zoom == value)
                return;
            zoom = value;
            UpdateSize();
            Invalidate();
        }
    }

    public int TileSizePx => zoom switch
    {
        MapZoom.Out => 16,
        MapZoom.In => 48,
        _ => 32
    };

    // ---- Map lifecycle ----

    public void NewMap(int width, int height)
    {
        MapWidth = width;
        MapHeight = height;
        layers = [new byte[width * height], new byte[width * height], new byte[width * height]];
        tilesetIndices[0] = tilesetIndices[1] = tilesetIndices[2] = 1;
        eventTriggers = [];
        eventConditions = [];
        eventActions = [];
        events = [];
        monsters = [];
        npcs = [];
        UpdateSize();
        Invalidate();
    }

    public void LoadMap(Map map)
    {
        MapWidth = map.Width;
        MapHeight = map.Height;
        layers =
        [
            (byte[])map.BackgroundLayer.Clone(),
            (byte[])map.ObjectLayer.Clone(),
            (byte[])map.ForegroundLayer.Clone()
        ];
        tilesetIndices[0] = map.BackgroundTilesetIndex;
        tilesetIndices[1] = map.ObjectTilesetIndex;
        tilesetIndices[2] = map.ForegroundTilesetIndex;
        eventTriggers = map.EventTriggers;
        eventConditions = map.EventConditions;
        eventActions = map.EventActions;
        events = map.Events;
        monsters = map.Monsters;
        npcs = map.NPCs;
        UpdateSize();
        Invalidate();
    }

    public Map ToMap() => new(
        (byte)MapWidth, (byte)MapHeight,
        tilesetIndices[0], tilesetIndices[1], tilesetIndices[2],
        layers[0], layers[1], layers[2],
        eventTriggers, eventConditions, eventActions, events, monsters, npcs);

    // ---- Per-layer configuration (driven by MainForm) ----

    public LayerTileset? GetLayerTileset(int layer) => tilesets[layer];

    public void SetLayerTileset(int layer, LayerTileset? tileset)
    {
        tilesets[layer]?.Dispose();
        tilesets[layer] = tileset;
        Invalidate();
    }

    public bool GetLayerVisible(int layer) => visible[layer];

    public void SetLayerVisible(int layer, bool value)
    {
        visible[layer] = value;
        Invalidate();
    }

    public ushort GetTilesetIndex(int layer) => tilesetIndices[layer];

    public void SetTilesetIndex(int layer, ushort value) => tilesetIndices[layer] = value;

    public byte GetTileValue(int layer, int x, int y) => layers[layer][y * MapWidth + x];

    private void UpdateSize() => Size = new Size(MapWidth * TileSizePx, MapHeight * TileSizePx);

    // ---- Rendering ----

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

        if (MapWidth == 0 || MapHeight == 0)
            return;

        int ts = TileSizePx;
        var clip = e.ClipRectangle;
        int x0 = Math.Max(0, clip.Left / ts);
        int y0 = Math.Max(0, clip.Top / ts);
        int x1 = Math.Min(MapWidth - 1, (clip.Right - 1) / ts);
        int y1 = Math.Min(MapHeight - 1, (clip.Bottom - 1) / ts);

        for (int layer = 0; layer < LayerCount; layer++)
        {
            if (!visible[layer])
                continue;

            var lt = tilesets[layer];
            if (lt == null)
                continue;

            var data = layers[layer];
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    var src = lt.GetTileSourceRect(data[y * MapWidth + x]);
                    if (src == null)
                        continue;
                    g.DrawImage(lt.Atlas, new Rectangle(x * ts, y * ts, ts, ts), src.Value, GraphicsUnit.Pixel);
                }
            }
        }

        if (ShowGrid && ts >= 16)
        {
            using var pen = new Pen(Color.FromArgb(40, Color.Black));
            for (int x = x0; x <= x1 + 1; x++)
                g.DrawLine(pen, x * ts, y0 * ts, x * ts, (y1 + 1) * ts);
            for (int y = y0; y <= y1 + 1; y++)
                g.DrawLine(pen, x0 * ts, y * ts, (x1 + 1) * ts, y * ts);
        }

        if (hoverCell.X >= 0)
        {
            var (bw, bh) = BlockSize(Tool);
            using var pen = new Pen(Color.Yellow, 2);
            g.DrawRectangle(pen, hoverCell.X * ts + 1, hoverCell.Y * ts + 1, bw * ts - 2, bh * ts - 2);
        }
    }

    // ---- Mouse / tools ----

    private Point CellAt(Point p) => new(p.X / TileSizePx, p.Y / TileSizePx);

    private bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < MapWidth && y < MapHeight;

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();

        var cell = CellAt(e.Location);
        if (!InBounds(cell.X, cell.Y))
            return;

        strokeChanged = false;

        switch (Tool)
        {
            case MapTool.Pen when e.Button == MouseButtons.Left:
                painting = true;
                paintButton = MouseButtons.Left;
                SetTile(ActiveLayer, cell.X, cell.Y, SelectedTile);
                lastPaintCell = cell;
                break;

            case MapTool.Pen when e.Button == MouseButtons.Right:
                PickTile(cell);
                break;

            case MapTool.Fill when e.Button == MouseButtons.Left:
                FloodFill(cell, SelectedTile);
                break;

            case MapTool.Fill when e.Button == MouseButtons.Right:
                FillAll(SelectedTile);
                break;

            case MapTool.Block2x2:
            case MapTool.Block2x3:
            case MapTool.Block3x2:
            case MapTool.Block3x3:
                if (e.Button is MouseButtons.Left or MouseButtons.Right)
                {
                    painting = true;
                    paintButton = e.Button;
                    StampBlock(cell, e.Button == MouseButtons.Right);
                    lastPaintCell = cell;
                }
                break;
        }

        if (strokeChanged && !painting)
            MapChanged?.Invoke();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        var cell = CellAt(e.Location);
        var newHover = InBounds(cell.X, cell.Y) ? cell : new Point(-1, -1);
        if (newHover != hoverCell)
        {
            var (bw, bh) = BlockSize(Tool);
            int ts = TileSizePx;
            if (hoverCell.X >= 0)
                Invalidate(new Rectangle(hoverCell.X * ts, hoverCell.Y * ts, bw * ts + 1, bh * ts + 1));
            if (newHover.X >= 0)
                Invalidate(new Rectangle(newHover.X * ts, newHover.Y * ts, bw * ts + 1, bh * ts + 1));
            hoverCell = newHover;
            HoverChanged?.Invoke(hoverCell);
        }

        if (!painting || newHover.X < 0 || cell == lastPaintCell)
            return;

        if (Tool == MapTool.Pen && paintButton == MouseButtons.Left)
            SetTile(ActiveLayer, cell.X, cell.Y, SelectedTile);
        else if (Tool is MapTool.Block2x2 or MapTool.Block2x3 or MapTool.Block3x2 or MapTool.Block3x3)
            StampBlock(cell, paintButton == MouseButtons.Right);

        lastPaintCell = cell;
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (painting)
        {
            painting = false;
            lastPaintCell = new Point(-1, -1);
            if (strokeChanged)
                MapChanged?.Invoke();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (hoverCell.X >= 0)
        {
            var (bw, bh) = BlockSize(Tool);
            int ts = TileSizePx;
            Invalidate(new Rectangle(hoverCell.X * ts, hoverCell.Y * ts, bw * ts + 1, bh * ts + 1));
            hoverCell = new Point(-1, -1);
            HoverChanged?.Invoke(hoverCell);
        }
    }

    private static (int Width, int Height) BlockSize(MapTool tool) => tool switch
    {
        MapTool.Block2x2 => (2, 2),
        MapTool.Block2x3 => (2, 3),
        MapTool.Block3x2 => (3, 2),
        MapTool.Block3x3 => (3, 3),
        _ => (1, 1)
    };

    private int ClampTileValue(int value)
    {
        int max = tilesets[ActiveLayer]?.TileCount ?? 255;
        return Math.Clamp(value, 0, max);
    }

    private void SetTile(int layer, int x, int y, int value)
    {
        var data = layers[layer];
        int i = y * MapWidth + x;
        if (data[i] == value)
            return;
        data[i] = (byte)value;
        strokeChanged = true;
        int ts = TileSizePx;
        Invalidate(new Rectangle(x * ts, y * ts, ts, ts));
    }

    private void PickTile(Point cell)
    {
        int value = layers[ActiveLayer][cell.Y * MapWidth + cell.X];
        SelectedTile = value;
        TilePicked?.Invoke(value);
    }

    private void StampBlock(Point cell, bool consecutive)
    {
        var (bw, bh) = BlockSize(Tool);
        int n = 0;
        for (int dy = 0; dy < bh; dy++)
        {
            for (int dx = 0; dx < bw; dx++)
            {
                int x = cell.X + dx;
                int y = cell.Y + dy;
                int value = consecutive ? ClampTileValue(SelectedTile + n) : SelectedTile;
                n++;
                if (InBounds(x, y))
                    SetTile(ActiveLayer, x, y, value);
            }
        }
    }

    private void FillAll(int value)
    {
        var data = layers[ActiveLayer];
        bool changed = false;
        for (int i = 0; i < data.Length; i++)
        {
            if (data[i] != value)
            {
                data[i] = (byte)value;
                changed = true;
            }
        }
        if (changed)
        {
            strokeChanged = true;
            Invalidate();
            MapChanged?.Invoke();
        }
    }

    private void FloodFill(Point start, int value)
    {
        var data = layers[ActiveLayer];
        int target = data[start.Y * MapWidth + start.X];
        if (target == value)
            return;

        var stack = new Stack<Point>();
        stack.Push(start);

        while (stack.Count > 0)
        {
            var p = stack.Pop();
            if (!InBounds(p.X, p.Y))
                continue;
            int i = p.Y * MapWidth + p.X;
            if (data[i] != target)
                continue;

            data[i] = (byte)value;
            stack.Push(new Point(p.X + 1, p.Y));
            stack.Push(new Point(p.X - 1, p.Y));
            stack.Push(new Point(p.X, p.Y + 1));
            stack.Push(new Point(p.X, p.Y - 1));
        }

        strokeChanged = true;
        Invalidate();
        MapChanged?.Invoke();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var ts in tilesets)
                ts?.Dispose();
        }
        base.Dispose(disposing);
    }
}
