using Amber.Common;
using Amber.Renderer.Common;
using AmberIsland.Game.Extensions;
using AmberIsland.Game.Screens;
using AmberIsland.GameData;

namespace AmberIsland.Game;

internal class MapMonster : MapActor
{
    private readonly Game game;
    private readonly MapScreen mapScreen;
    private readonly uint monsterIndex;
    // TODO: conditions
    private uint currentHitPoints = 0;
    private Position? targetPosition = null;
    private Direction spriteDirection = AmberIsland.GameData.Direction.Down;
    private MonsterState currentState = MonsterState.Idle;
    private long nextDecisionTicks = 0;
    private long lastMoveTicks = 0;
    private long lastAttackTicks = 0;
    private long lastAnimationTicks = 0;
    private long monsterTicks = 0;
    private readonly ISequencedSprite sprite;
    private readonly Dictionary<MonsterState, Animation> animations = [];

    private MonsterState CurrentState
    {
        get => currentState;
        set
        {
            if (currentState == value)
                return;

            currentState = value;

            if (currentState == MonsterState.Idle)
                SetupNextDecision();
            else if (currentState == MonsterState.Walking || currentState == MonsterState.Chasing || currentState == MonsterState.Fleeing)
            {
                lastMoveTicks = 0;
                var direction = Direction = (new Vector(targetPosition ?? Amber.Common.Position.Zero) - Center).Normalized();

                if (direction.Length() == 0)
                {
                    targetPosition = null;
                    return;
                }

                if (Math.Abs(direction.X) > Math.Abs(direction.Y))
                {
                    if (direction.X < 0)
                        CurrentDirection = GameData.Direction.Left;
                    else
                        CurrentDirection = GameData.Direction.Right;
                }
                else
                {
                    if (direction.Y < 0)
                        CurrentDirection = GameData.Direction.Up;
                    else
                        CurrentDirection = GameData.Direction.Down;
                }
            }
        }
    }

    public MapMonster(Game game, MapScreen mapScreen, uint monsterIndex, Position position, Direction direction)
        : base(game, ActorType.Monster)
    {
        this.game = game;
        this.mapScreen = mapScreen;
        this.monsterIndex = monsterIndex;

        animations = game.GameData.GetMonsterAnimations(monsterIndex);

        var monster = GetMonsterData();
        var layer = game.GetRenderLayer(Layer.Monsters);
        var animation = GetAnimation();
        sprite = layer.SpriteFactory!.CreateSequenced();
        sprite.TextureSize = new(64, 64);
        sprite.MirrorX = direction == GameData.Direction.Left || direction == GameData.Direction.Up; // TODO
        sprite.BaseLineOffset = 0; // TODO
        sprite.Visible = false;

        sprite.SetFrameIndicesAndOrigin(GetAnimation, direction, resetFrameIndex: true);

        Position = new(position);
        Size = new(32, 32);
        Visible = true;
    }

    public Direction CurrentDirection
    {
        get => spriteDirection;
        set
        {
            if (spriteDirection == value)
                return;

            int directionDiff = value - spriteDirection;

            spriteDirection = value;
            lastAnimationTicks = 0;

            var animation = GetAnimation();
            var newOrigin = sprite.FrameOrigin + directionDiff * (animation.DirectionOffset ?? Amber.Common.Position.Zero);
            sprite.MirrorX = animation.DirectionOffset == null && (spriteDirection == GameData.Direction.Left || spriteDirection == GameData.Direction.Up);
            sprite.FrameOrigin = newOrigin;
            sprite.CurrentFrameIndex = 0;
        }
    }

    private Monster GetMonsterData() => game.GameData.GetMonster(monsterIndex);

    private Animation GetAnimation(MonsterState state)
    {
        if (animations.TryGetValue(state, out var animation))
            return animation;

        // Fallbacks
        return state switch
        {
            MonsterState.Idle => default, // Not good :(
            MonsterState.Chasing => GetAnimation(MonsterState.Walking),
            MonsterState.Fleeing => GetAnimation(MonsterState.Walking),
            MonsterState.Attacking => GetAnimation(MonsterState.Walking),
            MonsterState.Casting => GetAnimation(MonsterState.Attacking),
            _ => GetAnimation(MonsterState.Idle)
        };
    }

    private Animation GetAnimation() => GetAnimation(currentState);

    private protected override void VisibilityChanged()
    {
        base.VisibilityChanged();

        sprite.Visible = Visible && VisibleOnMap;
    }

    private protected override void MapVisibilityChanged()
    {
        base.MapVisibilityChanged();

        sprite.Visible = Visible && VisibleOnMap;
    }

    private protected override void MapOffsetChanged()
    {
        base.MapOffsetChanged();

        sprite.Position = Position.Round() - MapOffset;
    }

