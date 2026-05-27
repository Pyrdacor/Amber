using Amber.Common;
using Amber.Renderer;
using Amberstar.GameData;

namespace Amberstar.Game.UI;

internal class List : Control
{
    public const int LineHeight = 7;

    readonly Game game;
    readonly IColoredRect? background = null;
    readonly List<Label> labels = [];
    readonly List<string> texts = [];
    byte displayLayer = 0;
    byte paletteIndex = 0;
    bool visible = false;
    bool destroyed = false;
    readonly int? backgroundColorIndex = null;
    int currentLabelY = 0;
    int scrollOffset = 0;
    int maxScrollOffset = 0;

    public event Action<int, string>? ItemClicked;

    private bool CanScroll => maxScrollOffset != 0;

    private int ScrollOffset
    {
        get => scrollOffset;
        set
        {
            if (scrollOffset == value)
                return;

            int scrollDiff = value - scrollOffset;
            scrollOffset = value;
            Redraw(scrollDiff);
        }
    }

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

        Clear();
    }

    public void Clear()
    {
        labels.ForEach(label => label.Destroy());
        labels.Clear();
        texts.Clear();
        currentLabelY = 0;
    }

    public void AddItem(string text)
    {
        AddItem().SetText(text, 15, TextManager.TransparentPaper, paletteIndex);
        texts.Add(text);
    }

    public void AddItem(IText text)
    {
        AddItem().SetText(text, Area.Size.Width - 3, 15, TextManager.TransparentPaper, paletteIndex);
        texts.Add(text.GetString());
    }

    private Label AddItem()
    {
        if (destroyed)
            throw new InvalidOperationException("Adding items to a destroyed list is not allowed.");

        byte textDisplayLayer = (byte)(displayLayer + 3);

        var label = new Label(game)
        {
            Area = new(Area.Position + new Position(3, currentLabelY), new(Area.Size.Width - 3, LineHeight)),
            DisplayLayer = textDisplayLayer,
            Visible = visible
        };
        currentLabelY += LineHeight;

        labels.Add(label);

        var areaHeight = Area.Size.Height;
        label.Visible = visible && currentLabelY <= areaHeight;
        maxScrollOffset = Math.Max(0, (currentLabelY - areaHeight) / LineHeight);

        return label;
    }

    private void Redraw(int scrollDiff)
    {
        scrollDiff *= LineHeight;

        labels.ForEach(label =>
        {
            label.Position = label.Position + new Position(0, -scrollDiff);
            label.Visible = visible && label.Position.Y > Area.Top && label.Position.Y + LineHeight <= Area.Bottom;
        });
    }

    public void Scroll(int amount)
    {
        if (amount == 0 || !CanScroll)
            return;

        if (amount > 0)
        {
            ScrollOffset += Math.Min(amount, maxScrollOffset - scrollOffset);
        }
        else
        {
            ScrollOffset -= Math.Min(amount, scrollOffset);
        }
    }

    public void ScrollToBegin() => Scroll(int.MinValue);

    public void ScrollToEnd() => Scroll(int.MaxValue);

    public void MouseWheel(int amount)
    {
        if (amount < 0)
            Scroll(-1);
        else if (amount > 0)
            Scroll(1);
    }

    public bool MouseClick(Position position)
    {
        int lineIndex = scrollOffset;

        foreach (var label in labels)
        {
            if (label.Visible && label.Area.Contains(position))
            {
                ItemClicked?.Invoke(lineIndex, texts[lineIndex]);
                return true;
            }

            lineIndex++;
        }

        return false;
    }
}
