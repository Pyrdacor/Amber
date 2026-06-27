using Amber.Common;
using Amber.Renderer.Common;
using AmberIsland.Game.Extensions;
using AmberIsland.Game.Screens;
using AmberIsland.GameData;

namespace AmberIsland.Game;

internal class MapProjectile : MapActor
{
    private readonly Game game;
    private readonly MapScreen mapScreen;
    private readonly uint projectileIndex;
    private readonly MapActor source;
    private float remainingTargetDistance = 0.0f;
    private ProjectileState currentState = ProjectileState.Flying;
    private long lastMoveTicks = 0;
    private long lastAttackTicks = 0;
    private long lastAnimationTicks = 0;
    private long projectileTicks = 0;
    private readonly ISequencedSprite sprite;
    private readonly Dictionary<ProjectileState, Animation> animations = [];
    private float travelledDistance = 0.0f;

    private ProjectileState CurrentState
    {
        get => currentState;
        set
        {
            if (currentState == value)
                return;

            currentState = value;
        }
    }

    public MapProjectile(Game game, MapScreen mapScreen, MapActor source, uint projectileIndex, Position position, Vector direction)
        : base(game, ActorType.Projectile)
    {
        this.game = game;
        this.mapScreen = mapScreen;
        this.projectileIndex = projectileIndex;
        this.source = source;

        animations = game.GameData.GetProjectileAnimations(projectileIndex);

        var projectile = GetProjectileData();
        var layer = game.GetRenderLayer(Layer.Projectiles);
        var animation = GetAnimation();
        sprite = layer.SpriteFactory!.CreateSequenced();
        sprite.TextureSize = new(animation.FrameSize.Width, animation.FrameSize.Height);
        sprite.BaseLineOffset = 0; // TODO
        sprite.PaletteIndex = (byte)mapScreen.GetProjectilePaletteIndices(projectileIndex)[projectile.PaletteIndex];
        sprite.Visible = false;

        var scaleFactor = projectile.ScaleFactor == 0 ? 16 : projectile.ScaleFactor;

        Direction = direction;

        sprite.SetFrameIndicesAndOrigin(GetAnimation, projectileIndex, VisualDirection, resetFrameIndex: true);

        Size = new(MathUtil.Round(scaleFactor * animation.FrameSize.Width / 16.0f), MathUtil.Round(scaleFactor * animation.FrameSize.Height / 16.0f));
        Position = new(position - new Position(Size.Width / 2, Size.Height / 2));
        Visible = true;

        remainingTargetDistance = MaxTravelDistance(projectile);

        CheckChasing(projectile);
    }

    private Projectile GetProjectileData() => game.GameData.GetProjectile(projectileIndex);

    private Animation GetAnimation(ProjectileState state)
    {
        if (animations.TryGetValue(state, out var animation))
            return animation;

        // Fallbacks
        return state switch
        {
            ProjectileState.Flying => default, // Not good :(
            ProjectileState.Chasing => GetAnimation(ProjectileState.Flying),
            ProjectileState.Die => GetAnimation(ProjectileState.Exploding),
            _ => GetAnimation(ProjectileState.Flying)
        };
    }

    private Animation GetAnimation() => GetAnimation(currentState);

    private float MaxTravelDistance(Projectile projectile) => projectile.MaxTravelDistance * MapScreen.TileWidth;

    private protected override void VisibilityChanged(bool oldVisibility, bool newVisibility)
    {
        base.VisibilityChanged(oldVisibility, newVisibility);

        sprite.Visible = Visible && VisibleOnMap;
    }

    private protected override void MapVisibilityChanged(bool oldMapVisibility, bool newMapVisibility)
    {
        base.MapVisibilityChanged(oldMapVisibility, newMapVisibility);

        sprite.Visible = Visible && VisibleOnMap;
    }

    private protected override void MapOffsetChanged(Position oldMapOffset, Position newMapOffset)
    {
        base.MapOffsetChanged(oldMapOffset, newMapOffset);

        sprite.Position = Position.Round() - MapOffset;
    }

    private protected override void PositionChanged(Vector oldPosition, Vector newPosition, Size oldSize, Size newSize)
    {
        base.PositionChanged(oldPosition, newPosition, oldSize, newSize);

        if (sprite == null)
            return;

        sprite.Position = Position.Round() - MapOffset;
        sprite.Size = Size;
    }

    private protected override void VisualDirectionChanged(Direction oldDirection, Direction newDirection)
    {
        base.VisualDirectionChanged(oldDirection, newDirection);

        int directionDiff = newDirection - oldDirection;

        lastAnimationTicks = 0;

        var animation = GetAnimation();
        var newOrigin = sprite.FrameOrigin + directionDiff * (animation.DirectionOffset ?? Amber.Common.Position.Zero);
        sprite.MirrorX = animation.DirectionOffset == null && (Direction.X < 0 || Direction.X == 0 && Direction.Y < 0);
        sprite.FrameOrigin = newOrigin;
        sprite.CurrentFrameIndex = 0;
    }

