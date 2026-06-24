using Amber.IO.FileFormats.Serialization;
using AmberIsland.GameData;

namespace AmberIslandTilesetEditor;

internal sealed class MainForm : Form
{
    private readonly AtlasCanvas atlasCanvas = new() { Dock = DockStyle.Fill };

    private readonly TileListView tileList = new();
    private readonly ScrollPanel tileListHost = new() { Dock = DockStyle.Fill };

    private readonly TilePreview preview = new() { Width = 80, Height = 80, Margin = new Padding(0, 4, 0, 0) };

    private readonly ComboBox typeCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
    private readonly NumericUpDown frameCountInput = new() { Minimum = 1, Maximum = 255, Value = 1, Width = 80 };
    private readonly NumericUpDown imageIndexInput = new() { Minimum = 0, Maximum = 65535, Value = 0, Width = 80 };
    private readonly List<(CheckBox Check, TileFlags Flag)> flagChecks = [];
    private readonly CheckBox[] travelChecks = new CheckBox[Enum.GetValues<TravelType>().Length];
    private FlowLayoutPanel propsPanel = null!;

    private readonly Label atlasInfo = new() { Text = "(no atlas loaded)", AutoSize = true, MaximumSize = new Size(340, 0) };

    private readonly System.Windows.Forms.Timer animationTimer = new() { Interval = 125 };
    private int animationStep;

    // Document state
    private List<Tile> tiles = [];
    private Atlas? atlas;
    private uint graphicAtlasIndex;
    private int selectedIndex = -1;

    private string? currentPath;
    private bool dirty;
    private bool populating;
    private string? lastAtlasPath;

    public MainForm()
    {
        Width = 1180;
        Height = 820;
        StartPosition = FormStartPosition.CenterScreen;

        var menu = BuildMenu();
        var sidePanel = BuildSidePanel();

        Controls.Add(atlasCanvas);
        Controls.Add(sidePanel);
        Controls.Add(menu);
        MainMenuStrip = menu;

        WireEvents();

        animationTimer.Tick += (_, _) =>
        {
            animationStep++;
            tileList.AnimationStep = animationStep;
            preview.AnimationStep = animationStep;
        };
        animationTimer.Start();

        NewTileset();
        dirty = false;
        UpdateTitle();
    }

    // ---- Menu ----

