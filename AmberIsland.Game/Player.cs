using Amber.Common;
using Amber.Renderer.Common;
using AmberIsland.Game.Extensions;
using AmberIsland.Game.Map;
using AmberIsland.GameData;

namespace AmberIsland.Game;

internal class Player : CombatMapActor
{
    public enum State
    {
        Idle,
        Walking,
        Running
    }

    // TODO: diagonal speed is higher (sqrt(2) * speed instead of 1 * speed)
    private const int TicksPerAnimationFrame = 6;
    private readonly Game game;
    private readonly ISequencedSprite sprite;
    private readonly ISequencedSprite outfitSprite;
    private static readonly Dictionary<State, Animation> animations = [];
    private State state = State.Idle;
    private long playerTicks = 0;
    private long lastAnimationTicks = 0;

    public uint MoveSpeed { get; set; } = 2; // TODO

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

    // TODO: Avoid magic numbers
    public override Rect CollisionArea => new(Area.Position.X + 4, Area.Position.Y + 1, sprite.Size.Width - 8, 15);

    private protected override uint TotalPhysicalMinDamage => 0; // TODO
    private protected override uint TotalPhysicalMaxDamage => 3; // TODO
    private protected override uint TotalMagicalMinDamage => 0; // TODO
    private protected override uint TotalMagicalMaxDamage => 0; // TODO
    private protected override uint TotalPhysicalDefense => 0; // TODO
    private protected override uint TotalMagicalDefense => 0; // TODO
    private protected override uint TotalPhysicalDamageReduction => 0; // TODO
    private protected override uint TotalMagicalDamageReduction => 0; // TODO
    private protected override uint TotalHit => 0; // TODO
    private protected override uint TotalDodge => 0; // TODO
    private protected override uint Level => 1; // TODO
    private protected override Element AttackElement => Element.None; // TODO
    private protected override Element DefendElement => Element.None; // TODO

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

    public Player(Game game) : base(game, ActorType.Player)
    {
        this.game = game;

        var layer = game.GetRenderLayer(Layer.Player);

        sprite = layer.SpriteFactory!.CreateSequenced();
        sprite.PaletteIndex = 0;
        sprite.Visible = false;

        layer = game.GetRenderLayer(Layer.Outfit);

        outfitSprite = layer.SpriteFactory!.CreateSequenced();
        outfitSprite.PaletteIndex = 0;
        outfitSprite.Visible = sprite.Visible;

        SetFrameIndicesAndOrigin(resetFrameIndex: true);

        Position = new(0, 0);
        Size = new(72, 60);
        Visible = true;
    }

    private Animation GetAnimation()
    {
        return animations[state];
    }

    private protected override void UpdateActor(long elapsedTicks)
    {
        base.UpdateActor(elapsedTicks);

        playerTicks += elapsedTicks;

        if (lastAnimationTicks == 0)
            lastAnimationTicks = playerTicks;

        if (sprite.FrameIndices.Length > 1)
        {
            long elapsed = playerTicks - lastAnimationTicks;

            while (elapsed >= TicksPerAnimationFrame)
            {
                sprite.CurrentFrameIndex += 1;
                outfitSprite.CurrentFrameIndex = sprite.CurrentFrameIndex;
                elapsed -= TicksPerAnimationFrame;
            }

            lastAnimationTicks = playerTicks - elapsed;
        }
    }

    private void InitAnimation()
    {
        SetFrameIndicesAndOrigin(resetFrameIndex: true);
    }

    private void SetFrameIndicesAndOrigin(bool resetFrameIndex)
    {
        sprite.SetFrameIndicesAndOrigin(GetAnimation, 1u, VisualDirection, resetFrameIndex);

        outfitSprite.TextureSize = sprite.TextureSize;
        outfitSprite.FrameOrigin = sprite.FrameOrigin;
        outfitSprite.FrameIndices = sprite.FrameIndices;
        outfitSprite.CurrentFrameIndex = sprite.CurrentFrameIndex;
    }

    private protected override void PositionChanged(Vector oldPosition, Vector newPosition, Size oldSize, Size newSize)
    {
        base.PositionChanged(oldPosition, newPosition, oldSize, newSize);

        if (sprite == null)
            return;

        var position = Position.Round();

        outfitSprite.Position = sprite.Position = position - MapOffset;
        outfitSprite.Size = sprite.Size = Size;

        game.State.PlayerPosition = position;
    }

    private protected override void VisualDirectionChanged(Direction oldDirection, Direction newDirection)
    {
        base.VisualDirectionChanged(oldDirection, newDirection);

        int directionDiff = newDirection - oldDirection;

        lastAnimationTicks = 0;

        var animation = GetAnimation();
        var newOrigin = sprite.FrameOrigin + directionDiff * (animation.DirectionOffset ?? Amber.Common.Position.Zero);
        outfitSprite.FrameOrigin = sprite.FrameOrigin = newOrigin;
        outfitSprite.CurrentFrameIndex = sprite.CurrentFrameIndex = 0;

        game.State.PlayerDirection = VisualDirection;
    }

    private protected override void VisibilityChanged(bool oldVisibility, bool newVisibility)
    {
        base.VisibilityChanged(oldVisibility, newVisibility);

        outfitSprite.Visible = sprite.Visible = Visible && VisibleOnMap;
    }

    private protected override void MapVisibilityChanged(bool oldMapVisibility, bool newMapVisibility)
    {
        base.MapVisibilityChanged(oldMapVisibility, newMapVisibility);

        outfitSprite.Visible = sprite.Visible = Visible && VisibleOnMap;
    }

    private protected override void MapOffsetChanged(Position oldMapOffset, Position newMapOffset)
    {
        base.MapOffsetChanged(oldMapOffset, newMapOffset);

        outfitSprite.Position = sprite.Position = Position.Round() - MapOffset;
    }
}
