using Amber.Common;
using Amber.Renderer.Common;
using AmberIsland.Game.Extensions;
using AmberIsland.Game.Map;
using AmberIsland.GameData;

namespace AmberIsland.Game;

internal class Player : CombatMapActor
{
    private const int TicksPerAnimationFrame = 6;
    private const float WalkSpeed = 90.0f;
    private const float RunSpeed = 150.0f;
    private readonly Game game;
    private readonly ISequencedSprite sprite;
    private readonly ISequencedSprite outfitSprite;
    //private readonly ISequencedSprite cloakSprite;
    //private readonly ISequencedSprite faceAssetSprite;
    private readonly ISequencedSprite hairSprite;
    private readonly ISequencedSprite hatSprite;
    private readonly ISequencedSprite primaryToolSprite;
    private readonly ISequencedSprite secondaryToolSprite;
    private static readonly Dictionary<PlayerState, PlayerStateSprites> playerStateSprites = [];
    private static readonly Dictionary<PlayerState, PlayerStateSprites> outfitStateSprites = [];
    //private static readonly Dictionary<PlayerState, PlayerStateSprites> cloakStateSprites = [];
    //private static readonly Dictionary<PlayerState, PlayerStateSprites> faceAssetStateSprites = [];
    private static readonly Dictionary<PlayerState, PlayerStateSprites> hairStateSprites = [];
    private static readonly Dictionary<PlayerState, PlayerStateSprites> hatStateSprites = [];
    private static readonly Dictionary<PlayerState, PlayerStateSprites> primaryToolStateSprites = [];
    private static readonly Dictionary<PlayerState, PlayerStateSprites> secondaryToolStateSprites = [];
    private static readonly Dictionary<PlayerState, Animation> animations = [];
    private PlayerState state = PlayerState.Idle;
    private PlayerStateSpriteVariantType outfitVariant = PlayerStateSpriteVariantType.Outfit_Robe;
    //private PlayerStateSpriteVariantType cloakVariant = PlayerStateSpriteVariantType.Cloak_Normal;
    //private PlayerStateSpriteVariantType faceAssetVariant = PlayerStateSpriteVariantType.Face_Normal;
    private PlayerStateSpriteVariantType hairVariant = PlayerStateSpriteVariantType.Hair_Bob;
    private PlayerStateSpriteVariantType hatVariant = PlayerStateSpriteVariantType.Hat_Cap;
    private PlayerStateSpriteVariantType primaryToolVariant = PlayerStateSpriteVariantType.PTool_Sword;
    private PlayerStateSpriteVariantType secondaryToolVariant = PlayerStateSpriteVariantType.STool_WoodenShield;
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
    private PlayerStateSprites StateSprites => playerStateSprites[state];

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

        void CreateLayerSprite(Layer layer, out ISequencedSprite sprite)
        {
            var renderLayer = game.GetRenderLayer(layer);

            sprite = renderLayer.SpriteFactory!.CreateSequenced();
            sprite.PaletteIndex = 0; // TODO: Allow changing palettes
            sprite.Visible = this.sprite.Visible;
        }

        CreateLayerSprite(Layer.Outfit, out outfitSprite);
        // TODO
        //CreateLayerSprite(Layer.Cloak, out cloakSprite);
        //CreateLayerSprite(Layer.FaceAsset, out faceAssetSprite);
        CreateLayerSprite(Layer.Hair, out hairSprite);
        CreateLayerSprite(Layer.Hats, out hatSprite);
        CreateLayerSprite(Layer.PrimaryTool, out primaryToolSprite);
        CreateLayerSprite(Layer.SecondaryTool, out secondaryToolSprite);

        InitAnimation();

