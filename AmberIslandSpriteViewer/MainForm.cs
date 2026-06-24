using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Amber.IO.FileFormats.Serialization;
using AmberIsland.GameData;

namespace AmberIslandSpriteViewer;

internal sealed class MainForm : Form
{
    private readonly ImageCanvas canvas = new() { Dock = DockStyle.Fill };
    private readonly AnimationPreview preview = new();

    private string? currentFilePath;

    // File
    private readonly Label fileLabel = new() { Text = "(no file)", AutoSize = true, MaximumSize = new Size(240, 0) };
    private readonly NumericUpDown zoomInput = Num(1, 16, 4);

    // Grid
    private readonly CheckBox gridCheck = new() { Text = "Show grid", Checked = true, AutoSize = true };
    private readonly NumericUpDown frameWidthInput = Num(1, 1024, 16);
    private readonly NumericUpDown frameHeightInput = Num(1, 1024, 16);
    private readonly NumericUpDown lineWidthInput = Num(1, 10, 1);
    private readonly Button gridColorButton = new() { Text = "Color", Width = 80, Height = 24 };
    private readonly NumericUpDown gridAlphaInput = Num(0, 255, 160);
    private Color gridColor = Color.Red;

    // Frame numbers
    private readonly CheckBox numbersCheck = new() { Text = "Show frame numbers", AutoSize = true };