    private protected override void PositionChanged()
    {
        base.PositionChanged();

        if (sprite == null)
            return;

        sprite.Position = Position.Round() - MapOffset;
        sprite.Size = Size;
    }

    private protected override void UpdateActor(long elapsedTicks)
    {
        base.UpdateActor(elapsedTicks);

        monsterTicks += elapsedTicks;

        if (lastAnimationTicks == 0)
            lastAnimationTicks = monsterTicks;

        if (lastMoveTicks == 0)
            lastMoveTicks = monsterTicks;

        if (nextDecisionTicks == 0)
            SetupNextDecision();

        if (sprite.FrameIndices.Length > 1)
        {
            long elapsed = monsterTicks - lastAnimationTicks;

            if (elapsed > 0)
            {
                double elapsedMinutes = Game.TicksToMinutes(elapsed);
                double framesPerMinute = GetAnimation().FramesPerMinute;
                int elapsedFrames = MathUtil.Floor(framesPerMinute * elapsedMinutes);

                sprite.CurrentFrameIndex += elapsedFrames;
                elapsed -= Game.MinutesToTicks(elapsedFrames / framesPerMinute);

                lastAnimationTicks = monsterTicks - elapsed;
            }
        }

        switch (currentState)
        {
            case MonsterState.Idle:
                HandleIdleState();
                break;
            case MonsterState.Walking:
                HandleWalkingState();
                break;
            case MonsterState.Chasing:
                HandleChasingState();
                break;
            case MonsterState.Fleeing:
                HandleFleeingState();
                break;
            case MonsterState.Sleeping:
                HandleSleepingState();
                break;
            case MonsterState.Attacking:
                HandleAttackingState();
                break;
            case MonsterState.Casting:
                HandleCastingState();
                break;
            case MonsterState.ReceivingDamage:
                HandleReceivingDamageState();
                break;
            case MonsterState.Die:
                HandleDieState();
                break;
            case MonsterState.Summon:
                HandleSummonState();
                break;
        }        
    }

    public void PlayerAttacks(Player player)
    {
        bool isDamaged = false;

        // TODO ...

        if (isDamaged)
        {
            // Note: This automatically ends the sleeping or fleeing state
            CurrentState = MonsterState.ReceivingDamage;
        }
    }

    private bool CheckLowHpBehavior(Monster monster)
    {
        uint GetLowHitPoints() => monster.LowHitpointDivisor == 0 ? 2 : Math.Max(2, monster.HitPoints / monster.LowHitpointDivisor);

        if (monster.LowHpBehavior != MonsterLowHpBehavior.Normal && currentHitPoints < GetLowHitPoints())
        {
            // TODO

            return true;
        }

        return false;
    }

    private bool IsAggressive(Monster monster) => currentState.IsAggressive() || monster.Flags.HasFlag(MonsterFlags.Aggressive);

    private void SetupNextDecision()
    {
        var monster = GetMonsterData();
        var minDecisionDelay = Game.SecondsToTicks(0.001 * monster.MinDecisionDelay);
        var maxDecisionDelay = Game.SecondsToTicks(0.001 * monster.MaxDecisionDelay);

        nextDecisionTicks = monsterTicks + Game.Random((int)minDecisionDelay, (int)maxDecisionDelay);
    }

    private void HandleIdleState()
    {
        var monster = GetMonsterData();

        if (CheckLowHpBehavior(monster))
            return;

        bool aggressive = IsAggressive(monster);

        if (!aggressive && monsterTicks < nextDecisionTicks)
            return;

        bool canMove = monster.Flags.CanMove();

        if (aggressive)
        {
            if (IsPlayerInAttackRange())
            {
                CurrentState = MonsterState.Attacking;
            }
            else if (canMove)
            {
                if (IsPlayerInSightRange())
                {
                    CurrentState = MonsterState.Chasing;
                }
                else if (monsterTicks >= nextDecisionTicks)
                {
                    FindRandomMoveSpot();
                }
            }
        }
        else if (canMove && monsterTicks >= nextDecisionTicks)
        {
            FindRandomMoveSpot();
        }

        void FindRandomMoveSpot()
        {
            targetPosition = FindNearbyRandomSpot(monster.MoveRange);

            if (targetPosition != null)
                CurrentState = MonsterState.Walking;
        }
    }

