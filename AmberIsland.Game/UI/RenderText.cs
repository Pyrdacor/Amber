using Amber.Common;
using Amber.Renderer.Common;
using AmberIsland.GameData;

namespace AmberIsland.Game.UI;

// This must match the colors in assets/text_palette_aipal!
// Never use more than 255 colors!
public enum TextColor
{
    White = 1,
    LightGray,
    Gray,
    DarkGray,
    DarkestGray,
    Black,
    Red,
    DarkerRed,
    DarkRed,
    DarkestRed,
    Green,
    DarkerGreen,
    DarkGreen,
    DarkestGreen,
    Blue,
    DarkerBlue,
    DarkBlue,
    DarkestBlue,
    // TODO ... 
}

public enum TextAlignment
{
    Left,
    Center,
    Right
}

internal record FontGlyphInfo
(
    int Advance,
    Position TextureOffset // relative to the font's atlas
);

internal record FontInfo
(
    Rect TextureArea,
    int GlyphTextureWidth,
    int GlyphTextureHeight,
    Dictionary<char, FontGlyphInfo> Glyphs
);

internal class RenderText
{
    private const int TabSize = 4;
    private static readonly Dictionary<FontIndex, Layer> fontLayers = new()
    {
        [FontIndex.DamageFont] = Layer.MapFont
    };
    private static readonly Dictionary<FontIndex, FontInfo> fonts = [];

    public static void RegisterFonts(Game game)
    {
        var fonts = game.GameData.GetFonts();

        foreach (var fontLayer in fontLayers)
        {
            var (fontIndex, layer) = fontLayer;
            var font = fonts[(uint)fontIndex];
            var textureAtlas = game.GetRenderLayer(layer).Config.Texture!;
            var textureArea = textureAtlas.GetArea((int)fontIndex);
            var glyphs = new Dictionary<char, FontGlyphInfo>();
            int glyphX = 0;
            int glyphY = 0;

            foreach (var glyph in font.Glyphs.OrderBy(glyph => glyph.Char.ToChar()))
            {
                var glyphInfo = new FontGlyphInfo(glyph.Advance, new(glyphX, glyphY));

                glyphs.Add(glyph.Char.ToChar(), glyphInfo);
                glyphX += font.GlyphWidth;

                if (glyphX > font.AtlasWidth - font.GlyphWidth)
                {
                    glyphX = 0;
                    glyphY += font.GlyphHeight;
                }
            }

            var fontInfo = new FontInfo(textureArea, font.GlyphWidth, font.GlyphHeight, glyphs);

            RenderText.fonts[fontIndex] = fontInfo;
        }
    }

    private readonly Game game;
    private readonly FontInfo font;
    private List<ISprite> glyphSprites;
    private List<ISprite> shadowSprites;
    private readonly Layer layer;
    private string text = "";
    private int fontSize = 24;
    private bool visible = false;
    private byte displayLayer = 0;
    private byte alpha = 255;
    private Position drawPosition = Position.Zero;
    private TextAlignment textAlignment = TextAlignment.Left;
    private Size textAreaSize = Size.Zero;
    private TextColor color = TextColor.White;
    private bool shadow = false;

    public bool Visible
    {
        get => visible;
        set
        {
            if (visible == value)
                return;

            visible = value;
            glyphSprites.ForEach(glyphSprite => glyphSprite.Visible = visible);
            shadowSprites.ForEach(glyphSprite => glyphSprite.Visible = visible);
        }
    }

    public bool Shadow
    {
        get => shadow;
        set
        {
            if (shadow == value)
                return;

            shadow = value;

            if (shadow)
            {
                shadowSprites = CreateShadowSprites();
            }
            else
            {
                shadowSprites.ForEach(shadowSprite => shadowSprite.Visible = false);
                shadowSprites.Clear();
            }

        }
    }

