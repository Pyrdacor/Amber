using Amber.Common;
using Amber.Renderer;
using Amberstar.GameData;

namespace Amberstar.Game.UI;

public enum TextAlignment
{
    Left,
    Center,
    Right
}

internal interface IRenderText
{
    bool SupportsScrolling { get; }
    int TextLineCount { get; }
	int LineHeight { get; }
    bool Visible { get; set; }

    event Action? ScrollEnded;

    void Show(int x, int y, byte displayLayer);
    void ShowInArea(int x, int y, int width, int height, byte displayLayer, TextAlignment textAlignment = TextAlignment.Left);
    void Delete();
    bool Scroll(int lines);
    bool ScrollFullHeight();
}

internal static class RenderTextExtensions
{
    public static void ShowInArea(this IRenderText text, Rect area, byte displayLayer, TextAlignment textAlignment = TextAlignment.Left)
    {
        text.ShowInArea(area.Position.X, area.Position.Y, area.Size.Width, area.Size.Height, displayLayer, textAlignment);
    }
}

internal class TextManager(Game game, IFont font,
    IFontInfoProvider fontInfoProvider)
{
    public const int DefaultInkColorIndex = 15;
    public const int DefaultPaperColorIndex = 2;
    public const int TransparentPaper = -1;
    const int TicksPerScroll = 4; // TODO
    public const byte DefaultPaletteIndex = 0; // UI

    public int GetTextRenderWidth(string text)
    {
        if (text.Trim('\n').Length == 0)
            return 0;

        var lines = text.Split('\n');
        int maxLineSize = lines.Max(line => line.Length);

        return maxLineSize * font.Advance;
    }

    struct TextBlock(int textColorIndex, int paperColorIndex, string text, bool runes)
    {
        public int TextColorIndex = textColorIndex;
        public int PaperColorIndex = paperColorIndex;
        public bool Runes = runes;
        public string Text = text;
    }

    struct TextLine(List<TextBlock> textBlocks)
    {
        public List<TextBlock> TextBlocks = textBlocks;
    }

    class Text
    (
        Game game, List<TextLine> textLines, IFont font,
        IFontInfoProvider fontInfoProvider, byte paletteIndex
    ) : IRenderText
    {
        readonly ILayer layer = game.GetRenderLayer(Layer.Text);
        readonly List<TextLine> textLines = textLines;
		readonly List<List<ISprite>> glyphShadows = [];
		readonly List<List<ISprite>> glyphs = [];
        readonly List<long> startedScrollAction = [];
        int maxScroll = 0;
        int areaX = 0;
        int areaY = 0;
        int areaHeight = 0;
        int scrollOffsetInPixels = 0;
        int scrollOffsetInLines = 0;
        byte displayLayer = 0;
        bool visible = false;

        public event Action? ScrollEnded;

        public bool SupportsScrolling { get; private set; } = false;

        public int TextLineCount => textLines.Count;

        public int LineHeight => font.LineHeight;

        public bool Visible
        {
            get => visible;
            set
            {
                if (visible == value)
                    return;

                if (value && glyphs.Count == 0)
                    return; // Cannot make text without glyphs visible

                visible = value;

                foreach (var glyph in glyphs.SelectMany(g => g))
                    glyph.Visible = visible;
                foreach (var shadow in glyphShadows.SelectMany(g => g))
                    shadow.Visible = visible;
            }
        }

		private static readonly Dictionary<char, int> UnicodeToAtariST = new()
        {
            { 'Ç', 0x80 },
            { 'ü', 0x81 },
            { 'é', 0x82 },
            { 'â', 0x83 },
            { 'ä', 0x84 },
            { 'à', 0x85 },
            { 'å', 0x86 },
            { 'ç', 0x87 },
            { 'ê', 0x88 },
            { 'ë', 0x89 },
            { 'è', 0x8A },
            { 'ï', 0x8B },
            { 'î', 0x8C },
            { 'ì', 0x8D },
            { 'Ä', 0x8E },
            { 'Å', 0x8F },
            { 'É', 0x90 },
            { 'æ', 0x91 },
            { 'Æ', 0x92 },
            { 'ô', 0x93 },
            { 'ö', 0x94 },
            { 'ò', 0x95 },
            { 'û', 0x96 },
            { 'ù', 0x97 },
            { 'ÿ', 0x98 },
            { 'Ö', 0x99 },
            { 'Ü', 0x9A },
            { '¢', 0x9B },
            { '£', 0x9C },
            { '¥', 0x9D },
            { 'ß', 0x9E },
            { 'ƒ', 0x9F },
            { 'á', 0xA0 },
            { 'í', 0xA1 },
            { 'ó', 0xA2 },
            { 'ú', 0xA3 },
            { 'ñ', 0xA4 },
            { 'Ñ', 0xA5 },
            { '¿', 0xA8 },
            { '¬', 0xAA },
            { '¡', 0xAD },
            { '«', 0xAE },
            { '»', 0xAF },
            { 'ã', 0xB0 },
            { 'õ', 0xB1 },
            { 'Ø', 0xB2 },
            { 'ø', 0xB3 },
            { 'œ', 0xB4 },
            { 'Œ', 0xB5 },
            { 'À', 0xB6 },
            { 'Ã', 0xB7 },
            { 'Õ', 0xB8 },
            { 'ĳ', 0xC0 },
            { 'Ĳ', 0xC1 },
            { '§', 0xDD },
            { '°', 0xF8 },
            { '²', 0xFD },
            { '³', 0xFE },
        };

        private static char ConvertChar(char ch)
        {
            // Note: This is not related to original Amberstar code but
            // the encoding on the Atari/Amiga was different so we have
            // to map some characters like german Umlauts here.
            return (char)UnicodeToAtariST.GetValueOrDefault(ch, ch);
        }

        // TODO: use paperIndex!
        private ISprite CreateTextSprite(int x, int y, int glyphIndex, int colorIndex, int paperIndex, int displayLayerOffset, bool shadow)
        {
            var textureAtlas = layer.Config.Texture!;
            var glyph = layer.SpriteFactory!.Create();
            glyph.DisplayLayer = (byte)MathUtil.Limit(byte.MinValue, displayLayer + displayLayerOffset, byte.MaxValue);
            glyph.Position = new(x, y);
            glyph.Size = new(font.GlyphWidth, font.GlyphHeight);
            glyph.TextureOffset = textureAtlas.GetOffset(glyphIndex);
            int clipHeight = areaHeight == 0 ? int.MaxValue : areaHeight;
            glyph.ClipRect = new(areaX, areaY, int.MaxValue, clipHeight);
            glyph.MaskColorIndex = (byte)colorIndex;
            glyph.PaletteIndex = (byte)(shadow ? 0 : paletteIndex);
            glyph.Visible = true;

            return glyph;
        }

        private void SetupTextLine(int x, int y, int line, TextLine textLine)
        {
            List<ISprite> glyphLine;
			List<ISprite> shadowLine;

			if (line == glyphs.Count)
            {
                glyphLine = [];
				shadowLine = [];
				glyphs.Add(glyphLine);
                glyphShadows.Add(shadowLine);
            }
            else
            {
                glyphs[line].ForEach(glyph => glyph.Visible = false);
                glyphs[line].Clear();
				glyphShadows[line].ForEach(shadow => shadow.Visible = false);
				glyphShadows[line].Clear();
				glyphLine = glyphs[line];
                shadowLine = glyphShadows[line];
            }

            foreach (var textBlock in textLine.TextBlocks)
            {
                var mapper = textBlock.Runes
                    ? fontInfoProvider.RuneGlyphTextureIndices
                    : fontInfoProvider.TextGlyphTextureIndices;

                foreach (char ch in textBlock.Text)
                {
                    if (ch == 14)
                        x -= font.Advance;
                    else if (ch == ' ' || ch == '\t')
                        x += font.Advance;
                    else
                    {
						shadowLine.Add(CreateTextSprite(x + 1, y + 1, mapper[ConvertChar(ch)], 0, textBlock.PaperColorIndex, 0, true));
						glyphLine.Add(CreateTextSprite(x, y, mapper[ConvertChar(ch)], textBlock.TextColorIndex, textBlock.PaperColorIndex, 2, false));						
						x += font.Advance;
                    }
                }
            }
        }

        private void InternalShow(int x, int y, byte displayLayer, int lineCount)
        {
            areaX = x;
            areaY = y;
            scrollOffsetInPixels = 0;
            scrollOffsetInLines = 0;
            this.displayLayer = displayLayer;

            Delete();

            if (startedScrollAction.Count != 0)
            {
                game.DeleteDelayedActions(startedScrollAction.ToArray());
                startedScrollAction.Clear();
            }

            lineCount = Math.Min(lineCount, textLines.Count);

            for (int i = 0; i < lineCount; i++)
            {
                SetupTextLine(x, y, i, textLines[i]);
                y += font.LineHeight;
            }

            Visible = true;
        }

        public void ShowInArea(int x, int y, int width, int height, byte displayLayer, TextAlignment textAlignment = TextAlignment.Left)
        {
            if (height <= 0)
                height = textLines.Count * font.LineHeight - Math.Max(0, font.LineHeight - font.GlyphHeight);

            int diff = MathUtil.Limit(0, font.LineHeight - font.GlyphHeight, height - 1);
            int numDisplayedRows = (height + diff) / font.LineHeight;
            areaHeight = height;

            maxScroll = Math.Max(0, textLines.Count - numDisplayedRows);
            SupportsScrolling = numDisplayedRows < textLines.Count;

            if (textAlignment != TextAlignment.Left)
            {
                int GetTextLineLength(TextLine textLine) => textLine.TextBlocks.Sum(textBlock => textBlock.Text.Length) * font.Advance;
                int maxLineWidth = textLines.Max(GetTextLineLength);

                if (width <= 0)
                    width = maxLineWidth;

                if (textAlignment == TextAlignment.Right)
                    x = x + width - maxLineWidth;
                else if (maxLineWidth < width)
                    x += (width - maxLineWidth) / 2;
            }

            InternalShow(x, y, displayLayer, numDisplayedRows);
        }

        public void Show(int x, int y, byte displayLayer)
        {
            areaHeight = 0;
            SupportsScrolling = false;
            InternalShow(x, y, displayLayer, textLines.Count);
        }

        public bool Scroll(int lines)
        {
            if (maxScroll == 0)
                return false;

            int scrollAmount = Math.Max(1, font.LineHeight / 2);

            if (lines > maxScroll)
                lines = maxScroll;

            maxScroll -= lines;

            void ScrollText() => ScrollTextBy(scrollAmount);

            void ScrollTextBy(int amount)
            {
                int diff = MathUtil.Limit(0, font.LineHeight - font.GlyphHeight, areaHeight - 1);
                int numDisplayedRows = (areaHeight + diff) / font.LineHeight;
                scrollOffsetInPixels += amount;

                if (scrollOffsetInPixels >= font.LineHeight)
                {
                    // Line leaves upper bound.
                    scrollOffsetInPixels -= font.LineHeight;
                    scrollOffsetInLines++;

                    var firstLine = glyphs[0];
                    var firstShadowLine = glyphShadows[0];
                    glyphs.RemoveAt(0);
                    glyphShadows.RemoveAt(0);

					foreach (var glyph in glyphs.SelectMany(g => g))
                    {
                        glyph.Position = new(glyph.Position.X, glyph.Position.Y - amount);
                    }

                    foreach (var shadow in glyphShadows.SelectMany(s => s))
                    {
                        shadow.Position = new(shadow.Position.X, shadow.Position.Y - amount);
					}

                    firstLine.ForEach(g => g.Visible = false);
                    firstShadowLine.ForEach(g => g.Visible = false);

                    /*glyphs.Add(firstLine);
                    glyphShadows.Add(firstShadowLine);

                    int offset = numDisplayedRows * font.LineHeight - scrollOffsetInPixels;
                    int lineIndex = scrollOffsetInLines + numDisplayedRows - 1;
                    SetupTextLine(areaX, areaY + offset, glyphs.Count - 1, textLines[lineIndex]);*/
                }
                else
                {
                    // Just move them up.
                    foreach (var glyph in glyphs.SelectMany(g => g))
                    {
                        glyph.Position = new(glyph.Position.X, glyph.Position.Y - amount);
                    }
					foreach (var shadow in glyphShadows.SelectMany(g => g))
					{
						shadow.Position = new(shadow.Position.X, shadow.Position.Y - amount);
					}

					if (glyphs.Count == numDisplayedRows)
                    {
                        int newLineIndex = scrollOffsetInLines + numDisplayedRows;

                        // We must show the additional line
                        if (newLineIndex < textLines.Count)
                            SetupTextLine(areaX, areaY - scrollOffsetInPixels + numDisplayedRows * font.LineHeight, glyphs.Count, textLines[newLineIndex]);
                    }
                }
            }

            void ScrollEnd() => ScrollEnded?.Invoke();

            int totalScrolls = lines * font.LineHeight / scrollAmount;

            var startedScrollActions = new List<long>(totalScrolls + 1);

            for (int i = 0; i < totalScrolls; i++)
            {
                startedScrollActions.Add(game.AddDelayedAction(i * TicksPerScroll, ScrollText));
            }

            int totalScrollAmount = totalScrolls * scrollAmount;

            if (totalScrollAmount < lines * font.LineHeight)
            {
                int amount = lines * font.LineHeight - totalScrollAmount;
                startedScrollActions.Add(game.AddDelayedAction(totalScrolls * TicksPerScroll, () => ScrollTextBy(amount)));
                startedScrollActions.Add(game.AddDelayedAction((totalScrolls + 1) * TicksPerScroll, ScrollEnd));
            }
            else
            {
                startedScrollActions.Add(game.AddDelayedAction(totalScrolls * TicksPerScroll, ScrollEnd));
            }

            return true;
        }

        public bool ScrollFullHeight()
        {
            int diff = MathUtil.Limit(0, font.LineHeight - font.GlyphHeight, areaHeight - 1);
            int numDisplayedRows = (areaHeight + diff) / font.LineHeight;
            return Scroll(numDisplayedRows);
        }

        public void Delete()
        {
            if (startedScrollAction.Count != 0)
            {
                game.DeleteDelayedActions(startedScrollAction.ToArray());
                startedScrollAction.Clear();
            }

            foreach (var glyph in glyphs.SelectMany(g => g))
                glyph.Visible = false;
			foreach (var shadow in glyphShadows.SelectMany(g => g))
				shadow.Visible = false;

            glyphs.Clear();
            glyphShadows.Clear();

            visible = false;
        }
    }

    public IRenderText Create(IText text, int maxWidth,
        int defaultTextColorIndex = DefaultInkColorIndex,
        int defaultPaperColorIndex = DefaultPaperColorIndex,
        byte paletteIndex = DefaultPaletteIndex)
    {
        int maxWidthInCharacters = maxWidth / font.Advance;
        var lines = text.GetLines(maxWidthInCharacters);
        var textLines = new List<TextLine>();
        var coloredTextBlocks = new List<TextBlock>();
        string currentTextBlock = string.Empty;
        int currentInk = defaultTextColorIndex;
        int currentPaper = defaultPaperColorIndex;
        bool runes = false;

        foreach (var line in lines)
        {
            for (int i = 0; i < line.Length; i++)
            {
                var ch = line[i];

                if (ch == 1) // Set ink
                {
                    int ink = line[++i];

                    if (ink == currentInk)
                        continue;

                    EndBlock();

                    currentInk = ink;
                }
                else if (ch == 2) // Set paper
                {
                    int paper = line[++i];

                    if (paper == 255) // transparent
                        paper = TransparentPaper;

                    if (paper == currentPaper)
                        continue;

                    EndBlock();

                    currentPaper = paper;
                }
                else if (ch == '~')
                {
                    EndBlock();

                    runes = !runes;
                }
                else if (ch == '#' || ch == '\n')
                {
                    EndBlock();
                    EndLine();
                }
                else
                {
                    currentTextBlock += ch;
                }
            }

            EndBlock();
            EndLine(true);
        }

        void EndBlock()
        {
            if (currentTextBlock.Length > 0)
            {
                coloredTextBlocks.Add(new(currentInk, currentPaper, currentTextBlock, runes));
                currentTextBlock = string.Empty;
            }
        }

        void EndLine(bool last = false)
        {
            if (!last || coloredTextBlocks.Count != 0)
                textLines.Add(new(new(coloredTextBlocks)));
            coloredTextBlocks.Clear();
        }

        return new Text(game, textLines, font, fontInfoProvider, paletteIndex);
    }

    public IRenderText Create(string text,
        int defaultTextColorIndex = DefaultInkColorIndex,
        int defaultPaperColorIndex = DefaultPaperColorIndex,
        byte paletteIndex = DefaultPaletteIndex)
    {
        var textLines = new List<TextLine>();
        var coloredTextBlocks = new List<TextBlock>();
        string currentTextBlock = string.Empty;
        int currentInk = defaultTextColorIndex;
        int currentPaper = defaultPaperColorIndex;
        bool runes = false;

        for (int i = 0; i < text.Length; i++)
        {
            var ch = text[i];

            if (ch == 1) // Set ink
            {
                int ink = text[++i];

                if (ink == currentInk)
                    continue;

                EndBlock();

                currentInk = ink;
            }
            else if (ch == 2) // Set paper
            {
                int paper = text[++i];

                if (paper == 255) // transparent
                    paper = TransparentPaper;

                if (paper == currentPaper)
                    continue;

                EndBlock();

                currentPaper = paper;
            }
            else if (ch == '~')
            {
                EndBlock();

                runes = !runes;
            }
            else if (ch == '#' || ch == '\n')
            {
                EndBlock();
                EndLine(i == text.Length - 1);
            }
            else
            {
                currentTextBlock += ch;
            }
        }

        void EndBlock()
        {
            if (currentTextBlock.Length > 0)
            {
                coloredTextBlocks.Add(new(currentInk, currentPaper, currentTextBlock, runes));
                currentTextBlock = string.Empty;
            }
        }

        void EndLine(bool last = false)
        {
			if (!last || coloredTextBlocks.Count != 0)
				textLines.Add(new(new(coloredTextBlocks)));
            coloredTextBlocks.Clear();
        }

        EndBlock();
        EndLine(true);

        return new Text(game, textLines, font, fontInfoProvider, paletteIndex);
    }
}