    private void HandleWalkingState()
    {
        if (targetPosition == null)
        {
            // No target
            CurrentState = MonsterState.Idle;
            return;
        }

        Vector diff = new Vector(targetPosition ?? Amber.Common.Position.Zero) - Center;

        if (diff.X == 0 && diff.Y == 0)
        {
            // Already there
            CurrentState = MonsterState.Idle;
            return;
        }

        var monster = GetMonsterData();
        long moveTicks = monsterTicks - lastMoveTicks;
        double moveTickMinutes = Game.TicksToMinutes(moveTicks);
        float movedPixels = Math.Min((float)(moveTickMinutes * monster.MoveSpeed), diff.Length());

        if (!float.IsNaN(movedPixels) && movedPixels > 0)
        {
            Move(movedPixels);
            var (centerX, centerY) = Center;

            if (new Position(MathUtil.Round(centerX), MathUtil.Round(centerY)) == targetPosition)
            {
                // Done
                lastMoveTicks = monsterTicks;
                CurrentState = MonsterState.Idle;
            }
            else
            {
                var moveTime = movedPixels / monster.MoveSpeed;
                moveTicks -= Game.MinutesToTicks(moveTime);
                lastMoveTicks = monsterTicks + moveTicks;
            }
        }
    }

    private void HandleChasingState()
    {
        var (playerX, playerY) = game.Player.Position;
        var (playerWidth, playerHeight) = game.Player.Size;

        if (targetPosition == null)
        {
            targetPosition = new(playerX + playerWidth / 2, playerY + playerHeight / 2);
            Direction = (new Vector(targetPosition ?? Amber.Common.Position.Zero) - Center).Normalized();
        }
            
        HandleWalkingState();

        if (!IsPlayerInSightRange())
            CurrentState = MonsterState.Idle;
    }

    private void HandleReceivingDamageState()
    {
        // TODO
    }

    private void HandleFleeingState()
    {
        // TODO
    }
    
    private void HandleSleepingState()
    {
        // Do nothing
    }
    
    private void HandleAttackingState()
    {
        CurrentState = MonsterState.Idle;// TODO: REMOVE
        long attackTicks = monsterTicks - lastAttackTicks;
        // TODO ...
    }

    private void HandleCastingState()
    {
        // TODO
    }

    private void HandleDieState()
    {
        // TODO
    }

    private void HandleSummonState()
    {
        // TODO
    }

    private bool IsTileBlocking(Map map, int x, int y)
    {
        // TODO
        return false;
    }

    private Position GetCurrentTile()
    {
        var (positionX, positionY) = Position;
        int x = (MathUtil.Round(positionX) + sprite.Size.Width / 2) / MapScreen.TileWidth;
        int y = (MathUtil.Round(positionY) + sprite.Size.Height / 2) / MapScreen.TileHeight;

        return new(x, y);
    }

    private Position GetPlayerTile()
    {
        var playerPosition = game.Player.Position;
        var playerSize = game.Player.Size;

        int x = (playerPosition.X + playerSize.Width / 2) / MapScreen.TileWidth;
        int y = (playerPosition.Y + playerSize.Height / 2) / MapScreen.TileHeight;

        return new(x, y);
    }

    private float GetDistanceToPlayer()
    {
        var playerSize = game.Player.Size;
        var playerCenter = new Vector(game.Player.Position) + 0.5f * new Vector(playerSize.Width, playerSize.Height);

        return (playerCenter - Center).Length() / MapScreen.TileWidth;
    }

    private Position? FindNearbyRandomSpot(ushort moveRange)
    {
        if (moveRange == 0)
            return null;

        var map = mapScreen.Map;

        var (tileColumn, tileRow) = GetCurrentTile();
        int prevTileColumn = tileColumn;
        int prevTileRow = tileRow;

        void NextRandomSpot(ref int x, ref int y)
        {
            int dx = Game.Random(0, 2) - 1;
            int dy = Game.Random(0, 2) - 1;

            if (IsTileBlocking(map, x + dx, y + dy))
            {
                if (dx != 0 && !IsTileBlocking(map, x, y + dy))
                {
                    y += dy;
                    return;
                }

                if (dy != 0 && !IsTileBlocking(map, x + dy, y))
                {
                    x += dx;
                    return;
                }
            }
            else
            {
                x += dx;
                y += dy;
            }
        }

        for (int i = 0; i < moveRange; i++)
            NextRandomSpot(ref tileColumn, ref tileRow);

        if (tileColumn == prevTileColumn && tileRow == prevTileRow)
            return null;

        return new Position(tileColumn * MapScreen.TileWidth + MapScreen.TileWidth / 2, tileRow * MapScreen.TileHeight + MapScreen.TileHeight / 2);
    }

    private bool IsPlayerInSightRange()
    {
        var monster = GetMonsterData();

        if (GetDistanceToPlayer() <= monster.VisionRange)
            return true; // TODO: blocked sight

        return false;
    }

    private bool IsPlayerInAttackRange()
    {
        var monster = GetMonsterData();

        if (GetDistanceToPlayer() <= monster.AttackRange)
            return true; // TODO: blocked sight

        return false;
    }
}
