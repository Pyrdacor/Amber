using AmberIsland.GameData;

namespace AmberIslandTerrainEditor;

internal sealed class MainForm : Form
{
    private readonly TerrainCanvas canvas = new() { Dock = DockStyle.Fill };
    private readonly ScrollPanel canvasHost = new() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(24, 24, 26) };

    private readonly TypeRangeListView typeRangeList = new();

    private readonly NumericUpDown widthInput = new() { Minimum = 8, Maximum = 1024, Value = 128, Width = 70 };
    private readonly NumericUpDown heightInput = new() { Minimum = 8, Maximum = 1024, Value = 128, Width = 70 };
    private readonly NumericUpDown seedInput = new() { Minimum = 0, Maximum = int.MaxValue, Value = 12345, Width = 90 };
    private readonly NumericUpDown scaleInput = new() { Minimum = 0.001m, Maximum = 1m, Increment = 0.005m, DecimalPlaces = 3, Value = 0.05m, Width = 70 };
    private readonly NumericUpDown octavesInput = new() { Minimum = 1, Maximum = 8, Value = 4, Width = 60 };
    private readonly NumericUpDown persistenceInput = new() { Minimum = 0.05m, Maximum = 1m, Increment = 0.05m, DecimalPlaces = 2, Value = 0.5m, Width = 60 };
    private readonly NumericUpDown lacunarityInput = new() { Minimum = 1m, Maximum = 4m, Increment = 0.1m, DecimalPlaces = 2, Value = 2.0m, Width = 60 };
    private readonly CheckBox islandFalloffCheck = new() { Text = "Island-Falloff", AutoSize = true };
    private readonly NumericUpDown islandStrengthInput = new() { Minimum = 0m, Maximum = 1m, Increment = 0.05m, DecimalPlaces = 2, Value = 0.5m, Width = 60 };

    private readonly Label containerInfoLabel = new() { Text = "(kein Sprite-Container geladen)", AutoSize = true, MaximumSize = new Size(230, 0) };

    private readonly List<(ToolStripButton Button, TerrainZoom Zoom)> zoomButtons = [];
    private readonly List<(ToolStripButton Button, TerrainViewMode Mode)> viewModeButtons = [];

    private readonly ToolStripStatusLabel statusCell = new() { Text = "—" };
    private readonly ToolStripStatusLabel statusInfo = new() { Spring = true, TextAlign = ContentAlignment.MiddleRight };

    private readonly ToolStripMenuItem presetsMenu = new("&Presets");

    // Document state
    private List<TerrainTypeRange> ranges = [];
    private float[]? heights;
    private string? spriteContainerPath;
    private string? lastAppliedPreset;
    private readonly Dictionary<uint, byte[]> containerFiles = [];
    private readonly Dictionary<uint, Atlas> loadedAtlases = [];
    private readonly List<TerrainPreset> presets = [];

    private string? currentPath;
    private bool dirty;
    private bool populating;

    public MainForm()
    {
        Width = 1300;
        Height = 840;
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;

        LoadBuiltInPresets();

        var menu = BuildMenu();
        var toolStrip = BuildToolStrip();
        var sidePanel = BuildSidePanel();
        var statusStrip = new StatusStrip();
        statusStrip.Items.Add(statusCell);
        statusStrip.Items.Add(statusInfo);

        canvasHost.Controls.Add(canvas);

        Controls.Add(canvasHost);    // Fill
        Controls.Add(sidePanel);     // Left
        Controls.Add(statusStrip);   // Bottom
        Controls.Add(toolStrip);     // Top
        Controls.Add(menu);          // Top
        MainMenuStrip = menu;

        WireEvents();

        SetZoom(TerrainZoom.Normal);
        SetViewMode(TerrainViewMode.Textured);

        ApplyProject(TerrainProject.CreateDefault());
        currentPath = null;
        dirty = false;
        UpdateTitle();
    }

    // ---- Menu ----

    private MenuStrip BuildMenu()
    {
        var menu = new MenuStrip { Dock = DockStyle.Top };

        var fileMenu = new ToolStripMenuItem("&File");
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("&Neu…", null, (_, _) => OnNew()) { ShortcutKeys = Keys.Control | Keys.N });
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("&Öffnen…", null, (_, _) => OnOpen()) { ShortcutKeys = Keys.Control | Keys.O });
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("&Speichern", null, (_, _) => OnSave()) { ShortcutKeys = Keys.Control | Keys.S });
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("Speichern &unter…", null, (_, _) => OnSaveAs()) { ShortcutKeys = Keys.Control | Keys.Shift | Keys.S });
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("&Sprite-Container laden…", null, (_, _) => OnLoadSpriteContainer()));
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(new ToolStripMenuItem("&Beenden", null, (_, _) => Close()));

        RebuildPresetsMenu();

        var viewMenu = new ToolStripMenuItem("&View");
        viewMenu.DropDownItems.Add(new ToolStripMenuItem("Zoom &Out", null, (_, _) => SetZoom(TerrainZoom.Small)) { ShortcutKeys = Keys.Control | Keys.Subtract });
        viewMenu.DropDownItems.Add(new ToolStripMenuItem("&Normal", null, (_, _) => SetZoom(TerrainZoom.Normal)) { ShortcutKeys = Keys.Control | Keys.D0 });
        viewMenu.DropDownItems.Add(new ToolStripMenuItem("Zoom &In", null, (_, _) => SetZoom(TerrainZoom.Large)) { ShortcutKeys = Keys.Control | Keys.Add });
        viewMenu.DropDownItems.Add(new ToolStripSeparator());
        viewMenu.DropDownItems.Add(new ToolStripMenuItem("&Höhen-Ansicht", null, (_, _) => SetViewMode(TerrainViewMode.Height)));
        viewMenu.DropDownItems.Add(new ToolStripMenuItem("&Textur-Ansicht", null, (_, _) => SetViewMode(TerrainViewMode.Textured)));

        menu.Items.Add(fileMenu);
        menu.Items.Add(presetsMenu);
        menu.Items.Add(viewMenu);
        return menu;
    }

    private void RebuildPresetsMenu()
    {
        presetsMenu.DropDownItems.Clear();

        foreach (var preset in presets)
        {
            var item = new ToolStripMenuItem(preset.Name);
            item.Click += (_, _) => ApplyPreset(preset);
            presetsMenu.DropDownItems.Add(item);
        }

        presetsMenu.DropDownItems.Add(new ToolStripSeparator());
        presetsMenu.DropDownItems.Add(new ToolStripMenuItem("Aktuelle Einstellungen als Preset speichern…", null, (_, _) => OnSaveAsPreset()));
    }

    private ToolStrip BuildToolStrip()
    {
        var strip = new ToolStrip { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };

        ToolStripButton ZoomButton(string text, TerrainZoom z)
        {
            var b = new ToolStripButton(text) { CheckOnClick = false };
            b.Click += (_, _) => SetZoom(z);
            zoomButtons.Add((b, z));
            return b;
        }

        strip.Items.Add(ZoomButton("25%", TerrainZoom.Tiny));
        strip.Items.Add(ZoomButton("50%", TerrainZoom.Small));
        strip.Items.Add(ZoomButton("100%", TerrainZoom.Normal));
        strip.Items.Add(ZoomButton("200%", TerrainZoom.Large));
        strip.Items.Add(new ToolStripSeparator());

        ToolStripButton ViewButton(string text, TerrainViewMode mode)
        {
            var b = new ToolStripButton(text) { CheckOnClick = false };
            b.Click += (_, _) => SetViewMode(mode);
            viewModeButtons.Add((b, mode));
            return b;
        }

        strip.Items.Add(ViewButton("Höhe", TerrainViewMode.Height));
        strip.Items.Add(ViewButton("Textur", TerrainViewMode.Textured));
        strip.Items.Add(new ToolStripSeparator());

        var randomizeButton = new ToolStripButton("Seed zufällig") { CheckOnClick = false };
        randomizeButton.Click += (_, _) => OnRandomizeSeed();
        strip.Items.Add(randomizeButton);

        var regenerateButton = new ToolStripButton("Neu generieren") { CheckOnClick = false };
        regenerateButton.Click += (_, _) => OnRegenerate();
        strip.Items.Add(regenerateButton);

        return strip;
    }

    private Control BuildSidePanel()
    {
        var host = new ScrollPanel { Dock = DockStyle.Left, Width = 340, Padding = new Padding(8) };

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };

        var genGroup = new GroupBox { Text = "Generierung", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Width = 310 };
        var genFlow = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(6, 18, 6, 6) };

        genFlow.Controls.Add(Row("Breite", widthInput));
        genFlow.Controls.Add(Row("Höhe", heightInput));

        var seedRow = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = true, Margin = new Padding(0, 2, 0, 2) };
        seedRow.Controls.Add(new Label { Text = "Seed", Width = 90, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 4, 4, 0) });
        seedRow.Controls.Add(seedInput);
        var randomSeedButton = new Button { Text = "Zufällig", AutoSize = true, Margin = new Padding(6, 0, 0, 0) };
        randomSeedButton.Click += (_, _) => OnRandomizeSeed();
        seedRow.Controls.Add(randomSeedButton);
        genFlow.Controls.Add(seedRow);

        genFlow.Controls.Add(Row("Scale", scaleInput));
        genFlow.Controls.Add(Row("Octaves", octavesInput));
        genFlow.Controls.Add(Row("Persistence", persistenceInput));
        genFlow.Controls.Add(Row("Lacunarity", lacunarityInput));
        genFlow.Controls.Add(islandFalloffCheck);
        genFlow.Controls.Add(Row("Falloff-Stärke", islandStrengthInput));

        var regenButton = new Button { Text = "Neu generieren", AutoSize = true, Margin = new Padding(0, 6, 0, 0) };
        regenButton.Click += (_, _) => OnRegenerate();
        genFlow.Controls.Add(regenButton);

        genGroup.Controls.Add(genFlow);
        flow.Controls.Add(genGroup);

        var containerGroup = new GroupBox { Text = "Sprite-Container", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Width = 310 };
        var containerFlow = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(6, 18, 6, 6) };
        var loadContainerButton = new Button { Text = "Container laden…", AutoSize = true };
        loadContainerButton.Click += (_, _) => OnLoadSpriteContainer();
        containerFlow.Controls.Add(loadContainerButton);
        containerFlow.Controls.Add(containerInfoLabel);
        containerGroup.Controls.Add(containerFlow);
        flow.Controls.Add(containerGroup);

        var typesGroup = new GroupBox { Text = "Terrain-Typen", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Width = 310 };
        var typesFlow = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(6, 18, 6, 6) };
        typesFlow.Controls.Add(typeRangeList);
        typesGroup.Controls.Add(typesFlow);
        flow.Controls.Add(typesGroup);

        host.Controls.Add(flow);
        return host;
    }

    private void WireEvents()
    {
        typeRangeList.RangeChanged += () =>
        {
            MarkDirty();
            RecomposeOnly();
        };
        typeRangeList.TexturePickRequested += OnTexturePickRequested;

        canvas.HoverChanged += (x, y, height, type) =>
            statusCell.Text = $"Zelle ({x}, {y}) • Höhe {height * 100:0}% • {type}";
        canvas.HoverLeft += () => statusCell.Text = "—";

        foreach (var input in new NumericUpDown[] { widthInput, heightInput, seedInput, scaleInput, octavesInput, persistenceInput, lacunarityInput, islandStrengthInput })
            input.ValueChanged += (_, _) => { if (!populating) MarkDirty(); };
        islandFalloffCheck.CheckedChanged += (_, _) => { if (!populating) MarkDirty(); };

        FormClosing += (_, e) =>
        {
            if (!ConfirmDiscardChanges())
                e.Cancel = true;
        };
    }

    // ---- Generation ----

    private NoiseParameters ReadNoiseParameters() => new(
        Seed: (int)seedInput.Value,
        Scale: (float)scaleInput.Value,
        Octaves: (int)octavesInput.Value,
        Persistence: (float)persistenceInput.Value,
        Lacunarity: (float)lacunarityInput.Value,
        IslandFalloff: islandFalloffCheck.Checked,
        IslandFalloffStrength: (float)islandStrengthInput.Value);

    private void ApplyNoiseParameters(NoiseParameters noise)
    {
        populating = true;
        seedInput.Value = Math.Clamp(noise.Seed, (int)seedInput.Minimum, (int)seedInput.Maximum);
        scaleInput.Value = (decimal)Math.Clamp(noise.Scale, (float)scaleInput.Minimum, (float)scaleInput.Maximum);
        octavesInput.Value = Math.Clamp(noise.Octaves, (int)octavesInput.Minimum, (int)octavesInput.Maximum);
        persistenceInput.Value = (decimal)Math.Clamp(noise.Persistence, (float)persistenceInput.Minimum, (float)persistenceInput.Maximum);
        lacunarityInput.Value = (decimal)Math.Clamp(noise.Lacunarity, (float)lacunarityInput.Minimum, (float)lacunarityInput.Maximum);
        islandFalloffCheck.Checked = noise.IslandFalloff;
        islandStrengthInput.Value = (decimal)Math.Clamp(noise.IslandFalloffStrength, (float)islandStrengthInput.Minimum, (float)islandStrengthInput.Maximum);
        populating = false;
    }

    private void OnRegenerate()
    {
        int width = (int)widthInput.Value;
        int height = (int)heightInput.Value;
        heights = HeightmapGenerator.Generate(width, height, ReadNoiseParameters());
        canvas.Compose(heights, width, height, ranges, GetOrLoadAtlas);
        UpdateInfo();
    }

    private void RecomposeOnly()
    {
        if (heights == null)
        {
            OnRegenerate();
            return;
        }
        canvas.Compose(heights, (int)widthInput.Value, (int)heightInput.Value, ranges, GetOrLoadAtlas);
    }

    private void OnRandomizeSeed()
    {
        seedInput.Value = new Random().Next(0, int.MaxValue);
        MarkDirty();
        OnRegenerate();
    }

    private Atlas? GetOrLoadAtlas(uint spriteIndex)
    {
        if (loadedAtlases.TryGetValue(spriteIndex, out var atlas))
            return atlas;

        if (!containerFiles.TryGetValue(spriteIndex, out var bytes))
            return null;

        try
        {
            var sprite = Sprite.Read(new Amber.IO.FileFormats.Serialization.DataReader(bytes));
            atlas = Atlas.FromSprite(sprite, spriteIndex);
            loadedAtlases[spriteIndex] = atlas;
            return atlas;
        }
        catch
        {
            return null;
        }
    }

    // ---- Presets ----

    private void LoadBuiltInPresets()
    {
        presets.Clear();
        string dir = Path.Combine(AppContext.BaseDirectory, "Presets");
        if (!Directory.Exists(dir))
            return;

        foreach (var file in Directory.GetFiles(dir, "*.json").OrderBy(f => f))
        {
            try
            {
                presets.Add(TerrainPreset.Load(file));
            }
            catch
            {
                // Skip unreadable preset files.
            }
        }
    }

    private void ApplyPreset(TerrainPreset preset)
    {
        ranges = preset.ToTypeRanges(ranges);
        typeRangeList.SetRanges(ranges);
        ApplyNoiseParameters(preset.Noise);
        lastAppliedPreset = preset.Name;
        MarkDirty();
        OnRegenerate();
    }

    private void OnSaveAsPreset()
    {
        using var dialog = new Form
        {
            Text = "Preset speichern",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ClientSize = new Size(320, 110)
        };

        var label = new Label { Text = "Name des Presets:", AutoSize = true, Location = new Point(12, 12) };
        var nameBox = new TextBox { Location = new Point(12, 34), Width = 296, Text = lastAppliedPreset ?? "Mein Preset" };
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 40, Padding = new Padding(4) };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 80 };
        var cancel = new Button { Text = "Abbrechen", DialogResult = DialogResult.Cancel, Width = 80 };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);

        dialog.Controls.Add(label);
        dialog.Controls.Add(nameBox);
        dialog.Controls.Add(buttons);
        dialog.AcceptButton = ok;
        dialog.CancelButton = cancel;

        if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(nameBox.Text))
            return;

        string name = nameBox.Text.Trim();
        var preset = TerrainPreset.FromCurrent(name, ReadNoiseParameters(), ranges);

        string dir = Path.Combine(AppContext.BaseDirectory, "Presets");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, TerrainPreset.SanitizeFileName(name) + ".json");
        preset.Save(path);

        presets.RemoveAll(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        presets.Add(preset);
        RebuildPresetsMenu();
        lastAppliedPreset = name;
    }

    // ---- Sprite container ----

    private void OnLoadSpriteContainer()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Sprite-Container öffnen",
            Filter = "Amber Island container (*.aic)|*.aic|Alle Dateien (*.*)|*.*"
        };
        if (spriteContainerPath != null)
        {
            dlg.InitialDirectory = Path.GetDirectoryName(spriteContainerPath);
            dlg.FileName = Path.GetFileName(spriteContainerPath);
        }
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            var files = FileContainer.ReadAllFiles(dlg.FileName);
            if (files.Count == 0)
                throw new InvalidDataException("Der Container ist leer.");

            foreach (var atlas in loadedAtlases.Values)
                atlas.Dispose();
            loadedAtlases.Clear();

            containerFiles.Clear();
            foreach (var (index, bytes) in files)
                containerFiles[index] = bytes;

            spriteContainerPath = dlg.FileName;
            containerInfoLabel.Text = $"{Path.GetFileName(spriteContainerPath)} ({containerFiles.Count} Dateien)";
            MarkDirty();
            RecomposeOnly();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Konnte Container nicht laden:\n{ex.Message}", "Sprite-Container laden",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnTexturePickRequested(TerrainTypeRange range)
    {
        if (containerFiles.Count == 0)
        {
            MessageBox.Show(this, "Bitte zuerst einen Sprite-Container laden.", "Textur wählen",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new TexturePickerDialog(containerFiles, range.Texture);
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Result == null)
            return;

        range.Texture = dialog.Result;
        typeRangeList.RefreshTextureButton(range);
        MarkDirty();
        RecomposeOnly();
    }

    // ---- Zoom / view mode ----

    private void SetZoom(TerrainZoom zoom)
    {
        canvas.Zoom = zoom;
        foreach (var (button, z) in zoomButtons)
            button.Checked = z == zoom;
    }

    private void SetViewMode(TerrainViewMode mode)
    {
        canvas.ViewMode = mode;
        foreach (var (button, m) in viewModeButtons)
            button.Checked = m == mode;
    }

    // ---- File menu ----

    private void OnNew()
    {
        if (!ConfirmDiscardChanges())
            return;
        ApplyProject(TerrainProject.CreateDefault());
        currentPath = null;
        dirty = false;
        UpdateTitle();
    }

    private void OnOpen()
    {
        if (!ConfirmDiscardChanges())
            return;

        using var dlg = new OpenFileDialog { Filter = $"Amber Island Terrain (*{TerrainProject.Extension})|*{TerrainProject.Extension}|Alle Dateien (*.*)|*.*" };
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            var project = TerrainProject.Load(dlg.FileName);
            ApplyProject(project);
            currentPath = dlg.FileName;
            dirty = false;
            UpdateTitle();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Konnte Projekt nicht öffnen:\n{ex.Message}", "Öffnen",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ApplyProject(TerrainProject project)
    {
        populating = true;
        widthInput.Value = Math.Clamp(project.Width, (int)widthInput.Minimum, (int)widthInput.Maximum);
        heightInput.Value = Math.Clamp(project.Height, (int)heightInput.Minimum, (int)heightInput.Maximum);
        populating = false;

        ApplyNoiseParameters(project.Noise);

        ranges = project.ToTypeRanges();
        typeRangeList.SetRanges(ranges);
        lastAppliedPreset = project.LastAppliedPreset;

        foreach (var atlas in loadedAtlases.Values)
            atlas.Dispose();
        loadedAtlases.Clear();
        containerFiles.Clear();
        spriteContainerPath = null;
        containerInfoLabel.Text = "(kein Sprite-Container geladen)";

        if (!string.IsNullOrEmpty(project.SpriteContainerPath) && File.Exists(project.SpriteContainerPath))
        {
            try
            {
                var files = FileContainer.ReadAllFiles(project.SpriteContainerPath);
                foreach (var (index, bytes) in files)
                    containerFiles[index] = bytes;
                spriteContainerPath = project.SpriteContainerPath;
                containerInfoLabel.Text = $"{Path.GetFileName(spriteContainerPath)} ({containerFiles.Count} Dateien)";
            }
            catch
            {
                // Container missing/unreadable — continue without textures (placeholder colors are used).
            }
        }

        OnRegenerate();
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
            Filter = $"Amber Island Terrain (*{TerrainProject.Extension})|*{TerrainProject.Extension}|Alle Dateien (*.*)|*.*",
            FileName = currentPath == null ? "terrain" + TerrainProject.Extension : Path.GetFileName(currentPath)
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
            var project = TerrainProject.FromCurrent(
                (int)widthInput.Value, (int)heightInput.Value, ReadNoiseParameters(),
                spriteContainerPath, ranges, lastAppliedPreset);
            project.Save(path);
            dirty = false;
            UpdateTitle();
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Konnte Projekt nicht speichern:\n{ex.Message}", "Speichern",
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
        string name = currentPath == null ? "unbenannt" : Path.GetFileName(currentPath);
        Text = $"Amber Island Terrain Editor — {name}{(dirty ? " *" : "")}";
    }

    private void UpdateInfo() =>
        statusInfo.Text = $"{(int)widthInput.Value}×{(int)heightInput.Value} • Seed {(int)seedInput.Value}";

    private bool ConfirmDiscardChanges()
    {
        if (!dirty)
            return true;

        var result = MessageBox.Show(this, "Das Terrain-Projekt hat ungespeicherte Änderungen. Jetzt speichern?",
            "Ungespeicherte Änderungen", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

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
            foreach (var atlas in loadedAtlases.Values)
                atlas.Dispose();
        }
        base.Dispose(disposing);
    }

    // ---- Tiny layout helpers ----

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
}