internal class Label(Game game) : ILayeredDrawable
{
    Rect area = new();
    IRenderText? renderText;
    byte displayLayer = 0;
    TextAlignment alignment = TextAlignment.Left;
    bool needsShowCall = true;

    public ILayer Layer => game.GetRenderLayer(Amberstar.Game.Layer.Text);

    public bool Visible
    {
        get => renderText?.Visible ?? false;
        set
        {
            if (renderText != null)
            {
                if (needsShowCall && value)
                    Show();
                else
                    renderText.Visible = value;
            }
        }
    }

    public IRenderText? Text => renderText;

    public byte DisplayLayer
    {
        get => displayLayer;
        set
        {
            if (displayLayer == value)
                return;

            displayLayer = value;

            Show();
        }
    }

    public TextAlignment Alignment
    {
        get => alignment;
        set
        {
            if (alignment == value)
                return;

            alignment = value;

            Show();
        }
    }

    public Position Position
    {
        get => area.Position;
        set => Area = new(value, area.Size);
    }

    public Size Size
    {
        get => area.Size;
        set => Area = new(area.Position, value);
    }

    public Rect Area
    {
        get => area;
        set
        {
            if (area == value)
                return;

            area = value;

            Show();
        }
    }

    private void Show()
    {
        if (renderText != null)
        {
            needsShowCall = false;
            renderText.ShowInArea(area, displayLayer, alignment);
        }
    }

