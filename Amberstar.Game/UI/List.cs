using Amber.Common;
using Amber.Renderer;
using Amberstar.GameData;

namespace Amberstar.Game.UI;

internal class List : Control
{
    readonly Game game;
    readonly IColoredRect? background = null;
    readonly List<Label> labels = [];
    byte displayLayer = 0;
    byte paletteIndex = 0;
    bool visible = false;
    bool destroyed = false;
    readonly int? backgroundColorIndex = null;

    public List(Game game, int x, int y, int width, int height, byte displayLayer, int? backgroundColorIndex = null, byte? paletteIndex = null)
    {
        this.game = game;
        this.displayLayer = Math.Min(displayLayer, (byte)250);
        this.paletteIndex = paletteIndex ?? game.PaletteIndexProvider.BuiltinPaletteIndices[GameData.Serialization.BuiltinPalette.UI];
        this.backgroundColorIndex = backgroundColorIndex;

        Area = new(x, y, width, height);

        if (backgroundColorIndex != null)
        {
            background = game.CreateColoredRect(Layer.UI, new(x, y), new(width, height), game.PaletteColorProvider.GetPaletteColor(this.paletteIndex, backgroundColorIndex.Value));
        }
    }

    public Rect Area { get; }

    public override bool Visible
    {
        get => visible && !destroyed;
        set
        {
            if (!destroyed || !value)
            {
                visible = value;
                labels.ForEach(label => label.Visible = visible);

                if (background != null)
                    background.Visible = visible;
            }
        }
    }

    public override byte DisplayLayer
    {
        get => displayLayer;
        set
        {
            if (!destroyed && displayLayer != value)
            {
                displayLayer = value;
                byte textDisplayLayer = (byte)(displayLayer + 3);
                labels.ForEach(label => label.DisplayLayer = textDisplayLayer);

                if (background != null)
                    background.DisplayLayer = displayLayer;
            }
        }
    }

    public override byte PaletteIndex
    {
        get => paletteIndex;
        set
        {
            if (!destroyed && paletteIndex != value)
            {
                paletteIndex = value;
                labels.ForEach(label => label.PaletteIndex = paletteIndex);

                if (background != null)
                    background.Color = game.PaletteColorProvider.GetPaletteColor(paletteIndex, backgroundColorIndex!.Value);
            }
        }
    }

    public override void Destroy()
    {
        base.Destroy();

        Visible = false;
        destroyed = true;

        labels.ForEach(label => label.Destroy());
        labels.Clear();
    }

    public void AddItem(string text)
    {
        AddItem().SetText(text, 15, TextManager.TransparentPaper, paletteIndex);
    }

    public void AddItem(IText text)
    {
        AddItem().SetText(text, Area.Size.Width - 3, 15, TextManager.TransparentPaper, paletteIndex);
    }

    private Label AddItem()
    {
        if (destroyed)
            throw new InvalidOperationException("Adding items to a destroyed list is not allowed.");

        byte textDisplayLayer = (byte)(displayLayer + 3);

        var label = new Label(game)
        {
            Position = Area.Position + new Position(3, 0),
            DisplayLayer = textDisplayLayer,
            Visible = visible
        };

        labels.Add(label);

        return label;
    }
}
