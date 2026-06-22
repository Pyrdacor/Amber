namespace AmberIsland.GameData;

public readonly record struct Graphic(
    ushort Width,
    ushort Height,
    ColorRgba[] Pixels
)
{
    public static Graphic FromSpriteWithEmbeddedPalette(Sprite sprite)
    {
        ushort width = sprite.Width;
        ushort height = sprite.Height;

        var pixels = new ColorRgba[sprite.ColorIndices.Length];

        for (int i = 0; i < sprite.ColorIndices.Length; i++)
        {
            int index = sprite.ColorIndices[i];

            if (index == 0)
                pixels[i] = ColorRgba.Transparent;
            else
                pixels[i] = ColorRgba.FromColorRgb(sprite.Colors[index - 1]);
        }

        return new Graphic(width, height, pixels);
    }

    public static Graphic FromSpriteAndPalette(Sprite sprite, PaletteRgb palette)
    {
        ushort width = sprite.Width;
        ushort height = sprite.Height;

        var pixels = new ColorRgba[sprite.ColorIndices.Length];

        for (int i = 0; i < sprite.ColorIndices.Length; i++)
        {
            int index = sprite.ColorIndices[i];

            if (index == 0)
                pixels[i] = ColorRgba.Transparent;
            else
                pixels[i] = ColorRgba.FromColorRgb(palette.Colors[index - 1]);
        }

        return new Graphic(width, height, pixels);
    }
}