    public void SetText(string text,
        int defaultTextColorIndex = TextManager.DefaultInkColorIndex,
        int defaultPaperColorIndex = TextManager.DefaultPaperColorIndex,
        byte paletteIndex = TextManager.DefaultPaletteIndex)
    {
        bool wasVisible = renderText?.Visible ?? false;

        renderText?.Delete();
        renderText = game.TextManager.Create(text, defaultTextColorIndex, defaultPaperColorIndex, paletteIndex);

        if (wasVisible)
            Show();
        else
            needsShowCall = true;
    }

    public void SetText(IText text, int? maxWidth = null,
        int defaultTextColorIndex = TextManager.DefaultInkColorIndex,
        int defaultPaperColorIndex = TextManager.DefaultPaperColorIndex,
        byte paletteIndex = TextManager.DefaultPaletteIndex)
    {
        bool wasVisible = renderText?.Visible ?? false;
        maxWidth ??= Size.Width;

        renderText?.Delete();
        renderText = game.TextManager.Create(text, maxWidth.Value, defaultTextColorIndex, defaultPaperColorIndex, paletteIndex);

        if (wasVisible)
            Show();
        else
            needsShowCall = true;
    }

    public void Destroy()
    {
        renderText?.Delete();
        renderText = null;

        needsShowCall = true;
    }
}