        Position = new(0, 0);
        Size = new(96, 96);
        Visible = true;
    }

    private static void SetupAnimations(Game game)
    {
        if (animations.Count > 0)
            return;

        var playerSpriteSheet = game.GameData.GetPlayerSpriteSheet();
        var outfitSpriteSheet = game.GameData.GetOutfitSpriteSheet();
        // TODO
        //var cloakSpriteSheet = game.GameData.GetCloakSpriteSheet();
        //var faceAssetSpriteSheet = game.GameData.GetFaceAssetSpriteSheet();
        var hairSpriteSheet = game.GameData.GetHairSpriteSheet();
        var hatSpriteSheet = game.GameData.GetHatSpriteSheet();
        var primaryToolSpriteSheet = game.GameData.GetPrimaryToolSpriteSheet();
        var secondaryToolSpriteSheet = game.GameData.GetSecondaryToolSpriteSheet();
        var frameSize = PlayerStateSprites.FrameSize;

        foreach (var stateSprite in playerSpriteSheet.StateSprites)
        {
            playerStateSprites.Add(stateSprite.State, stateSprite);
            animations.Add(stateSprite.State, new()
            {
                FrameSize = frameSize,
                FrameIndices = stateSprite.FrameIndices.Select(i => (uint)i).ToArray(),
                DirectionOffset = new(0, frameSize.Height),
            });
        }

        foreach (var stateSprite in outfitSpriteSheet.StateSprites)
        {
            outfitStateSprites.Add(stateSprite.State, stateSprite);
        }

        // TODO
        /*foreach (var stateSprite in cloakSpriteSheet.StateSprites)
        {
            cloakStateSprites.Add(stateSprite.State, stateSprite);
        }

        foreach (var stateSprite in faceAssetSpriteSheet.StateSprites)
        {
            faceAssetStateSprites.Add(stateSprite.State, stateSprite);
        }*/

        foreach (var stateSprite in hairSpriteSheet.StateSprites)
        {
            hairStateSprites.Add(stateSprite.State, stateSprite);
        }

        foreach (var stateSprite in hatSpriteSheet.StateSprites)
        {
            hatStateSprites.Add(stateSprite.State, stateSprite);
        }

        foreach (var stateSprite in primaryToolSpriteSheet.StateSprites)
        {
            primaryToolStateSprites.Add(stateSprite.State, stateSprite);
        }

        foreach (var stateSprite in secondaryToolSpriteSheet.StateSprites)
        {
            secondaryToolStateSprites.Add(stateSprite.State, stateSprite);
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
                //cloakSprite.CurrentFrameIndex = sprite.CurrentFrameIndex;
                //faceAssetSprite.CurrentFrameIndex = sprite.CurrentFrameIndex;
                hairSprite.CurrentFrameIndex = sprite.CurrentFrameIndex;
                hatSprite.CurrentFrameIndex = sprite.CurrentFrameIndex;
                primaryToolSprite.CurrentFrameIndex = sprite.CurrentFrameIndex;
                secondaryToolSprite.CurrentFrameIndex = sprite.CurrentFrameIndex;
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

            /*sprite.CurrentFrameIndex = 0;
            outfitSprite.CurrentFrameIndex = 0;
            lastAnimationTicks = playerTicks;*/
        }
    }

    private void InitAnimation()
    {
        SetFrameIndicesAndOrigin(resetFrameIndex: true);

        var stateSprites = StateSprites;
        var frameSize = PlayerStateSprites.FrameSize;
        var origin = new Position(stateSprites.OffsetX * frameSize.Width, stateSprites.OffsetY * frameSize.Height);
        var newOrigin = origin + (int)VisualDirection * (GetAnimation().DirectionOffset ?? Amber.Common.Position.Zero);
        sprite.FrameOrigin = newOrigin;

        UpdateFrameOrigin(outfitSprite, outfitStateSprites, outfitVariant);
        // TODO
        //UpdateFrameOrigin(cloakSprite, cloakStateSprites, cloakVariant);
        //UpdateFrameOrigin(faceAssetSprite, faceAssetStateSprites, faceAssetVariant);
        UpdateFrameOrigin(hairSprite, hairStateSprites, hairVariant);
        UpdateFrameOrigin(hatSprite, hatStateSprites, hatVariant);
        UpdateFrameOrigin(primaryToolSprite, primaryToolStateSprites, primaryToolVariant);
        UpdateFrameOrigin(secondaryToolSprite, secondaryToolStateSprites, secondaryToolVariant);

        void UpdateFrameOrigin(ISequencedSprite sprite, Dictionary<PlayerState, PlayerStateSprites> stateSprites, PlayerStateSpriteVariantType variantType)
        {
            sprite.FrameOrigin = newOrigin;
            sprite.FrameOrigin += new Position(0, stateSprites[state].Variants[variantType].OffsetY);
        }
    }

    private void SetFrameIndicesAndOrigin(bool resetFrameIndex)
    {
        sprite.SetFrameIndicesAndOrigin(GetAnimation, 1u, VisualDirection, resetFrameIndex);

        void CopySpriteData(ISequencedSprite sequencedSprite)
        {
            sequencedSprite.TextureSize = sprite.TextureSize;
            sequencedSprite.FrameOrigin = sprite.FrameOrigin;
            sequencedSprite.FrameIndices = sprite.FrameIndices;
            sequencedSprite.CurrentFrameIndex = sprite.CurrentFrameIndex;
        }

        CopySpriteData(outfitSprite);
        // TODO
        //CopySpriteData(cloakSprite);
        //CopySpriteData(faceAssetSprite);
        CopySpriteData(hairSprite);
        CopySpriteData(hatSprite);
        CopySpriteData(primaryToolSprite);
        CopySpriteData(secondaryToolSprite);
    }

    private protected override void PositionChanged(Vector oldPosition, Vector newPosition, Size oldSize, Size newSize)
    {
        base.PositionChanged(oldPosition, newPosition, oldSize, newSize);

        if (sprite == null)
            return;

        var position = Position.Round();

        sprite.Position = position - MapOffset;
        sprite.Size = Size;

        UpdateMetrics(outfitSprite);
        // TODO
        //UpdateMetrics(cloakSprite);
        //UpdateMetrics(faceAssetSprite);
        UpdateMetrics(hairSprite);
        UpdateMetrics(hatSprite);
        UpdateMetrics(primaryToolSprite);
        UpdateMetrics(secondaryToolSprite);

        void UpdateMetrics(ISequencedSprite sequencedSprite)
        {
            sequencedSprite.Position = sprite.Position;
            sequencedSprite.Size = sprite.Size;
        }

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
        sprite.FrameOrigin = newOrigin;
        sprite.CurrentFrameIndex = 0;

        void UpdateFrameInfo(ISequencedSprite sprite, Dictionary<PlayerState, PlayerStateSprites> stateSprites, PlayerStateSpriteVariantType variantType)
        {
            sprite.FrameOrigin = newOrigin;
            sprite.FrameOrigin += new Position(0, stateSprites[state].Variants[variantType].OffsetY);
            sprite.CurrentFrameIndex = this.sprite.CurrentFrameIndex;
        }

        UpdateFrameInfo(outfitSprite, outfitStateSprites, outfitVariant);
        // TODO
        //UpdateFrameInfo(cloakSprite, cloakStateSprites, cloakVariant);
        //UpdateFrameInfo(faceAssetSprite, faceAssetStateSprites, faceAssetVariant);
        UpdateFrameInfo(hairSprite, hairStateSprites, hairVariant);
        UpdateFrameInfo(hatSprite, hatStateSprites, hatVariant);
        UpdateFrameInfo(primaryToolSprite, primaryToolStateSprites, primaryToolVariant);
        UpdateFrameInfo(secondaryToolSprite, secondaryToolStateSprites, secondaryToolVariant);

        game.State.PlayerDirection = VisualDirection;
    }

    private protected override void VisibilityChanged(bool oldVisibility, bool newVisibility)
    {
        base.VisibilityChanged(oldVisibility, newVisibility);

        sprite.Visible = Visible && VisibleOnMap;
        outfitSprite.Visible = sprite.Visible;
        // TODO
        //cloakSprite.Visible = sprite.Visible;
        //faceAssetSprite.Visible = sprite.Visible;
        hairSprite.Visible = sprite.Visible;
        hatSprite.Visible = sprite.Visible;
        primaryToolSprite.Visible = sprite.Visible;
        secondaryToolSprite.Visible = sprite.Visible;
    }

    private protected override void MapVisibilityChanged(bool oldMapVisibility, bool newMapVisibility)
    {
        base.MapVisibilityChanged(oldMapVisibility, newMapVisibility);

        sprite.Visible = Visible && VisibleOnMap;
        outfitSprite.Visible = sprite.Visible;
        // TODO
        //cloakSprite.Visible = sprite.Visible;
        //faceAssetSprite.Visible = sprite.Visible;
        hairSprite.Visible = sprite.Visible;
        hatSprite.Visible = sprite.Visible;
        primaryToolSprite.Visible = sprite.Visible;
        secondaryToolSprite.Visible = sprite.Visible;
    }

    private protected override void MapOffsetChanged(Position oldMapOffset, Position newMapOffset)
    {
        base.MapOffsetChanged(oldMapOffset, newMapOffset);

        sprite.Position = Position.Round() - MapOffset;
        outfitSprite.Position = sprite.Position;
        // TODO
        //cloakSprite.Position = sprite.Position;
        //faceAssetSprite.Position = sprite.Position;
        hairSprite.Position = sprite.Position;
        hatSprite.Position = sprite.Position;
        primaryToolSprite.Position = sprite.Position;
        secondaryToolSprite.Position = sprite.Position;
    }
}
