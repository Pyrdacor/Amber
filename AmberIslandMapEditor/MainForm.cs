using Amber.IO.FileFormats.Serialization;
using AmberIsland.GameData;

namespace AmberIslandMapEditor;

internal sealed class MainForm : Form
{
    private static readonly string[] LayerNames = ["Background", "Objects", "Foreground"];

    private readonly MapCanvas canvas = new();
    private readonly ScrollPanel mapHost = new() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(40, 40, 44) };

    private readonly TilesetView tilesetView = new();
    private readonly ScrollPanel tilesetHost = new() { Dock = DockStyle.Fill };

    private readonly RadioButton[] layerRadios = new RadioButton[MapCanvas.LayerCount];
    private readonly CheckBox[] layerVisibleChecks = new CheckBox[MapCanvas.LayerCount];
    private readonly NumericUpDown tilesetIndexInput = new() { Minimum = 0, Maximum = 65535, Value = 1, Width = 80 };
    private readonly Label tilesetInfoLabel = new() { Text = "(no tileset loaded)", AutoSize = true, MaximumSize = new Size(250, 0) };

    private readonly int[] selectedPerLayer = new int[MapCanvas.LayerCount];

    private readonly List<(ToolStripButton Button, MapTool Tool)> toolButtons = [];
    private readonly List<(ToolStripButton Button, MapZoom Zoom)> zoomButtons = [];

    private readonly ToolStripStatusLabel statusCell = new() { Text = "—" };
    private readonly ToolStripStatusLabel statusInfo = new() { Spring = true, TextAlign = ContentAlignment.MiddleRight };

    private string? currentPath;
    private bool dirty;
    private bool suppressIndexEvent;

    private string? lastTilesetPath;
    private string? lastAtlasPath;

    public MainForm()
    {
        Width = 1200;
        Height = 800;
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;

        var menu = BuildMenu();
        var toolStrip = BuildToolStrip();
        var sidePanel = BuildSidePanel();
        var statusStrip = new StatusStrip();
        statusStrip.Items.Add(statusCell);
        statusStrip.Items.Add(statusInfo);

        mapHost.Controls.Add(canvas);

        Controls.Add(mapHost);
        Controls.Add(sidePanel);
        Controls.Add(statusStrip);
        Controls.Add(toolStrip);
        Controls.Add(menu);
        MainMenuStrip = menu;

        WireEvents();

        SetTool(MapTool.Pen);
        SetZoom(MapZoom.Normal);

        canvas.NewMap(32, 32);
        SyncActiveLayer();
        dirty = false;
        UpdateTitle();
        UpdateInfo();
    }

    // ---- Menu / toolbar ----

    private MenuStrip BuildMenu()
    {
        var menu = new MenuStrip { Dock = DockStyle.Top };

        var fileMenu = new ToolStripMenuItem("&File");
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("&New…", null, (_, _) => OnNew()) { ShortcutKeys = Keys.Control | Keys.N });
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("&Open…", null, (_, _) => OnOpen()) { ShortcutKeys = Keys.Control | Keys.O });
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("&Save", null, (_, _) => OnSave()) { ShortcutKeys = Keys.Control | Keys.S });
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("Save &As…", null, (_, _) => OnSaveAs()) { ShortcutKeys = Keys.Control | Keys.Shift | Keys.S });
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("E&xit", null, (_, _) => Close()));

        var viewMenu = new ToolStripMenuItem("&View");
        viewMenu.DropDownItems.Add(new ToolStripMenuItem("Zoom &Out", null, (_, _) => SetZoom(MapZoom.Out)) { ShortcutKeys = Keys.Control | Keys.Subtract });
        viewMenu.DropDownItems.Add(new ToolStripMenuItem("&Normal zoom", null, (_, _) => SetZoom(MapZoom.Normal)) { ShortcutKeys = Keys.Control | Keys.D0 });
        viewMenu.DropDownItems.Add(new ToolStripMenuItem("Zoom &In", null, (_, _) => SetZoom(MapZoom.In)) { ShortcutKeys = Keys.Control | Keys.Add });

        menu.Items.Add(fileMenu);
        menu.Items.Add(viewMenu);
        return menu;
    }

    private ToolStrip BuildToolStrip()
    {
        var strip = new ToolStrip { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };

        ToolStripButton ZoomButton(string text, MapZoom z)
        {
            var b = new ToolStripButton(text) { CheckOnClick = false };
            b.Click += (_, _) => SetZoom(z);
            zoomButtons.Add((b, z));
            return b;
        }

        strip.Items.Add(ZoomButton("Zoom Out", MapZoom.Out));
        strip.Items.Add(ZoomButton("Normal", MapZoom.Normal));
        strip.Items.Add(ZoomButton("Zoom In", MapZoom.In));
        strip.Items.Add(new ToolStripSeparator());

        ToolStripButton ToolButton(string text, MapTool t)
        {
            var b = new ToolStripButton(text) { CheckOnClick = false };
            b.Click += (_, _) => SetTool(t);
            toolButtons.Add((b, t));
            return b;
        }

        strip.Items.Add(ToolButton("Pen", MapTool.Pen));
        strip.Items.Add(ToolButton("Block 2×2", MapTool.Block2x2));
        strip.Items.Add(ToolButton("Block 2×3", MapTool.Block2x3));
        strip.Items.Add(ToolButton("Block 3×2", MapTool.Block3x2));
        strip.Items.Add(ToolButton("Block 3×3", MapTool.Block3x3));
        strip.Items.Add(ToolButton("Fill", MapTool.Fill));
        strip.Items.Add(new ToolStripSeparator());

        var gridButton = new ToolStripButton("Grid") { CheckOnClick = true, Checked = true };
        gridButton.CheckedChanged += (_, _) =>
        {
            canvas.ShowGrid = gridButton.Checked;
            canvas.Invalidate();
        };
        strip.Items.Add(gridButton);

        return strip;
    }

    private Panel BuildSidePanel()
    {
        var panel = new Panel { Dock = DockStyle.Left, Width = 300, Padding = new Padding(8) };

        var top = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };

        top.Controls.Add(Bold("Layers"));

        for (int i = 0; i < MapCanvas.LayerCount; i++)
        {
            int layer = i;

            var row = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Margin = new Padding(0, 2, 0, 2)
            };

            var radio = new RadioButton { Text = LayerNames[i], AutoSize = true, Checked = i == 0, Width = 150 };
            radio.CheckedChanged += (_, _) =>
            {
                if (radio.Checked)
                    SyncActiveLayer();
            };
            layerRadios[i] = radio;

            var visible = new CheckBox { Text = "visible", AutoSize = true, Checked = true };
            visible.CheckedChanged += (_, _) => canvas.SetLayerVisible(layer, visible.Checked);
            layerVisibleChecks[i] = visible;

            row.Controls.Add(radio);
            row.Controls.Add(visible);
            top.Controls.Add(row);
        }

        var loadButton = new Button { Text = "Load tileset for active layer…", AutoSize = true, Margin = new Padding(0, 8, 0, 4) };
        loadButton.Click += (_, _) => OnLoadTileset();
        top.Controls.Add(loadButton);

        var indexRow = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = true };
        indexRow.Controls.Add(new Label { Text = "Tileset index", AutoSize = true, Margin = new Padding(0, 6, 6, 0) });
        indexRow.Controls.Add(tilesetIndexInput);
        top.Controls.Add(indexRow);

        top.Controls.Add(tilesetInfoLabel);
        top.Controls.Add(Bold("Tiles"));

        var tilesetHeader = new Label
        {
            Dock = DockStyle.Top,
            Height = 2,
            Margin = new Padding(0)
        };

        tilesetHost.Controls.Add(tilesetView);

        panel.Controls.Add(tilesetHost);
        panel.Controls.Add(tilesetHeader);
        panel.Controls.Add(top);
        return panel;
    }

    private static Label Bold(string text) => new()
    {
        Text = text,
        Font = new Font(SystemFonts.DefaultFont!, FontStyle.Bold),
        AutoSize = true,
        Margin = new Padding(0, 8, 0, 4)
    };

    private void WireEvents()
    {
        canvas.MapChanged += () =>
        {
            if (!dirty)
            {
                dirty = true;
                UpdateTitle();
            }
        };
        canvas.TilePicked += value =>
        {
            selectedPerLayer[canvas.ActiveLayer] = value;
            tilesetView.SelectedValue = value;
        };
        canvas.HoverChanged += UpdateHoverStatus;

        tilesetView.SelectedChanged += value =>
        {
            selectedPerLayer[canvas.ActiveLayer] = value;
            canvas.SelectedTile = value;
        };

        tilesetHost.Resize += (_, _) =>
        {
            tilesetView.Width = tilesetHost.ClientSize.Width;
            tilesetView.Relayout();
        };

        tilesetIndexInput.ValueChanged += (_, _) =>
        {
            if (suppressIndexEvent)
                return;
            canvas.SetTilesetIndex(canvas.ActiveLayer, (ushort)tilesetIndexInput.Value);
            if (!dirty)
            {
                dirty = true;
                UpdateTitle();
            }
        };

        FormClosing += (_, e) =>
        {
            if (!ConfirmDiscardChanges())
                e.Cancel = true;
        };
    }

    // ---- Active layer / tool / zoom state ----

    private void SyncActiveLayer()
    {
        int active = 0;
        for (int i = 0; i < layerRadios.Length; i++)
        {
            if (layerRadios[i].Checked)
                active = i;
        }

        canvas.ActiveLayer = active;

        int selected = selectedPerLayer[active];
        canvas.SelectedTile = selected;

        tilesetView.Width = tilesetHost.ClientSize.Width;
        tilesetView.SetTileset(canvas.GetLayerTileset(active));
        tilesetView.SelectedValue = selected;

        suppressIndexEvent = true;
        tilesetIndexInput.Value = canvas.GetTilesetIndex(active);
        suppressIndexEvent = false;

        UpdateTilesetInfo();
    }

    private void SetTool(MapTool tool)
    {
        canvas.Tool = tool;
        foreach (var (button, t) in toolButtons)
            button.Checked = t == tool;
        canvas.Invalidate();
    }

    private void SetZoom(MapZoom zoom)
    {
        canvas.Zoom = zoom;
        foreach (var (button, z) in zoomButtons)
            button.Checked = z == zoom;
    }

    private void UpdateTilesetInfo()
    {
        var ts = canvas.GetLayerTileset(canvas.ActiveLayer);
        tilesetInfoLabel.Text = ts == null
            ? "(no tileset loaded)"
            : $"{ts.TileCount} tiles, atlas {ts.Atlas.Width}×{ts.Atlas.Height}";
    }

    private void UpdateHoverStatus(Point cell)
    {
        if (cell.X < 0)
        {
            statusCell.Text = "—";
            return;
        }
        int value = canvas.GetTileValue(canvas.ActiveLayer, cell.X, cell.Y);
        statusCell.Text = $"Cell ({cell.X}, {cell.Y})  •  {LayerNames[canvas.ActiveLayer]} tile = {value}";
    }

    private void UpdateInfo() =>
        statusInfo.Text = $"Map {canvas.MapWidth}×{canvas.MapHeight}";

    // ---- Tileset loading ----

    private void OnLoadTileset()
    {
        string? tilesetPath = AskFile("Select tileset data container (tile definitions)", lastTilesetPath);
        if (tilesetPath == null)
            return;

        string? atlasPath = AskFile("Select tileset graphic atlas container", lastAtlasPath ?? tilesetPath);
        if (atlasPath == null)
            return;

        try
        {
            var tileset = LayerTileset.Load(tilesetPath, atlasPath);
            canvas.SetLayerTileset(canvas.ActiveLayer, tileset);
            lastTilesetPath = tilesetPath;
            lastAtlasPath = atlasPath;

            tilesetView.Width = tilesetHost.ClientSize.Width;
            tilesetView.SetTileset(tileset);
            if (selectedPerLayer[canvas.ActiveLayer] > tileset.TileCount)
            {
                selectedPerLayer[canvas.ActiveLayer] = 0;
                canvas.SelectedTile = 0;
                tilesetView.SelectedValue = 0;
            }
            UpdateTilesetInfo();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not load tileset:\n{ex.Message}", "Load tileset",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private string? AskFile(string title, string? initial)
    {
        using var dlg = new OpenFileDialog
        {
            Title = title,
            Filter = "Amber Island container (*.aic)|*.aic|All files (*.*)|*.*"
        };
        if (initial != null)
        {
            dlg.InitialDirectory = Path.GetDirectoryName(initial);
            dlg.FileName = Path.GetFileName(initial);
        }
        return dlg.ShowDialog(this) == DialogResult.OK ? dlg.FileName : null;
    }

    // ---- File menu ----

    private void OnNew()
    {
        if (!ConfirmDiscardChanges())
            return;

        if (!ShowNewMapDialog(out int width, out int height))
            return;

        canvas.NewMap(width, height);
        for (int i = 0; i < selectedPerLayer.Length; i++)
            selectedPerLayer[i] = 0;
        currentPath = null;
        dirty = false;
        SyncActiveLayer();
        UpdateTitle();
        UpdateInfo();
    }

    private void OnOpen()
    {
        if (!ConfirmDiscardChanges())
            return;

        using var dlg = new OpenFileDialog { Filter = "Amber Island map (*.aimap)|*.aimap|All files (*.*)|*.*" };
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            var map = Map.Read(new DataReader(File.ReadAllBytes(dlg.FileName)));
            canvas.LoadMap(map);
            for (int i = 0; i < selectedPerLayer.Length; i++)
                selectedPerLayer[i] = 0;
            currentPath = dlg.FileName;
            dirty = false;
            SyncActiveLayer();
            UpdateTitle();
            UpdateInfo();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not open map:\n{ex.Message}", "Open",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private bool OnSave()
    {
        if (currentPath == null)
            return OnSaveAs();
        return SaveTo(currentPath);
    }

    private bool OnSaveAs()
    {
        using var dlg = new SaveFileDialog
        {
            Filter = "Amber Island map (*.aimap)|*.aimap|All files (*.*)|*.*",
            FileName = currentPath == null ? "map.aimap" : Path.GetFileName(currentPath)
        };
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return false;

        if (!SaveTo(dlg.FileName))
            return false;

        currentPath = dlg.FileName;
        UpdateTitle();
        return true;
    }

    private bool SaveTo(string path)
    {
        try
        {
            var writer = new DataWriter();
            canvas.ToMap().Write(writer);
            File.WriteAllBytes(path, writer.ToArray());
            dirty = false;
            UpdateTitle();
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not save map:\n{ex.Message}", "Save",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private void UpdateTitle()
    {
        string name = currentPath == null ? "untitled" : Path.GetFileName(currentPath);
        Text = $"Amber Island Map Editor — {name}{(dirty ? " *" : "")}";
    }

    private bool ConfirmDiscardChanges()
    {
        if (!dirty)
            return true;

        var result = MessageBox.Show(this, "The map has unsaved changes. Save them now?",
            "Unsaved changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

        return result switch
        {
            DialogResult.Yes => OnSave(),
            DialogResult.No => true,
            _ => false
        };
    }

    private bool ShowNewMapDialog(out int width, out int height)
    {
        width = 32;
        height = 32;

        using var dialog = new Form
        {
            Text = "New map",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ClientSize = new Size(240, 130)
        };

        var widthInput = new NumericUpDown { Minimum = 1, Maximum = 255, Value = 32, Left = 110, Top = 15, Width = 100 };
        var heightInput = new NumericUpDown { Minimum = 1, Maximum = 255, Value = 32, Left = 110, Top = 45, Width = 100 };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Left = 50, Top = 85, Width = 80 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 140, Top = 85, Width = 80 };

        dialog.Controls.Add(new Label { Text = "Width (tiles)", Left = 12, Top = 17, AutoSize = true });
        dialog.Controls.Add(new Label { Text = "Height (tiles)", Left = 12, Top = 47, AutoSize = true });
        dialog.Controls.Add(widthInput);
        dialog.Controls.Add(heightInput);
        dialog.Controls.Add(ok);
        dialog.Controls.Add(cancel);
        dialog.AcceptButton = ok;
        dialog.CancelButton = cancel;

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return false;

        width = (int)widthInput.Value;
        height = (int)heightInput.Value;
        return true;
    }
}
