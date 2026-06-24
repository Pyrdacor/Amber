using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace AmberIslandSpriteViewer;

/// <summary>
/// Scrollable, zoomable surface that draws a sprite sheet with an optional
/// frame grid and frame numbers. The grid cell size is the "frame size".
/// </summary>
internal sealed class ImageCanvas : Panel
{
    private Bitmap? image;

    public Bitmap? Image
    {
        get => image;
        set { image = value; UpdateScrollSize(); Invalidate(); }
    }

    private int zoom = 4;
    public int Zoom
    {
        get => zoom;
        set { zoom = Math.Max(1, value); UpdateScrollSize(); Invalidate(); }
    }

    public bool ShowGrid { get; set; } = true;
    public Color GridColor { get; set; } = Color.FromArgb(160, Color.Red);
    public float GridLineWidth { get; set; } = 1f;

    public int FrameWidth { get; set; } = 16;
    public int FrameHeight { get; set; } = 16;

    public bool ShowFrameNumbers { get; set; }

    private static readonly Font NumberFont = new("Consolas", 8f, FontStyle.Bold);

    public ImageCanvas()
    {
        DoubleBuffered = true;
        AutoScroll = true;
        BackColor = Color.FromArgb(45, 45, 48);
    }

    private void UpdateScrollSize()
    {
        AutoScrollMinSize = image is null
            ? Size.Empty
            : new Size(image.Width * zoom, image.Height * zoom);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (image is null)
            return;

        var g = e.Graphics;
        g.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);

        int w = image.Width;
        int h = image.Height;

        // Pixel-perfect upscaling for pixel art.
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        g.DrawImage(image, new Rectangle(0, 0, w * zoom, h * zoom), new Rectangle(0, 0, w, h), GraphicsUnit.Pixel);

        int fw = Math.Max(1, FrameWidth);
        int fh = Math.Max(1, FrameHeight);

        if (ShowGrid)
            DrawGrid(g, w, h, fw, fh);

        if (ShowFrameNumbers)
            DrawFrameNumbers(g, w, fw, fh);
    }

    private void DrawGrid(Graphics g, int w, int h, int fw, int fh)
    {
        using var pen = new Pen(GridColor, Math.Max(0.1f, GridLineWidth));

        int totalW = w * zoom;
        int totalH = h * zoom;

        for (int x = 0; x <= w; x += fw)
            g.DrawLine(pen, x * zoom, 0, x * zoom, totalH);

        for (int y = 0; y <= h; y += fh)
            g.DrawLine(pen, 0, y * zoom, totalW, y * zoom);
    }

    private void DrawFrameNumbers(Graphics g, int w, int fw, int fh)
    {
        int framesPerRow = Math.Max(1, w / fw);
        int rows = (image!.Height + fh - 1) / fh;
        int cols = (w + fw - 1) / fw;

        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                int number = row * framesPerRow + col;
                string text = number.ToString();
                float tx = col * fw * zoom + 2;
                float ty = row * fh * zoom + 1;

                // Shadow for readability over arbitrary backgrounds.
                g.DrawString(text, NumberFont, Brushes.Black, tx + 1, ty + 1);
                g.DrawString(text, NumberFont, Brushes.Yellow, tx, ty);
            }
        }
    }
}
