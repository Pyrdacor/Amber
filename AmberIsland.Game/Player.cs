using Amber.Common;
using Amber.Renderer.Common;
using AmberIsland.GameData;

namespace AmberIsland.Game;

internal class Player
{
    public enum State
    {
        Idle,
        Walking,
        Running
    }

    private const int TicksPerAnimationFrame = 6;
    private readonly Game game;
    private readonly ISequencedSprite sprite;
    private readonly ISequencedSprite outfitSprite;
    private static readonly Dictionary<State, Animation> animations = [];
    private State state = State.Idle;
    private Direction direction = Direction.Down;
    private long lastAnimationTicks = 0;

    public State CurrentState
    {
        get => state;
        set
        {
            if (state == value)
                return;

            state = value;
            lastAnimationTicks = 0;
            InitAnimation();
        }
    }

    public Direction CurrentDirection
    {
        get => direction;
        set
        {
            if (direction == value)
                return;

            int directionDiff = value - direction;

            direction = value;
            lastAnimationTicks = 0;

            var animation = GetAnimation();
            var newOrigin = sprite.FrameOrigin + directionDiff * (animation.DirectionOffset ?? Position.Zero);
            outfitSprite.FrameOrigin = sprite.FrameOrigin = newOrigin;
            outfitSprite.CurrentFrameIndex = sprite.CurrentFrameIndex = 0;
        }
    }

    public Position Position
    {
        get => sprite.Position;
        set
        {
            sprite.Position = value;
            outfitSprite.Position = value;
        }
    }

    public Rect CollisionArea => new(Position.X + 4, Position.Y + 1, sprite.Size.Width - 8, 15);

    static Player()
    {
        var frameSize = new Size(48, 40);

        animations.Add(State.Idle, new()
        {
            FrameSize = frameSize,
            FrameIndices = [0],
            DirectionOffset = new(0, 40),
        });

        animations.Add(State.Walking, new()
        {
            FrameSize = frameSize,
            FrameIndices = [32, 33, 34, 35, 36, 37],
            DirectionOffset = new(0, 40),
        });

        animations.Add(State.Running, new()
        {
            FrameSize = frameSize,
            FrameIndices = [37, 39, 34, 38],
            DirectionOffset = new(0, 40),
        });
    }

    public Player(Game game)
    {
        this.game = game;

        var layer = game.GetRenderLayer(Layer.Player);

        var animation = GetAnimation();
        sprite = layer.SpriteFactory!.CreateSequenced();
        sprite.Position = new(0, 0);
        sprite.Size = new(24, 20);
        sprite.PaletteIndex = 0;
        sprite.Visible = true;

        layer = game.GetRenderLayer(Layer.Outfit);

        outfitSprite = layer.SpriteFactory!.CreateSequenced();
        outfitSprite.Position = sprite.Position;
        outfitSprite.Size = sprite.Size;
        outfitSprite.PaletteIndex = 0;
        outfitSprite.Visible = sprite.Visible;

        SetFrameIndicesAndOrigin(resetFrameIndex: true);
    }

    private Animation GetAnimation()
    {
        return animations[state];
    }

    public void Update(long ticks)
    {
        if (lastAnimationTicks == 0)
            lastAnimationTicks = ticks;

        if (sprite.FrameIndices.Length > 1)
        {
            long elapsed = ticks - lastAnimationTicks;

            while (elapsed >= TicksPerAnimationFrame)
            {
                sprite.CurrentFrameIndex += 1;
                outfitSprite.CurrentFrameIndex = sprite.CurrentFrameIndex;
                elapsed -= TicksPerAnimationFrame;
            }

            lastAnimationTicks = ticks - elapsed;
        }
    }

    private void InitAnimation()
    {
        SetFrameIndicesAndOrigin(resetFrameIndex: true);
    }

    private void SetFrameIndicesAndOrigin(bool resetFrameIndex)
    {
        var animation = GetAnimation();
        var atlas = sprite.Layer.Config.Texture!;
        int framesPerRow = atlas.Size.Width / animation.FrameSize.Width;
        int firstIndex = (int)animation.FrameIndices.Min();
        int row = firstIndex / framesPerRow;
        int column = firstIndex % framesPerRow;
        var origin = new Position(column * animation.FrameSize.Width, row * animation.FrameSize.Height) + (int)direction * (animation.DirectionOffset ?? Position.Zero);

        outfitSprite.TextureSize = sprite.TextureSize = animation.FrameSize;
        outfitSprite.FrameOrigin = sprite.FrameOrigin = origin;
        outfitSprite.FrameIndices = sprite.FrameIndices = animation.FrameIndices.Select(index => (int)(index - firstIndex)).ToArray();

        if (resetFrameIndex)
            outfitSprite.CurrentFrameIndex = sprite.CurrentFrameIndex = 0;
    }
}