    private MenuStrip BuildMenu()
    {
        var menu = new MenuStrip { Dock = DockStyle.Top };

        var fileMenu = new ToolStripMenuItem("&File");
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("&New", null, (_, _) => OnNew()) { ShortcutKeys = Keys.Control | Keys.N });
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("&Open…", null, (_, _) => OnOpen()) { ShortcutKeys = Keys.Control | Keys.O });
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("&Save", null, (_, _) => OnSave()) { ShortcutKeys = Keys.Control | Keys.S });
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("Save &As…", null, (_, _) => OnSaveAs()) { ShortcutKeys = Keys.Control | Keys.Shift | Keys.S });
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("&Open atlas…", null, (_, _) => OnOpenAtlas()));
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("E&xit", null, (_, _) => Close()));

        menu.Items.Add(fileMenu);
        return menu;
    }

    // ---- Side panel ----

    private Panel BuildSidePanel()
    {
        var panel = new Panel { Dock = DockStyle.Right, Width = 380, Padding = new Padding(8) };

        panel.Controls.Add(tileListHost); // Fill (added first)
        panel.Controls.Add(BuildAtlasPanel()); // Top
        panel.Controls.Add(BuildPropsPanel()); // Bottom

        tileListHost.Controls.Add(tileList);
        return panel;
    }

    private Control BuildAtlasPanel()
    {
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };

        flow.Controls.Add(Bold("Atlas"));

        var buttonRow = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = true };
        var openAtlasButton = new Button { Text = "Open atlas…", AutoSize = true };
        openAtlasButton.Click += (_, _) => OnOpenAtlas();
        var zoomOut = new Button { Text = "−", Width = 30 };
        zoomOut.Click += (_, _) => atlasCanvas.Zoom--;
        var zoomIn = new Button { Text = "+", Width = 30 };
        zoomIn.Click += (_, _) => atlasCanvas.Zoom++;
        buttonRow.Controls.Add(openAtlasButton);
        buttonRow.Controls.Add(zoomOut);
        buttonRow.Controls.Add(zoomIn);
        flow.Controls.Add(buttonRow);

        var gridCheck = new CheckBox { Text = "Show grid", Checked = true, AutoSize = true };
        gridCheck.CheckedChanged += (_, _) => { atlasCanvas.ShowGrid = gridCheck.Checked; atlasCanvas.Invalidate(); };
        var indexCheck = new CheckBox { Text = "Show tile indices (0-based)", Checked = true, AutoSize = true };
        indexCheck.CheckedChanged += (_, _) => { atlasCanvas.ShowIndices = indexCheck.Checked; atlasCanvas.Invalidate(); };
        flow.Controls.Add(gridCheck);
        flow.Controls.Add(indexCheck);
        flow.Controls.Add(atlasInfo);

        var tilesHeader = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = true, Margin = new Padding(0, 8, 0, 0) };
        tilesHeader.Controls.Add(Bold("Tiles"));
        var addButton = new Button { Text = "Add", AutoSize = true, Margin = new Padding(16, 4, 4, 0) };
        addButton.Click += (_, _) => OnAddTile();
        var removeButton = new Button { Text = "Remove", AutoSize = true, Margin = new Padding(0, 4, 0, 0) };
        removeButton.Click += (_, _) => OnRemoveTile();
        tilesHeader.Controls.Add(addButton);
        tilesHeader.Controls.Add(removeButton);
        flow.Controls.Add(tilesHeader);

        return flow;
    }

    private Control BuildPropsPanel()
    {
        propsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Enabled = false,
            Padding = new Padding(0, 8, 0, 0)
        };

        propsPanel.Controls.Add(Bold("Tile properties"));

        typeCombo.Items.AddRange(Enum.GetNames<TileType>());
        propsPanel.Controls.Add(Row("Type", typeCombo));

        // Flags
        var flagsGroup = new GroupBox { Text = "Flags", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        var flagsFlow = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(6, 18, 6, 6) };
        foreach (var flag in Enum.GetValues<TileFlags>())
        {
            if (flag == TileFlags.None)
                continue;
            var check = new CheckBox { Text = SplitPascal(flag.ToString()), AutoSize = true };
            check.CheckedChanged += (_, _) => ApplyControlsToSelectedTile();
            flagChecks.Add((check, flag));
            flagsFlow.Controls.Add(check);
        }
        flagsGroup.Controls.Add(flagsFlow);
        propsPanel.Controls.Add(flagsGroup);

        propsPanel.Controls.Add(Row("Frame count", frameCountInput));

        // Blocked travel
        var travelGroup = new GroupBox { Text = "Blocked travel types", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        var travelFlow = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(6, 18, 6, 6) };
        foreach (var travel in Enum.GetValues<TravelType>())
        {
            var check = new CheckBox { Text = travel.ToString(), AutoSize = true };
            check.CheckedChanged += (_, _) => ApplyControlsToSelectedTile();
            travelChecks[(int)travel] = check;
            travelFlow.Controls.Add(check);
        }
        travelGroup.Controls.Add(travelFlow);
        propsPanel.Controls.Add(travelGroup);

        propsPanel.Controls.Add(Row("Image index", imageIndexInput));

        propsPanel.Controls.Add(new Label { Text = "Preview:", AutoSize = true, Margin = new Padding(0, 6, 0, 0) });
        propsPanel.Controls.Add(preview);

        return propsPanel;
    }

    private void WireEvents()
    {
        tileList.SelectedIndexChanged += index =>
        {
            selectedIndex = index;
            PopulateControls();
        };

        tileListHost.Resize += (_, _) =>
        {
            tileList.Width = tileListHost.ClientSize.Width;
            tileList.Relayout();
        };

        atlasCanvas.FrameClicked += frame =>
        {
            if (selectedIndex >= 0)
                imageIndexInput.Value = Math.Min(frame, (int)imageIndexInput.Maximum);
        };

        typeCombo.SelectedIndexChanged += (_, _) => ApplyControlsToSelectedTile();
        frameCountInput.ValueChanged += (_, _) => ApplyControlsToSelectedTile();
        imageIndexInput.ValueChanged += (_, _) => ApplyControlsToSelectedTile();

        FormClosing += (_, e) =>
        {
            if (!ConfirmDiscardChanges())
                e.Cancel = true;
        };
    }

    // ---- Tile selection / editing ----

    private void PopulateControls()
    {
        bool has = selectedIndex >= 0 && selectedIndex < tiles.Count;
        propsPanel.Enabled = has;

        if (!has)
        {
            preview.SetTile(null, atlas);
            atlasCanvas.HighlightFrame = -1;
            return;
        }

        populating = true;
        var tile = tiles[selectedIndex];
        typeCombo.SelectedIndex = (int)tile.Type;
        foreach (var (check, flag) in flagChecks)
            check.Checked = (tile.Flags & flag) != 0;
        frameCountInput.Value = Math.Clamp((int)tile.FrameCount, 1, 255);
        for (int i = 0; i < travelChecks.Length; i++)
            travelChecks[i].Checked = (tile.BlockedTravel & (1 << i)) != 0;
        imageIndexInput.Value = Math.Min(tile.ImageIndex, (int)imageIndexInput.Maximum);
        populating = false;

        preview.SetTile(tile, atlas);
        atlasCanvas.HighlightFrame = tile.ImageIndex;
    }

    private void ApplyControlsToSelectedTile()
    {
        if (populating || selectedIndex < 0 || selectedIndex >= tiles.Count)
            return;

        var flags = TileFlags.None;
        foreach (var (check, flag) in flagChecks)
            if (check.Checked)
                flags |= flag;

        byte travel = 0;
        for (int i = 0; i < travelChecks.Length; i++)
            if (travelChecks[i].Checked)
                travel |= (byte)(1 << i);

        var tile = new Tile(
            (TileType)typeCombo.SelectedIndex,
            flags,
            (byte)frameCountInput.Value,
            travel,
            (ushort)imageIndexInput.Value);

        tiles[selectedIndex] = tile;
        tileList.RefreshTiles();
        preview.SetTile(tile, atlas);
        atlasCanvas.HighlightFrame = tile.ImageIndex;
        MarkDirty();
    }

    private void OnAddTile()
    {
        tiles.Add(new Tile(TileType.Grass, TileFlags.None, 1, 0, 0));
        selectedIndex = tiles.Count - 1;
        tileList.SetData(tiles, atlas);
        tileList.SelectedIndex = selectedIndex;
        PopulateControls();
        MarkDirty();
    }

    private void OnRemoveTile()
    {
        if (selectedIndex < 0 || selectedIndex >= tiles.Count)
            return;
        tiles.RemoveAt(selectedIndex);
        if (selectedIndex >= tiles.Count)
            selectedIndex = tiles.Count - 1;
        tileList.SetData(tiles, atlas);
        tileList.SelectedIndex = selectedIndex;
        PopulateControls();
        MarkDirty();
    }

    // ---- Atlas ----

    private void OnOpenAtlas()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Open tileset atlas container",
            Filter = "Amber Island container (*.aic)|*.aic|All files (*.*)|*.*"
        };
        if (lastAtlasPath != null)
        {
            dlg.InitialDirectory = Path.GetDirectoryName(lastAtlasPath);
            dlg.FileName = Path.GetFileName(lastAtlasPath);
        }
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            Dictionary<uint, byte[]> files;
            using (var stream = File.OpenRead(dlg.FileName))
                files = FileContainer.ReadAllFiles(stream);

            if (files.Count == 0)
                throw new InvalidDataException("The atlas container is empty.");

            var indices = files.Keys.OrderBy(k => k).ToList();
            uint chosen = indices[0];
            if (indices.Count > 1 && !ShowIndexChooser(indices, graphicAtlasIndex, out chosen))
                return;

            var sprite = Sprite.Read(new DataReader(files[chosen]));
            var newAtlas = Atlas.FromSprite(sprite, chosen);

            atlas?.Dispose();
            atlas = newAtlas;
            lastAtlasPath = dlg.FileName;

            if (graphicAtlasIndex != chosen)
            {
                graphicAtlasIndex = chosen;
                MarkDirty();
            }

            atlasCanvas.Atlas = atlas;
            tileList.SetData(tiles, atlas);
            UpdateAtlasInfo();
            PopulateControls();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not open atlas:\n{ex.Message}", "Open atlas",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UpdateAtlasInfo() =>
        atlasInfo.Text = atlas == null
            ? "(no atlas loaded)"
            : $"Atlas #{atlas.Index}: {atlas.Bitmap.Width}×{atlas.Bitmap.Height}, {atlas.FrameCount} frames";

    private bool ShowIndexChooser(List<uint> indices, uint preferred, out uint chosen)
    {
        chosen = indices[0];

        using var dialog = new Form
        {
            Text = "Select atlas (file index)",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ClientSize = new Size(240, 280)
        };

        var listBox = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
        foreach (var index in indices)
            listBox.Items.Add($"File index {index}");
        listBox.SelectedIndex = Math.Max(0, indices.IndexOf(preferred));

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 40, Padding = new Padding(4) };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 80 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 80 };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);

        dialog.Controls.Add(listBox);
        dialog.Controls.Add(buttons);
        dialog.AcceptButton = ok;
        dialog.CancelButton = cancel;

        if (dialog.ShowDialog(this) != DialogResult.OK || listBox.SelectedIndex < 0)
            return false;

        chosen = indices[listBox.SelectedIndex];
        return true;
    }

    // ---- File menu ----

    private void OnNew()
    {
        if (!ConfirmDiscardChanges())
            return;
        NewTileset();
        currentPath = null;
        dirty = false;
        UpdateTitle();
    }

    private void NewTileset()
    {
        tiles = [];
        selectedIndex = -1;
        graphicAtlasIndex = atlas?.Index ?? 0;
        tileList.SetData(tiles, atlas);
        tileList.SelectedIndex = -1;
        PopulateControls();
        UpdateAtlasInfo();
    }

    private void OnOpen()
    {
        if (!ConfirmDiscardChanges())
            return;

        using var dlg = new OpenFileDialog { Filter = "Amber Island tileset (*.aitileset)|*.aitileset|All files (*.*)|*.*" };
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            var tileset = Tileset.Read(new DataReader(File.ReadAllBytes(dlg.FileName)));
            tiles = [.. tileset.Tiles];
            graphicAtlasIndex = tileset.GraphicAtlasIndex;
            selectedIndex = tiles.Count > 0 ? 0 : -1;
            currentPath = dlg.FileName;
            dirty = false;

            tileList.SetData(tiles, atlas);
            tileList.SelectedIndex = selectedIndex;
            PopulateControls();
            UpdateAtlasInfo();
            UpdateTitle();

            if (atlas == null || atlas.Index != graphicAtlasIndex)
                MessageBox.Show(this,
                    $"This tileset references atlas file index {graphicAtlasIndex}. Use \"Open atlas…\" to load it for previews.",
                    "Open tileset", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not open tileset:\n{ex.Message}", "Open",
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
            Filter = "Amber Island tileset (*.aitileset)|*.aitileset|All files (*.*)|*.*",
            FileName = currentPath == null ? "tileset.aitileset" : Path.GetFileName(currentPath)
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
            new Tileset(graphicAtlasIndex, [.. tiles]).Write(writer);
            File.WriteAllBytes(path, writer.ToArray());
            dirty = false;
            UpdateTitle();
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not save tileset:\n{ex.Message}", "Save",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    // ---- State helpers ----

    private void MarkDirty()
    {
        if (dirty)
            return;
        dirty = true;
        UpdateTitle();
    }

    private void UpdateTitle()
    {
        string name = currentPath == null ? "untitled" : Path.GetFileName(currentPath);
        Text = $"Amber Island Tileset Editor — {name}{(dirty ? " *" : "")}  ({tiles.Count} tiles)";
    }

    private bool ConfirmDiscardChanges()
    {
        if (!dirty)
            return true;

        var result = MessageBox.Show(this, "The tileset has unsaved changes. Save them now?",
            "Unsaved changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

        return result switch
        {
            DialogResult.Yes => OnSave(),
            DialogResult.No => true,
            _ => false
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            animationTimer.Dispose();
            atlas?.Dispose();
        }
        base.Dispose(disposing);
    }

    // ---- Tiny layout helpers ----

    private static Label Bold(string text) => new()
    {
        Text = text,
        Font = new Font(SystemFonts.DefaultFont!, FontStyle.Bold),
        AutoSize = true,
        Margin = new Padding(0, 4, 0, 4)
    };

    private static Control Row(string label, Control input)
    {
        var panel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            Margin = new Padding(0, 2, 0, 2)
        };
        panel.Controls.Add(new Label { Text = label, Width = 90, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 4, 4, 0) });
        panel.Controls.Add(input);
        return panel;
    }

    private static string SplitPascal(string text)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < text.Length; i++)
        {
            if (i > 0 && char.IsUpper(text[i]) && !char.IsUpper(text[i - 1]))
                sb.Append(' ');
            sb.Append(text[i]);
        }
        return sb.ToString();
    }
}