    private protected override void DirectionChanged(Vector oldDirection, Vector newDirection)
    {
        base.DirectionChanged(oldDirection, newDirection);

        var animation = GetAnimation();
        sprite.MirrorX = animation.DirectionOffset == null && (Direction.X < 0 || Direction.X == 0 && Direction.Y < 0);
    }

    private protected override void UpdateActor(long elapsedTicks)
    {
        base.UpdateActor(elapsedTicks);

        projectileTicks += elapsedTicks;

        if (lastAnimationTicks == 0)
            lastAnimationTicks = projectileTicks;

        if (lastMoveTicks == 0)
            lastMoveTicks = projectileTicks;

        if (sprite.FrameIndices.Length > 1)
        {
            long elapsed = projectileTicks - lastAnimationTicks;

            if (elapsed > 0)
            {
                double elapsedMinutes = Game.TicksToMinutes(elapsed);
                double framesPerMinute = GetAnimation().FramesPerMinute;
                int elapsedFrames = MathUtil.Floor(framesPerMinute * elapsedMinutes);

                sprite.CurrentFrameIndex += elapsedFrames;
                elapsed -= Game.MinutesToTicks(elapsedFrames / framesPerMinute);

                lastAnimationTicks = projectileTicks - elapsed;
            }
        }

        switch (currentState)
        {
            case ProjectileState.Flying:
                HandleFlyingState();
                break;
            case ProjectileState.Chasing:
                HandleChasingState();
                break;
            case ProjectileState.Exploding:
                HandleExplodingState();
                break;
            case ProjectileState.Die:
                HandleDieState();
                break;
        }        
    }

    private void HandleFlyingState()
    {
        var projectile = GetProjectileData();

        if (CheckForCollision(projectile))
            return;

        long moveTicks = projectileTicks - lastMoveTicks;
        double moveTickMinutes = Game.TicksToMinutes(moveTicks);
        float movedPixels = Math.Min((float)(moveTickMinutes * projectile.MoveSpeed), remainingTargetDistance);

        if (!float.IsNaN(movedPixels) && movedPixels > 0)
        {
            Move(movedPixels);
            remainingTargetDistance -= movedPixels;
            travelledDistance += movedPixels;

            if (remainingTargetDistance <= 0.0f)
            {
                // Done
                remainingTargetDistance = 0.0f;
                lastMoveTicks = projectileTicks;

                if (travelledDistance >= MaxTravelDistance(projectile))
                {
                    if (projectile.Flags.HasFlag(ProjectileFlags.ExplodeOnImpact))
                        CurrentState = ProjectileState.Exploding;
                    else
                        CurrentState = ProjectileState.Die;

                    return;
                }

                if (!CheckChasing(projectile))
                    remainingTargetDistance = MaxTravelDistance(projectile) - travelledDistance;
            }
            else
            {
                var moveTime = movedPixels / projectile.MoveSpeed;
                moveTicks -= Game.MinutesToTicks(moveTime);
                lastMoveTicks = projectileTicks + moveTicks;
            }
        }
    }

    private bool CheckForCollision(Projectile projectile)
    {
        return false;
        if (source.Type == ActorType.Player)
        {
            // TODO: Check collision with monsters and other stuff (based on flags)
        }
        else if (GetDistanceToPlayer() * MapScreen.TileWidth < projectile.CollisionRadius) // TODO: include player's collision radius?
        {
            if (projectile.Flags.HasFlag(ProjectileFlags.ExplodeOnImpact))
                CurrentState = ProjectileState.Exploding;
            else
                CurrentState = ProjectileState.Die;

            return true;
        }

        return false;
    }

    private bool CheckChasing(Projectile projectile)
    {
        if (source.Type == ActorType.Player || !projectile.Flags.HasFlag(ProjectileFlags.ChasePlayer))
            return false;

        if (!IsPlayerInSightRange())
            return false;

        CurrentState = ProjectileState.Chasing;

        return true;
    }

    private void HandleChasingState()
    {
        // We just determine the direction towards the player and move a full tile.
        // Then we might chase again if needed.
        var projectile = GetProjectileData();
        Direction = (game.Player.Center - Center).Normalized();
        remainingTargetDistance = Math.Min(MapScreen.TileWidth, MaxTravelDistance(projectile) - travelledDistance);
        CurrentState = ProjectileState.Flying;
    }

    private void HandleExplodingState()
    {
        // TODO
    }

    private void HandleDieState()
    {
        // TODO: play animation if exists
        Visible = false;
        mapScreen.RemoveActor(this);
    }

    private float GetDistanceToPlayer()
    {
        return (game.Player.Center - Center).Length() / MapScreen.TileWidth;
    }

    private bool IsPlayerInSightRange()
    {
        var projectile = GetProjectileData();

        if (GetDistanceToPlayer() * MapScreen.TileWidth <= projectile.VisionRange)
            return true; // TODO: blocked sight

        return false;
    }
}
