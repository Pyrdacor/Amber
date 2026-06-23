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
    private readonly IAnimatedSprite sprite;
    private readonly IAnimatedSprite outfitSprite;
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

            direction = value;
            lastAnimationTicks = 0;

            var animation = GetAnimation();
            outfitSprite.TextureOffset = sprite.TextureOffset = animation.FirstFrameOffset + ((int)direction * animation.DirectionOffset!.Value);
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
            FirstFrameOffset = new(0, 0),
            FrameSize = frameSize,
            FrameCount = 1,
            DirectionOffset = new(0, 40),
        });

        animations.Add(State.Walking, new()
        {
            FirstFrameOffset = new(0, 160),
            FrameSize = frameSize,
            FrameCount = 6,
            DirectionOffset = new(0, 40),
        });

        animations.Add(State.Running, new()
        {
            FirstFrameOffset = new(288, 160),
            FrameSize = frameSize,
            FrameCount = 2,
            DirectionOffset = new(0, 40),
        });
    }

    public Player(Game game)
    {
        this.game = game;

        var layer = game.GetRenderLayer(Layer.Player);

        var animation = GetAnimation();
        sprite = layer.SpriteFactory!.CreateAnimated();
        sprite.TextureOffset = animation.FirstFrameOffset;
        sprite.TextureSize = animation.FrameSize;
        sprite.Position = new(0, 0);
        sprite.Size = new(24, 20);
        sprite.CurrentFrameIndex = 0;
        sprite.FrameCount = (int)animation.FrameCount;
        sprite.PaletteIndex = 0;
        sprite.Visible = true;

        layer = game.GetRenderLayer(Layer.Outfit);

        outfitSprite = layer.SpriteFactory!.CreateAnimated();
        outfitSprite.TextureOffset = sprite.TextureOffset;
        outfitSprite.TextureSize = sprite.TextureSize;
        outfitSprite.Position = sprite.Position;
        outfitSprite.Size = sprite.Size;
        outfitSprite.CurrentFrameIndex = sprite.CurrentFrameIndex;
        outfitSprite.FrameCount = sprite.FrameCount;
        outfitSprite.PaletteIndex = 0;
        outfitSprite.Visible = sprite.Visible;
    }

    private Animation GetAnimation()
    {
        return animations[state];
    }

    public void Update(long ticks)
    {
        if (lastAnimationTicks == 0)
            lastAnimationTicks = ticks;

        if (sprite.FrameCount > 1)
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
        var animation = GetAnimation();

        outfitSprite.TextureOffset = sprite.TextureOffset = animation.FirstFrameOffset + ((int)direction * animation.DirectionOffset!.Value);
        outfitSprite.TextureSize = sprite.TextureSize = animation.FrameSize;
        outfitSprite.CurrentFrameIndex = sprite.CurrentFrameIndex = 0;
        outfitSprite.FrameCount = sprite.FrameCount = (int)animation.FrameCount;
    }
}
