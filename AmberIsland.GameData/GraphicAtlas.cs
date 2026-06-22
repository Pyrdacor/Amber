using Amber.Common;

namespace AmberIsland.GameData;

public readonly record struct GraphicAtlas(
    Graphic Graphic,
    Dictionary<uint, Position> Offsets
)
{
    public static GraphicAtlas FromGraphic(Graphic graphic, Size frameSize, uint? frameCount = null)
    {
        int framesPerRow = graphic.Width / frameSize.Width;
        int frameRows = graphic.Height / frameSize.Height;
        frameCount ??= (uint)(frameRows * framesPerRow);
        var offsets = new Dictionary<uint, Position>((int)frameCount);

        for (int i = 0; i < frameCount.Value; i++)
        {
            offsets.Add((uint)i, new(i % framesPerRow, i / framesPerRow));
        }

        return new GraphicAtlas(graphic, offsets);
    }

    public static GraphicAtlas FromSpriteWithEmbeddedPalette(Sprite sprite, Size frameSize, uint? frameCount = null)
    {
        var graphic = Graphic.FromSpriteWithEmbeddedPalette(sprite);

        return FromGraphic(graphic, frameSize, frameCount);
    }

    public static GraphicAtlas FromSpriteAndPalette(Sprite sprite, PaletteRgb palette, Size frameSize, uint? frameCount = null)
    {
        var graphic = Graphic.FromSpriteAndPalette(sprite, palette);

        return FromGraphic(graphic, frameSize, frameCount);
    }
}
