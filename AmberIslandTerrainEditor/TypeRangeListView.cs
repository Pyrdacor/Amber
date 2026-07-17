namespace AmberIslandTerrainEditor;

/// <summary>
/// One row per <see cref="TerrainTypeRange"/>: enabled toggle, type label, min/max height
/// (as a percentage) and a button to pick the assigned texture frame.
/// </summary>
internal sealed class TypeRangeListView : FlowLayoutPanel
{
    private readonly List<(TerrainTypeRange Range, CheckBox Enabled, NumericUpDown Min, NumericUpDown Max, Button Texture)> rows = [];

    public event Action? RangeChanged;
    public event Action<TerrainTypeRange>? TexturePickRequested;

    public TypeRangeListView()
    {
        FlowDirection = FlowDirection.TopDown;
        WrapContents = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
    }

    public void SetRanges(IReadOnlyList<TerrainTypeRange> ranges)
    {
        Controls.Clear();
        rows.Clear();

        foreach (var range in ranges)
            AddRow(range);
    }

    private void AddRow(TerrainTypeRange range)
    {
        var row = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            Margin = new Padding(0, 2, 0, 2)
        };

        var enabledCheck = new CheckBox { Checked = range.Enabled, AutoSize = true, Margin = new Padding(0, 4, 4, 0) };
        var typeLabel = new Label { Text = range.Type.ToString(), Width = 100, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 4, 4, 0) };
        var minInput = new NumericUpDown { Minimum = 0, Maximum = 100, Value = (decimal)Math.Clamp(range.MinHeight * 100, 0, 100), Width = 55, Margin = new Padding(0, 2, 2, 0) };
        var maxInput = new NumericUpDown { Minimum = 0, Maximum = 100, Value = (decimal)Math.Clamp(range.MaxHeight * 100, 0, 100), Width = 55, Margin = new Padding(0, 2, 8, 0) };
        var textureButton = new Button { AutoSize = true, Margin = new Padding(0, 1, 0, 0) };
        UpdateTextureButtonText(textureButton, range);

        enabledCheck.CheckedChanged += (_, _) =>
        {
            range.Enabled = enabledCheck.Checked;
            RangeChanged?.Invoke();
        };
        minInput.ValueChanged += (_, _) =>
        {
            range.MinHeight = (float)minInput.Value / 100f;
            RangeChanged?.Invoke();
        };
        maxInput.ValueChanged += (_, _) =>
        {
            range.MaxHeight = (float)maxInput.Value / 100f;
            RangeChanged?.Invoke();
        };
        textureButton.Click += (_, _) => TexturePickRequested?.Invoke(range);

        row.Controls.Add(enabledCheck);
        row.Controls.Add(typeLabel);
        row.Controls.Add(new Label { Text = "Min%", AutoSize = true, Margin = new Padding(0, 4, 2, 0) });
        row.Controls.Add(minInput);
        row.Controls.Add(new Label { Text = "Max%", AutoSize = true, Margin = new Padding(0, 4, 2, 0) });
        row.Controls.Add(maxInput);
        row.Controls.Add(textureButton);

        Controls.Add(row);
        rows.Add((range, enabledCheck, minInput, maxInput, textureButton));
    }

    public void RefreshTextureButton(TerrainTypeRange range)
    {
        var row = rows.FirstOrDefault(r => ReferenceEquals(r.Range, range));
        if (row.Texture != null)
            UpdateTextureButtonText(row.Texture, range);
    }

    private static void UpdateTextureButtonText(Button button, TerrainTypeRange range) =>
        button.Text = range.Texture == null
            ? "Textur wählen…"
            : $"Sprite {range.Texture.SpriteIndex} · Frame {range.Texture.AtlasFrame}";
}
