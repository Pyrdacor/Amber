using Amber.IO.FileFormats.Serialization;
using AmberIsland.GameData;

namespace AmberIslandTerrainEditor;

/// <summary>
/// Modal dialog to pick a texture frame for a terrain type: choose a sprite (file index)
/// from the currently loaded sprite container, then click a 16×16 frame in its atlas.
/// </summary>
internal sealed class TexturePickerDialog : Form
{
    private readonly IReadOnlyDictionary<uint, byte[]> containerFiles;
    private readonly ComboBox spriteCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top };
    private readonly AtlasCanvas canvas = new() { Dock = DockStyle.Fill };
    private readonly Label infoLabel = new() { Dock = DockStyle.Top, AutoSize = false, Height = 20, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Button okButton = new() { Text = "OK", DialogResult = DialogResult.OK, Width = 80, Enabled = false };

    private Atlas? atlas;
    private int selectedFrame = -1;

    public TerrainTextureRef? Result { get; private set; }

    public TexturePickerDialog(IReadOnlyDictionary<uint, byte[]> containerFiles, TerrainTextureRef? current)
    {
        this.containerFiles = containerFiles;

        Text = "Textur wählen";
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(520, 520);
        MinimumSize = new Size(360, 360);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 40, Padding = new Padding(4) };
        var cancel = new Button { Text = "Abbrechen", DialogResult = DialogResult.Cancel, Width = 80 };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(okButton);

        Controls.Add(canvas);      // Fill
        Controls.Add(infoLabel);   // Top
        Controls.Add(spriteCombo); // Top
        Controls.Add(buttons);     // Bottom
        AcceptButton = okButton;
        CancelButton = cancel;

        var indices = containerFiles.Keys.OrderBy(k => k).ToList();
        foreach (var index in indices)
            spriteCombo.Items.Add($"Datei-Index {index}");

        spriteCombo.SelectedIndexChanged += (_, _) =>
        {
            if (spriteCombo.SelectedIndex < 0)
                return;
            LoadSprite(indices[spriteCombo.SelectedIndex]);
        };

        canvas.FrameClicked += frame =>
        {
            selectedFrame = frame;
            canvas.HighlightFrame = frame;
            okButton.Enabled = true;
        };

        FormClosed += (_, _) => atlas?.Dispose();

        int preferredIndexPos = current != null ? indices.IndexOf(current.SpriteIndex) : -1;
        spriteCombo.SelectedIndex = preferredIndexPos >= 0 ? preferredIndexPos : (indices.Count > 0 ? 0 : -1);

        if (current != null && preferredIndexPos >= 0)
        {
            selectedFrame = current.AtlasFrame;
            canvas.HighlightFrame = current.AtlasFrame;
            okButton.Enabled = true;
        }

        okButton.Click += (_, _) =>
        {
            if (atlas != null && selectedFrame >= 0)
                Result = new TerrainTextureRef(atlas.Index, selectedFrame);
        };
    }

    private void LoadSprite(uint index)
    {
        try
        {
            var sprite = Sprite.Read(new DataReader(containerFiles[index]));
            var newAtlas = Atlas.FromSprite(sprite, index);
            atlas?.Dispose();
            atlas = newAtlas;
            canvas.Atlas = atlas;
            infoLabel.Text = $"Sprite #{index}: {atlas.Bitmap.Width}×{atlas.Bitmap.Height}, {atlas.FrameCount} Frames";
        }
        catch (Exception ex)
        {
            infoLabel.Text = $"Konnte Sprite #{index} nicht laden: {ex.Message}";
            atlas?.Dispose();
            atlas = null;
            canvas.Atlas = null;
        }
    }
}