    public byte DisplayLayer
    {
        get => displayLayer;
        set
        {
            if (displayLayer == value)
                return;

            displayLayer = value;
            glyphSprites.ForEach(glyphSprite => glyphSprite.DisplayLayer = (byte)Math.Min(displayLayer + 1, 255));
            shadowSprites.ForEach(glyphSprite => glyphSprite.DisplayLayer = Math.Min(displayLayer, (byte)254));
        }
    }

    public byte Alpha
    {
        get => alpha;
        set
        {
            if (alpha == value)
                return;

            alpha = value;
            glyphSprites.ForEach(glyphSprite =>
            {
                if (glyphSprite is IAlphaSprite alphaSprite)
                    alphaSprite.Alpha = alpha;
            });
            shadowSprites.ForEach(shadowSprite =>
            {
                if (shadowSprite is IAlphaSprite alphaSprite)
                    alphaSprite.Alpha = alpha;
            });
        }
    }

    public string Text
    {
        get => text;
        set
        {
            if (text == value)
                return;

            text = value;
            glyphSprites.ForEach(glyphSprite => glyphSprite.Visible = false);
            glyphSprites = CreateGlyphSprites();

            if (shadow)
            {
                shadowSprites.ForEach(glyphSprite => glyphSprite.Visible = false);
                shadowSprites = CreateShadowSprites();
            }
        }
    }

    public Position DrawPosition
    {
        get => drawPosition;
        set
        {
            if (drawPosition == value)
                return;

            var diff = value - drawPosition;
            drawPosition = value;
            glyphSprites.ForEach(glyphSprite => glyphSprite.Position = game.Renderer.ToScreen(game.Renderer.FromScreen(glyphSprite.Position) + diff));
            shadowSprites.ForEach(shadowSprite => shadowSprite.Position = game.Renderer.ToScreen(game.Renderer.FromScreen(shadowSprite.Position) + diff));
        }
    }

    private Position AnchorPositionOffset => textAlignment switch
    {
        TextAlignment.Center => new Position(textAreaSize.Width / 2, 0),
        TextAlignment.Right => new Position(textAreaSize.Width, 0),
        _ => Position.Zero
    };

    /// <summary>
    /// This is the anchor position.
    /// If TextAlignment is Left this matches the draw position.
    /// For TextAlignment Center this is the center position (Y still is at top!).
    /// For TextAlignment Right this is the right-most position.
    /// </summary>
    public Position AnchorPosition
    {
        get => DrawPosition + AnchorPositionOffset;
        set => DrawPosition = value - AnchorPositionOffset;
    }

    public TextAlignment TextAlignment
    {
        get => textAlignment;
        set
        {
            if (textAlignment == value)
                return;

            var anchorPosition = AnchorPosition;

            textAlignment = value;

            AnchorPosition = anchorPosition;
        }
    }

    public TextColor Color
    {
        get => color;
        set
        {
            if (color == value)
                return;

            color = value;
            glyphSprites.ForEach(glyphSprite => glyphSprite.MaskColorIndex = (byte)color);
        }
    }

    public RenderText(Game game, FontIndex fontIndex, string text, int fontSize)
    {
        if (fonts.Count == 0)
            RegisterFonts(game);

        this.game = game;
        this.text = text;
        this.fontSize = fontSize;
        font = fonts[fontIndex];
        layer = fontLayers[fontIndex];
        glyphSprites = CreateGlyphSprites();
        shadowSprites = [];
    }

    public RenderText(Game game, FontIndex fontIndex, string text, int fontSize, Position anchorPosition, TextAlignment textAlignment)
    {
        if (fonts.Count == 0)
            RegisterFonts(game);

        this.game = game;
        this.text = text;
        this.fontSize = fontSize;
        font = fonts[fontIndex];
        layer = fontLayers[fontIndex];
        glyphSprites = CreateGlyphSprites();
        shadowSprites = [];

        this.textAlignment = textAlignment;
        AnchorPosition = anchorPosition;
    }

