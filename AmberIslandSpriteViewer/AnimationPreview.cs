using System.Drawing.Drawing2D;
using AmberIsland.GameData;

namespace AmberIslandSpriteViewer;

/// <summary>
/// Cycles through the frames of an <see cref="Animation"/> taken from a sprite
/// sheet. The selected <see cref="Direction"/> shifts the source rectangle by
/// the animation's DirectionOffset (Down=0, Up=1, Right=2, Left=3 times).
/// </summary>
internal sealed class AnimationPreview : Panel
{
    private readonly System.Windows.Forms.Timer timer = new();
    private int step;

    private Bitmap? sheet;
    private Animation? animation;

    public AnimationPreview()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(30, 30, 30);
        timer.Tick += (_, _) =>
        {
            var anim = animation;
            if (anim is null || anim.Value.FrameIndices.Length == 0)
                return;
            step = (step + 1) % anim.Value.FrameIndices.Length;
            Invalidate();
        };
    }

    public Bitmap? Sheet
    {
        get => sheet;
        set { sheet = value; Invalidate(); }
    }

    public Animation? Animation
    {
        get => animation;
        set { animation = value; step = 0; Invalidate(); }
    }

    public Direction Direction { get; set; }

    public int Zoom { get; set; } = 4;

    public int Fps
    {
        get => fps;
        set
        {
            fps = Math.Max(1, value);
            timer.Interval = Math.Max(1, 1000 / fps);
        }
    }
    private int fps = 8;

    public bool Playing
    {
        get => timer.Enabled;
        set
        {
            timer.Enabled = value;
            if (!value)
                Invalidate();
        }
    }

    public void Restart()
    {
        step = 0;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;
        var anim = animation;

        if (sheet is null || anim is null || anim.Value.FrameIndices.Length == 0)
            return;

        var a = anim.Value;
        int fw = a.FrameSize.Width;
        int fh = a.FrameSize.Height;
        if (fw <= 0 || fh <= 0)
            return;

        int framesPerRow = Math.Max(1, sheet.Width / fw);
        if (step >= a.FrameIndices.Length)
            step = 0;

        uint frame = a.FrameIndices[step];
        int col = (int)(frame % framesPerRow);
        int row = (int)(frame / framesPerRow);

        int dirFactor = (int)Direction;
        var offset = a.DirectionOffset ?? Amber.Common.Position.Zero;

        int srcX = col * fw + dirFactor * offset.X;
        int srcY = row * fh + dirFactor * offset.Y;

        var src = new Rectangle(srcX, srcY, fw, fh);

        int destW = fw * Zoom;
        int destH = fh * Zoom;
        int destX = (ClientSize.Width - destW) / 2;
        int destY = (ClientSize.Height - destH) / 2;

        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;

        // Guard against source rectangles that fall outside the sheet.
        if (src.Right <= sheet.Width && src.Bottom <= sheet.Height && src.X >= 0 && src.Y >= 0)
        {
            g.DrawImage(sheet, new Rectangle(destX, destY, destW, destH), src, GraphicsUnit.Pixel);
        }
        else
        {
            using var pen = new Pen(Color.OrangeRed, 1);
            g.DrawRectangle(pen, destX, destY, destW - 1, destH - 1);
            g.DrawString("frame\nout of\nsheet", Font, Brushes.OrangeRed, destX + 4, destY + 4);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            timer.Dispose();
        base.Dispose(disposing);
    }
}
