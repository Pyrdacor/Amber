using Amber.Common;
using Amber.Renderer;

namespace Amberstar.Game.UI;

internal class Input : Control
{
    public const int Height = 8;

    readonly Game game;
    readonly IColoredRect? background = null;
    readonly Label? label = null;
    byte displayLayer = 0;
    byte paletteIndex = 0;
    bool visible = false;
    bool destroyed = false;
    string text = "_";
    readonly int maxLength = 0;

    public string Text => text[..^1];

    public Input(Game game, int x, int y, int width, byte displayLayer, int? maxLength = null)
    {
        this.game = game;
        this.displayLayer = Math.Min(displayLayer, (byte)250);
        paletteIndex = game.PaletteIndexProvider.BuiltinPaletteIndices[GameData.Serialization.BuiltinPalette.UI];

        Area = new(x, y, width, Height);

        background = game.CreateColoredRect(Layer.UI, new(x, y), new(width, Height), game.PaletteColorProvider.GetPaletteColor(paletteIndex, 3));
        background!.Visible = true;

        this.maxLength = maxLength ?? (width - 1) / 6 - 1;

        label = new Label(game)
        {
            Area = new(x + 1, y + 1, width - 1, 7),
            DisplayLayer = (byte)(displayLayer + 3),
            Visible = true
        };

        UpdateText();
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

                if (background != null)
                    background.Visible = visible;

                if (label != null)
                    label.Visible = visible;
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

                if (background != null)
                    background.DisplayLayer = displayLayer;

                if (label != null)
                    label.DisplayLayer = (byte)(displayLayer + 3);
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

                if (background != null)
                    background.Color = game.PaletteColorProvider.GetPaletteColor(paletteIndex, 3);

                if (label != null)
                    label.PaletteIndex = paletteIndex;
            }
        }
    }

    public override void Destroy()
    {
        base.Destroy();

        Visible = false;
        destroyed = true;

        label?.Destroy();
    }

    private void UpdateText()
    {
        label?.SetText(text);
    }

    public bool KeyDown(Key key)
    {
        if (destroyed || !Visible)
            return false;

        if (key == Key.Backspace || key == Key.Delete)
        {
            if (text.Length > 1)
            {
                text = text[..^2] + "_";
                UpdateText();
            }

            return true;
        }

        return false;
    }

    public bool KeyChar(char ch)
    {
        if (destroyed || !Visible)
            return false;

        if (text.Length == maxLength)
            return true;

        if ((ch >= '0' && ch <= '9') ||
            (ch >= 'A' && ch <= 'Z') ||
            (ch >= 'a' && ch <= 'z') ||
            ch == ' ' || ch == '.' /* ||
            ch == '/' || ch == ':' || ch == '\'' || ch == '_'*/)
        {
            text = text.Insert(text.Length - 1, char.ToUpper(ch).ToString());
            UpdateText();
        }

        return true;
    }
}
