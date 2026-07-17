using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using AmberIsland.GameData;

namespace AmberIslandTerrainEditor;

internal enum TerrainViewMode
{
    Height,
    Textured
}

internal enum TerrainZoom
{
    Tiny,
    Small,
    Normal,
    Large
}

/// <summary>
/// Scrollable, zoomable preview of a generated terrain. Composes two off-screen bitmaps
/// (grayscale height view, textured view) once per generation/parameter change, then
/// OnPaint just blits the active one scaled to the current zoom — a single draw call
/// regardless of grid size, mirroring the "compose once, blit scaled" approach used by
/// the sibling editors' atlas canvases.
/// </summary>
internal sealed class TerrainCanvas : Panel
{
    public const int CellPixelSize = Atlas.TileSize;

    private static readonly Dictionary<TileType, Color> PlaceholderColors = new()
    {
        [TileType.DeepWater] = Color.FromArgb(30, 60, 140),
        [TileType.ShallowWater] = Color.FromArgb(70, 130, 220),
        [TileType.Sand] = Color.FromArgb(230, 210, 150),
        [TileType.HotSand] = Color.FromArgb(235, 180, 90),
        [TileType.Mud] = Color.FromArgb(110, 80, 50),
        [TileType.Swamp] = Color.FromArgb(90, 100, 60),
        [TileType.Grass] = Color.FromArgb(70, 150, 60),
        [TileType.HighGrass] = Color.FromArgb(40, 110, 40),
        [TileType.Earth] = Color.FromArgb(120, 90, 60),
        [TileType.Stone] = Color.FromArgb(130, 130, 130),
        [TileType.Ice] = Color.FromArgb(190, 230, 235),
        [TileType.Snow] = Color.FromArgb(245, 245, 250),
    };

    private static readonly (TerrainZoom Zoom, float Factor)[] ZoomFactors =
    [
        (TerrainZoom.Tiny, 0.25f),
        (TerrainZoom.Small, 0.5f),
        (TerrainZoom.Normal, 1.0f),
        (TerrainZoom.Large, 2.0f)
    ];

    private Bitmap? heightBitmap;
    private Bitmap? texturedBitmap;
    private float[]? heights;
    private IReadOnlyList<TerrainTypeRange> ranges = [];
    private int gridWidth;
    private int gridHeight;
    private TerrainZoom zoom = TerrainZoom.Normal;
    private TerrainViewMode viewMode = TerrainViewMode.Textured;

    public event Action<int, int, float, TileType>? HoverChanged;
    public event Action? HoverLeft;

    public TerrainCanvas()
    {
        DoubleBuffered = true;
        AutoScroll = true;
        BackColor = Color.FromArgb(30, 30, 34);
    }

    public TerrainZoom Zoom
    {
        get => zoom;
        set { zoom = value; UpdateScrollSize(); Invalidate(); }
    }

    public TerrainViewMode ViewMode
    {
        get => viewMode;
        set { viewMode = value; Invalidate(); }
    }

    private static float FactorFor(TerrainZoom z) => ZoomFactors.First(f => f.Zoom == z).Factor;

    public void Compose(float[] heightValues, int width, int height, IReadOnlyList<TerrainTypeRange> typeRanges, Func<uint, Atlas?> atlasLookup)
    {
        heights = heightValues;
        ranges = typeRanges;
        gridWidth = width;
        gridHeight = height;

        heightBitmap?.Dispose();
        texturedBitmap?.Dispose();

        int pixelWidth = Math.Max(1, width * CellPixelSize);
        int pixelHeight = Math.Max(1, height * CellPixelSize);

        heightBitmap = new Bitmap(pixelWidth, pixelHeight, PixelFormat.Format24bppRgb);
        texturedBitmap = new Bitmap(pixelWidth, pixelHeight, PixelFormat.Format32bppArgb);

        using var hg = Graphics.FromImage(heightBitmap);
        using var tg = Graphics.FromImage(texturedBitmap);
        tg.InterpolationMode = InterpolationMode.NearestNeighbor;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float h = heightValues[y * width + x];
                byte gray = (byte)Math.Clamp(h * 255f, 0f, 255f);
                var dest = new Rectangle(x * CellPixelSize, y * CellPixelSize, CellPixelSize, CellPixelSize);

                using (var grayBrush = new SolidBrush(Color.FromArgb(gray, gray, gray)))
                    hg.FillRectangle(grayBrush, dest);

                var type = TerrainTypeResolver.Resolve(h, typeRanges);
                var range = typeRanges.FirstOrDefault(r => r.Type == type);
                var atlas = range?.Texture != null ? atlasLookup(range.Texture.SpriteIndex) : null;

                if (atlas != null && range?.Texture != null && atlas.IsValidFrame(range.Texture.AtlasFrame))
                {
                    var src = atlas.GetFrameRect(range.Texture.AtlasFrame);
                    tg.DrawImage(atlas.Bitmap, dest, src, GraphicsUnit.Pixel);
                }
                else
                {
                    var color = PlaceholderColors.TryGetValue(type, out var c) ? c : Color.Magenta;
                    using var brush = new SolidBrush(color);
                    tg.FillRectangle(brush, dest);
                }
            }
        }

        UpdateScrollSize();
        Invalidate();
    }

    public bool TryGetCellInfo(int cellX, int cellY, out float height, out TileType type)
    {
        if (heights == null || cellX < 0 || cellY < 0 || cellX >= gridWidth || cellY >= gridHeight)
        {
            height = 0f;
            type = TileType.Grass;
            return false;
        }

        height = heights[cellY * gridWidth + cellX];
        type = TerrainTypeResolver.Resolve(height, ranges);
        return true;
    }

    private void UpdateScrollSize()
    {
        var bitmap = viewMode == TerrainViewMode.Height ? heightBitmap : texturedBitmap;
        float factor = FactorFor(zoom);
        AutoScrollMinSize = bitmap is null
            ? Size.Empty
            : new Size((int)(bitmap.Width * factor), (int)(bitmap.Height * factor));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var bitmap = viewMode == TerrainViewMode.Height ? heightBitmap : texturedBitmap;
        if (bitmap is null)
            return;

        float factor = FactorFor(zoom);
        var g = e.Graphics;
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;

        var dest = new Rectangle(
            AutoScrollPosition.X, AutoScrollPosition.Y,
            (int)(bitmap.Width * factor), (int)(bitmap.Height * factor));

        g.DrawImage(bitmap, dest, new Rectangle(0, 0, bitmap.Width, bitmap.Height), GraphicsUnit.Pixel);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        float factor = FactorFor(zoom);
        float cellScreenSize = CellPixelSize * factor;
        int cellX = (int)((e.X - AutoScrollPosition.X) / cellScreenSize);
        int cellY = (int)((e.Y - AutoScrollPosition.Y) / cellScreenSize);

        if (TryGetCellInfo(cellX, cellY, out float height, out var type))
            HoverChanged?.Invoke(cellX, cellY, height, type);
        else
            HoverLeft?.Invoke();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        HoverLeft?.Invoke();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            heightBitmap?.Dispose();
            texturedBitmap?.Dispose();
        }
        base.Dispose(disposing);
    }
}
