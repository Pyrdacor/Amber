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

    public enum Direction
    {
        Down,
        Up,
        Right,
        Left
    }

    private const int HeightOverGround = 32;
    private const int TicksPerAnimationFrame = 10;
    private readonly Game game;
    private readonly IAnimatedSprite sprite;
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
            sprite.TextureOffset = animation.FirstFrameOffset + ((int)direction * animation.DirectionOffset!.Value);
            sprite.CurrentFrameIndex = 0;
        }
    }

    public Position Position
    {
        get => sprite.Position;
        set => sprite.Position = value;
    }

    static Player()
    {
        var frameSize = new Size(32, 40);

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
            FirstFrameOffset = new(192, 160),
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
        sprite.CurrentFrameIndex = 0;
        sprite.FrameCount = (int)animation.FrameCount;
        sprite.PaletteIndex = 0;
        sprite.Visible = true;
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
                elapsed -= TicksPerAnimationFrame;
            }
        }
    }

    private void InitAnimation()
    {
        var animation = GetAnimation();

        sprite.TextureOffset = animation.FirstFrameOffset + ((int)direction * animation.DirectionOffset!.Value);
        sprite.TextureSize = animation.FrameSize;
        sprite.CurrentFrameIndex = 0;
        sprite.FrameCount = (int)animation.FrameCount;
    }
}