    // Animation
    private readonly CheckBox animationCheck = new() { Text = "Animation", AutoSize = true };
    private readonly TextBox frameIndicesInput = new() { Text = "0, 1, 2, 3", Width = 150 };
    private readonly CheckBox directionsCheck = new() { Text = "Has directions", AutoSize = true };
    private readonly NumericUpDown dirOffsetXInput = Num(0, 9999, 0);
    private readonly NumericUpDown dirOffsetYInput = Num(0, 9999, 0);
    private readonly ComboBox directionCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110 };
    private readonly NumericUpDown framesPerMinuteInput = Num(1, 6000, 480);
    private readonly Button playButton = new() { Text = "Pause", Width = 80, Height = 24 };

    public MainForm()
    {
        Text = "Amber Island Sprite Viewer";
        Width = 1100;
        Height = 760;
        StartPosition = FormStartPosition.CenterScreen;

        directionCombo.Items.AddRange(Enum.GetNames<Direction>());
        directionCombo.SelectedIndex = 0;

        preview.Width = 240;
        preview.Height = 160;

        var menu = BuildMenu();
        var sidePanel = BuildSidePanel();

        // Add Fill first, then docked controls, so they layout correctly.
        Controls.Add(canvas);
        Controls.Add(sidePanel);
        Controls.Add(menu);
        MainMenuStrip = menu;

        WireEvents();
        ApplyAll();
    }

    private static NumericUpDown Num(int min, int max, int value) => new()
    {
        Minimum = min,
        Maximum = max,
        Value = value,
        Width = 70
    };

    private MenuStrip BuildMenu()
    {
        var menu = new MenuStrip { Dock = DockStyle.Top };

        var fileMenu = new ToolStripMenuItem("&File");
        var openItem = new ToolStripMenuItem("&Open PNG…", null, OnOpen) { ShortcutKeys = Keys.Control | Keys.O };
        var saveSpriteItem = new ToolStripMenuItem("Save &sprite…", null, OnSaveSprite);
        var saveAnimationItem = new ToolStripMenuItem("Save &animation…", null, OnSaveAnimation);
        var exitItem = new ToolStripMenuItem("E&xit", null, (_, _) => Close());
        fileMenu.DropDownItems.Add(openItem);
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(saveSpriteItem);
        fileMenu.DropDownItems.Add(saveAnimationItem);
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(exitItem);

        menu.Items.Add(fileMenu);
        return menu;
    }

    private Panel BuildSidePanel()
    {
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            Width = 300,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(8),
            BackColor = SystemColors.Control
        };

        var openButton = new Button { Text = "Open PNG…", Width = 120, Height = 26 };
        openButton.Click += OnOpen;

        flow.Controls.Add(Group("File",
            openButton,
            fileLabel,
            Row("Zoom", zoomInput)));

        flow.Controls.Add(Group("Grid",
            gridCheck,
            Row("Frame width", frameWidthInput),
            Row("Frame height", frameHeightInput),
            Row("Line width", lineWidthInput),
            Row("Grid color", gridColorButton),
            Row("Grid alpha", gridAlphaInput)));

        flow.Controls.Add(Group("Frame numbers",
            numbersCheck));

        flow.Controls.Add(Group("Animation",
            animationCheck,
            Row("Frame indices", frameIndicesInput),
            directionsCheck,
            Row("Offset X", dirOffsetXInput),
            Row("Offset Y", dirOffsetYInput),
            Row("Direction", directionCombo),
            Row("Frames/minute", framesPerMinuteInput),
            playButton,
            LabelInfo("Preview:"),
            preview));

        return flow;
    }

    private void WireEvents()
    {
        zoomInput.ValueChanged += (_, _) => ApplyAll();
        gridCheck.CheckedChanged += (_, _) => ApplyAll();
        frameWidthInput.ValueChanged += (_, _) => ApplyAll();
        frameHeightInput.ValueChanged += (_, _) => ApplyAll();
        lineWidthInput.ValueChanged += (_, _) => ApplyAll();
        gridAlphaInput.ValueChanged += (_, _) => ApplyAll();
        numbersCheck.CheckedChanged += (_, _) => ApplyAll();

        gridColorButton.Click += (_, _) =>
        {
            using var dlg = new ColorDialog { Color = gridColor, FullOpen = true };
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                gridColor = dlg.Color;
                ApplyAll();
            }
        };

        animationCheck.CheckedChanged += (_, _) => ApplyAll();
        frameIndicesInput.TextChanged += (_, _) => ApplyAll();
        directionsCheck.CheckedChanged += (_, _) => ApplyAll();
        dirOffsetXInput.ValueChanged += (_, _) => ApplyAll();
        dirOffsetYInput.ValueChanged += (_, _) => ApplyAll();
        directionCombo.SelectedIndexChanged += (_, _) => ApplyAll();
        framesPerMinuteInput.ValueChanged += (_, _) => ApplyAll();
        playButton.Click += (_, _) =>
        {
            preview.Playing = !preview.Playing;
            playButton.Text = preview.Playing ? "Pause" : "Play";
        };
    }

    private void OnOpen(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog { Filter = "PNG images (*.png)|*.png|All files (*.*)|*.*" };
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            // Load via a copy so the file is not locked.
            using var temp = new Bitmap(dlg.FileName);
            var bmp = new Bitmap(temp);

            var previous = canvas.Image;
            preview.Sheet = null; // detach before disposing the previous sheet
            canvas.Image = bmp;
            preview.Sheet = bmp;
            previous?.Dispose();

            currentFilePath = dlg.FileName;
            fileLabel.Text = Path.GetFileName(dlg.FileName);
            preview.Restart();
            ApplyAll();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not load image:\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ApplyAll()
    {
        int fw = (int)frameWidthInput.Value;
        int fh = (int)frameHeightInput.Value;

        // Canvas
        canvas.Zoom = (int)zoomInput.Value;
        canvas.ShowGrid = gridCheck.Checked;
        canvas.GridColor = Color.FromArgb((int)gridAlphaInput.Value, gridColor);
        canvas.GridLineWidth = (float)lineWidthInput.Value;
        canvas.FrameWidth = fw;
        canvas.FrameHeight = fh;
        canvas.ShowFrameNumbers = numbersCheck.Checked;
        gridColorButton.BackColor = gridColor;
        gridColorButton.ForeColor = gridColor.GetBrightness() < 0.5f ? Color.White : Color.Black;
        canvas.Invalidate();

        // Animation enable/disable
        bool animEnabled = animationCheck.Checked;
        bool hasDir = directionsCheck.Checked;
        frameIndicesInput.Enabled = animEnabled;
        directionsCheck.Enabled = animEnabled;
        dirOffsetXInput.Enabled = animEnabled && hasDir;
        dirOffsetYInput.Enabled = animEnabled && hasDir;
        directionCombo.Enabled = animEnabled && hasDir;
        framesPerMinuteInput.Enabled = animEnabled;
        playButton.Enabled = animEnabled;

        if (animEnabled)
        {
            preview.Animation = BuildAnimation();
            preview.Direction = (Direction)directionCombo.SelectedIndex;
            preview.Zoom = (int)zoomInput.Value;
            preview.FramesPerMinute = (int)framesPerMinuteInput.Value;
            if (!preview.Playing)
            {
                preview.Playing = true;
                playButton.Text = "Pause";
            }
        }
        else
        {
            preview.Playing = false;
            playButton.Text = "Play";
            preview.Invalidate();
        }
    }

    private static uint[] ParseIndices(string text)
    {
        var result = new List<uint>();
        foreach (var part in text.Split(new[] { ',', ' ', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (uint.TryParse(part, out var value))
                result.Add(value);
        }
        return result.ToArray();
    }

    private Animation BuildAnimation()
    {
        int fw = (int)frameWidthInput.Value;
        int fh = (int)frameHeightInput.Value;

        Amber.Common.Position? offset = directionsCheck.Checked
            ? new Amber.Common.Position((int)dirOffsetXInput.Value, (int)dirOffsetYInput.Value)
            : null;

        return new Animation(
            new Amber.Common.Size(fw, fh),
            ParseIndices(frameIndicesInput.Text),
            (ushort)framesPerMinuteInput.Value,
            offset);
    }

    // ---- File menu: serialization ----

    private void OnSaveSprite(object? sender, EventArgs e)
    {
        if (canvas.Image is null)
        {
            MessageBox.Show(this, "Open a PNG first.", "Save sprite",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Sprite sprite;
        try
        {
            sprite = BuildSprite(canvas.Image);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Save sprite",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        using var dlg = new SaveFileDialog
        {
            Filter = "Sprite (*.aispr)|*.aispr|All files (*.*)|*.*",
            FileName = SuggestFileName(".aispr")
        };
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        var writer = new DataWriter();
        sprite.Write(writer);
        File.WriteAllBytes(dlg.FileName, writer.ToArray());
    }

    private void OnSaveAnimation(object? sender, EventArgs e)
    {
        var animation = BuildAnimation();

        if (animation.FrameIndices.Length == 0)
        {
            MessageBox.Show(this, "Enable \"Animation\" and enter at least one frame index first.",
                "Save animation", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new SaveFileDialog
        {
            Filter = "Animation (*.aianim)|*.aianim|All files (*.*)|*.*",
            FileName = SuggestFileName(".aianim")
        };
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        var writer = new DataWriter();
        animation.Write(writer);
        File.WriteAllBytes(dlg.FileName, writer.ToArray());
    }

    private string SuggestFileName(string extension) =>
        currentFilePath is null
            ? "untitled" + extension
            : Path.GetFileNameWithoutExtension(currentFilePath) + extension;

    /// <summary>
    /// Converts the loaded bitmap into a <see cref="Sprite"/> with an embedded
    /// palette: fully transparent pixels become color index 0, every other
    /// distinct color is added to the Colors array (referenced 1-based).
    /// </summary>
    private static Sprite BuildSprite(Bitmap bitmap)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;

        var data = bitmap.LockBits(new Rectangle(0, 0, width, height),
            ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        var bgra = new byte[width * height * 4];
        Marshal.Copy(data.Scan0, bgra, 0, bgra.Length);
        bitmap.UnlockBits(data);

        var colors = new List<ColorRgb>();
        var colorIndices = new byte[width * height];
        int offset = 0;

        for (int i = 0; i < colorIndices.Length; i++)
        {
            byte b = bgra[offset];
            byte g = bgra[offset + 1];
            byte r = bgra[offset + 2];
            byte a = bgra[offset + 3];
            offset += 4;

            if (a == 0)
            {
                colorIndices[i] = 0;
                continue;
            }

            var color = new ColorRgb(r, g, b);
            int index = colors.IndexOf(color);

            if (index == -1)
            {
                if (colors.Count >= 255)
                    throw new InvalidOperationException(
                        "The image uses more than 255 distinct colors and cannot be stored as a palette sprite.");

                colors.Add(color);
                colorIndices[i] = (byte)colors.Count; // 1-based
            }
            else
            {
                colorIndices[i] = (byte)(1 + index);
            }
        }

        return new Sprite((ushort)width, (ushort)height, colors.ToArray(), colorIndices);
    }

    // ---- Tiny layout helpers (code-only WinForms) ----

    private static Control Group(string title, params Control[] rows)
    {
        var panel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BorderStyle = BorderStyle.FixedSingle,
            MinimumSize = new Size(264, 0),
            Margin = new Padding(0, 0, 0, 10),
            Padding = new Padding(8)
        };

        panel.Controls.Add(new Label
        {
            Text = title,
            Font = new Font(SystemFonts.DefaultFont!, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 6)
        });
        panel.Controls.AddRange(rows);
        return panel;
    }

    private static Control Row(string label, Control input)
    {
        var panel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 2, 0, 2)
        };
        panel.Controls.Add(new Label
        {
            Text = label,
            Width = 90,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 4, 4, 0)
        });
        panel.Controls.Add(input);
        return panel;
    }

    private static Control LabelInfo(string text) => new Label
    {
        Text = text,
        AutoSize = true,
        Margin = new Padding(0, 6, 0, 2)
    };
}
