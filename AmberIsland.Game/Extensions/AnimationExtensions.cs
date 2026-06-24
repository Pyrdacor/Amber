using Amber.Common;
using Amber.Renderer.Common;
using AmberIsland.GameData;

namespace AmberIsland.Game.Extensions;

internal static class AnimationExtensions
{
    public static void SetFrameIndicesAndOrigin(this ISequencedSprite sprite, Func<Animation> animationProvider, Direction direction, bool resetFrameIndex)
    {
        var animation = animationProvider();
        var atlas = sprite.Layer.Config.Texture!;
        int framesPerRow = atlas.Size.Width / animation.FrameSize.Width;
        int firstIndex = (int)animation.FrameIndices.Min();
        int row = firstIndex / framesPerRow;
        int column = firstIndex % framesPerRow;
        var origin = new Position(column * animation.FrameSize.Width, row * animation.FrameSize.Height) + (int)direction * (animation.DirectionOffset ?? Position.Zero);

        sprite.TextureSize = animation.FrameSize;
        sprite.FrameOrigin = origin;
        sprite.FrameIndices = animation.FrameIndices.Select(index => (int)(index - firstIndex)).ToArray();

        if (resetFrameIndex)
            sprite.CurrentFrameIndex = 0;
    }
}
