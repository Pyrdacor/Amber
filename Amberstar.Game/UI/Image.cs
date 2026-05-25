using Amber.Common;
using Amber.Renderer.Common;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.UI;

internal class Image : Control
{
    readonly ISprite image;
    bool destroyed = false;

    public Image(Game game, int x, int y, int width, int height, int textureIndex, byte displayLayer, Layer layer = Layer.UI, byte? paletteIndex = null, bool opaque = false)
        : this(game, new(x, y), new(width, height), textureIndex, displayLayer, layer, paletteIndex, opaque)
    {

    }

    public Image(Game game, Position position, Size size, int textureIndex, byte displayLayer, Layer layer = Layer.UI, byte? paletteIndex = null, bool opaque = false)
    {
        image = game.CreateSprite(layer, position, size, textureIndex, paletteIndex ?? game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI], opaque)!;
        image.DisplayLayer = displayLayer;
        image.Visible = true;
    }

    public override bool Visible
    {
        get => image.Visible && !destroyed;
        set
        {
            if (!destroyed)
                image.Visible = value;
        }
    }

    public override byte DisplayLayer
    {
        get => image.DisplayLayer;
        set => image.DisplayLayer = value;
    }

    public override byte PaletteIndex
    {
        get => image.PaletteIndex;
        set => image.PaletteIndex = value;
    }

    public void SetTextureIndex(int textureIndex)
    {
        var textureAtlas = image.Layer.Config.Texture!;
        image.TextureOffset = textureAtlas.GetOffset(textureIndex);
    }

    public override void Destroy()
    {
        base.Destroy();

        destroyed = true;
        image.Visible = false;
    }
}