    private List<ISprite> CreateShadowSprites()
    {
        var shadowSprites = new List<ISprite>(glyphSprites.Count);
        var renderLayer = game.GetRenderLayer(layer);
        var spriteFactory = renderLayer.SpriteFactory!;
        bool useAlpha = renderLayer.Config.LayerFeatures.HasFlag(LayerFeatures.Alpha);
        var shadowOffset = new Position(2, 2);

        foreach (var glyphSprite in glyphSprites)
        {
            var shadowSprite = useAlpha
                ? spriteFactory.CreateWithAlpha()
                : spriteFactory.Create();

            shadowSprite.Position = game.Renderer.ToScreen(game.Renderer.FromScreen(glyphSprite.Position) + shadowOffset);
            shadowSprite.Size = glyphSprite.Size;
            shadowSprite.TextureSize = glyphSprite.TextureSize;
            shadowSprite.TextureOffset = glyphSprite.TextureOffset;
            shadowSprite.DisplayLayer = (byte)Math.Max(0, glyphSprite.DisplayLayer - 1);
            shadowSprite.MaskColorIndex = (byte)TextColor.Black;
            shadowSprite.PaletteIndex = glyphSprite.PaletteIndex;
            shadowSprite.Visible = glyphSprite.Visible;

            if (shadowSprite is IAlphaSprite alphaSprite)
                alphaSprite.Alpha = alpha;

            shadowSprites.Add(shadowSprite);
        }

        return shadowSprites;
    }

    private List<ISprite> CreateGlyphSprites()
    {
        // Relative positions
        int x = 0;
        int y = 0;
        var glyphSprites = new List<ISprite>();
        var renderLayer = game.GetRenderLayer(layer);
        var spriteFactory = renderLayer.SpriteFactory!;
        bool useAlpha = renderLayer.Config.LayerFeatures.HasFlag(LayerFeatures.Alpha);
        var textureSize = new Size(font.GlyphTextureWidth, font.GlyphTextureHeight);
        int width = 0;
        var anchorPosition = AnchorPosition;
        float sizeFactor = (float)fontSize / font.GlyphTextureHeight;

        foreach (var ch in text.TrimEnd())
        {
            if (ch == ' ')
                x += MathUtil.Round(sizeFactor * font.Glyphs[ch].Advance);
            else if (ch == '\t')
                x += TabSize * MathUtil.Round(sizeFactor * font.Glyphs[ch].Advance);
            else if (ch == '\r')
                continue;
            else if (ch == '\n')
            {
                if (x > width)
                    width = x;

                x = 0;
                y += fontSize; // TODO: do we need a proper line height?
            }
            else if (font.Glyphs.TryGetValue(ch, out var glyph))
            {
                ISprite glyphSprite = useAlpha
                    ? spriteFactory.CreateWithAlpha()
                    : spriteFactory.Create();

                glyphSprite.Position = game.Renderer.ToScreen(drawPosition + new Position(x, y));
                glyphSprite.Size = game.Renderer.ToScreen(new Size(MathUtil.Round(sizeFactor * font.GlyphTextureWidth), fontSize)); // TODO: do we need a proper glyph render height or use line height?
                glyphSprite.TextureSize = textureSize;
                glyphSprite.TextureOffset = font.TextureArea.Position + glyph.TextureOffset;
                glyphSprite.DisplayLayer = (byte)Math.Min(255, displayLayer + 1);
                glyphSprite.MaskColorIndex = (byte)color;
                glyphSprite.PaletteIndex = 0; // always
                glyphSprite.Visible = visible;

                if (glyphSprite is IAlphaSprite alphaSprite)
                    alphaSprite.Alpha = alpha;

                glyphSprites.Add(glyphSprite);
                x += MathUtil.Round(sizeFactor * glyph.Advance);
            }
        }

        if (x > width)
            width = x;

        textAreaSize = new(width, y + font.GlyphTextureHeight);
        AnchorPosition = anchorPosition;

        return glyphSprites;
    }

    public void Delete()
    {
        Visible = false;
        glyphSprites.Clear();
        shadowSprites.Clear();
    }
}
