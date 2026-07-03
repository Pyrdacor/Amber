using Amber.Common;
using Amber.Renderer.Common;
using AmberIsland.Game.Extensions;
using AmberIsland.Game.Map;
using AmberIsland.GameData;

namespace AmberIsland.Game;

internal class Player : CombatMapActor
{
    private const int TicksPerAnimationFrame = 6;
    private const float WalkSpeed = 60.0f;
    private const float RunSpeed = 120.0f;
    private readonly Game game;
    private readonly ISequencedSprite sprite;
    private readonly ISequencedSprite outfitSprite;
    private static readonly Dictionary<PlayerState, PlayerStateSprites> stateSprites = [];
    private static readonly Dictionary<PlayerState, Animation> animations = [];
    private PlayerState state = PlayerState.Idle;
    private long playerTicks = 0;
    private long lastAnimationTicks = 0;
    private long lastAttackTicks = 0;
    private float moveSpeed = WalkSpeed;
    private long attackTicks = 60; // TODO
    private int ticksPerAnimationFrame = TicksPerAnimationFrame;

    public event Action? Attack;

    public PlayerState CurrentState
    {
        get => state;
        set
        {
            if (state == value)
                return;

            state = value;
            lastAnimationTicks = 0;
            InitAnimation();

            moveSpeed = state switch
            {
                PlayerState.Walking => WalkSpeed,
                PlayerState.Running => RunSpeed,
                _ => 0.0f
            };

            ticksPerAnimationFrame = state switch
            {
                PlayerState.SwingingForth => Math.Max(1, (int)(attackTicks / sprite.FrameIndices.Length)),
                // TODO: Add other states that have different animation frame rates
                _ => TicksPerAnimationFrame
            };
        }
    }

    public float MoveSpeed => moveSpeed;

    // TODO: Avoid magic numbers
    public override Rect CollisionArea => new(Area.Position.X + 4, Area.Position.Y + 1, sprite.Size.Width - 8, 15);
    private PlayerStateSprites StateSprites => stateSprites[state];

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
    public int AttackRange => 1; // TODO
    public int VisionRange => 5; // TODO

    public Player(Game game) : base(game, ActorType.Player)
    {
        this.game = game;

        SetupAnimations(game);

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
        Size = new(96, 96);
        Visible = true;
    }

    private static void SetupAnimations(Game game)
    {
        if (animations.Count > 0)
            return;

        var playerGraphics = game.GameData.GetPlayerSpriteSheet();
        var frameSize = PlayerStateSprites.FrameSize;

        foreach (var stateSprite in playerGraphics.StateSprites)
        {
            stateSprites.Add(stateSprite.State, stateSprite);
            animations.Add(stateSprite.State, new()
            {
                FrameSize = frameSize,
                FrameIndices = stateSprite.FrameIndices.Select(i => (uint)i).ToArray(),
                DirectionOffset = new(0, frameSize.Height),
            });
        }
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
        if (lastAttackTicks == 0 || CurrentState != PlayerState.SwingingForth) // TODO
            lastAttackTicks = playerTicks;

        if (sprite.FrameIndices.Length > 1)
        {
            long elapsed = playerTicks - lastAnimationTicks;

            while (elapsed >= ticksPerAnimationFrame)
            {
                sprite.CurrentFrameIndex += 1;
                outfitSprite.CurrentFrameIndex = sprite.CurrentFrameIndex;
                elapsed -= ticksPerAnimationFrame;
            }

            lastAnimationTicks = playerTicks - elapsed;
        }

        if (CurrentState == PlayerState.SwingingForth) // TODO
        {
            while ((playerTicks - lastAttackTicks) >= attackTicks)
            {
                lastAttackTicks += attackTicks;
                Attack?.Invoke();
            }

            sprite.CurrentFrameIndex = 0;
            outfitSprite.CurrentFrameIndex = 0;
            lastAnimationTicks = playerTicks;
        }
    }

    private void InitAnimation()
    {
        SetFrameIndicesAndOrigin(resetFrameIndex: true);

        var stateSprites = StateSprites;
        var frameSize = PlayerStateSprites.FrameSize;
        var origin = new Position(stateSprites.OffsetX * frameSize.Width, stateSprites.OffsetY * frameSize.Height);
        var newOrigin = origin + (int)VisualDirection * (GetAnimation().DirectionOffset ?? Amber.Common.Position.Zero);
        outfitSprite.FrameOrigin = sprite.FrameOrigin = newOrigin;
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

        lastAnimationTicks = 0;

        var animation = GetAnimation();
        var stateSprites = StateSprites;
        var frameSize = PlayerStateSprites.FrameSize;
        var origin = new Position(stateSprites.OffsetX * frameSize.Width, stateSprites.OffsetY * frameSize.Height);
        var newOrigin = origin + (int)newDirection * (animation.DirectionOffset ?? Amber.Common.Position.Zero);
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
