using Amber.Common;
using Amber.Renderer.Common;

namespace Amberworlds.Game;

partial class Game
{
    // TODO
    Position GetTextureOffsetFromIndex(uint index) => new();

    internal ISprite CreateSprite(Layer layer, Position position, Size size, uint textureIndex, Size? textureSize = null, bool visible = true)
    {
        var sprite = renderer.Layers[(int)layer].SpriteFactory!.Create();
        sprite.TextureOffset = GetTextureOffsetFromIndex(textureIndex);
        sprite.Position = position;
        sprite.TextureSize = textureSize;
        sprite.Size = size;
        sprite.Visible = visible;

        return sprite;
    }
}
