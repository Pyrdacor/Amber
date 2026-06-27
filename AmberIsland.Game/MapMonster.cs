using Amber.Common;
using Amber.Renderer.Common;
using AmberIsland.Game.Extensions;
using AmberIsland.Game.Screens;
using AmberIsland.GameData;

namespace AmberIsland.Game;

internal class MapMonster : CombatMapActor
{
    private readonly Game game;
    private readonly MapScreen mapScreen;
    private readonly uint monsterIndex;
    // TODO: conditions
    private uint currentHitPoints = 0;
    private Position? targetPosition = null;
    private float remainingTargetDistance = 0.0f;
    private MonsterState currentState = MonsterState.Idle;
    private Monster? monster;
    private long nextDecisionTicks = 0;
    private long lastMoveTicks = 0;
    private long lastAttackTicks = 0;
    private long lastAnimationTicks = 0;
    private long playAnimationStartTicks = 0;
    private long playAnimationDurationInTicks = 0;
    private bool playAnimationTriggeredNextStateAlready = false;
    private long monsterTicks = 0;
    private readonly ISequencedSprite sprite;
    private readonly Dictionary<MonsterState, Animation> animations = [];

    private MonsterState CurrentState
    {
        get => currentState;
        set
        {
            var lastState = currentState;

            if (currentState == value)
                return;

            currentState = value;

            var animation = GetAnimation();

            if (animation != GetAnimation(lastState))
            {
                lastAnimationTicks = 0;
                sprite.SetFrameIndicesAndOrigin(GetAnimation, monsterIndex, VisualDirection, resetFrameIndex: true);
            }

            if (currentState == MonsterState.Idle)
            {
                SetupNextDecision();
            }
            else if (currentState == MonsterState.Walking || currentState == MonsterState.Chasing || currentState == MonsterState.Fleeing)
            {
                lastMoveTicks = 0;
                var direction = Direction = (new Vector(targetPosition ?? Amber.Common.Position.Zero) - Center).Normalized();

                if (direction.Length() == 0)
                {
                    targetPosition = null;
                    remainingTargetDistance = 0.0f;
                    return;
                }
            }
        }
    }

    private protected override uint ScaleFactor
    {
        get
        {
            var monster = GetMonsterData();
            return monster.ScaleFactor == 0 ? 16u : monster.ScaleFactor;
        }
    }

    private protected override Position RelativeCenter
    {
        get
        {
            var monster = GetMonsterData();
            var baseValue = base.RelativeCenter;
            var centerX = monster.CenterX == ushort.MaxValue ? baseValue.X : ScaleRoundCoordinate(monster.CenterX);
            var centerY = monster.CenterY == ushort.MaxValue ? baseValue.Y : ScaleRoundCoordinate(monster.CenterY);

            return new(sprite.MirrorX ? Size.Width - centerX : centerX, centerY);
        }
    }

    private Position RelativeProjectileSourcePosition
    {
        get
        {
            var monster = GetMonsterData();
            var baseValue = RelativeCenter;
            var sourceX = monster.ProjectileSourceX == ushort.MaxValue ? baseValue.X : ScaleRoundCoordinate(monster.ProjectileSourceX);
            var sourceY = monster.ProjectileSourceY == ushort.MaxValue ? baseValue.Y : ScaleRoundCoordinate(monster.ProjectileSourceY);

            return new(sourceX, sourceY);
        }
    }

    private protected override uint TotalPhysicalMinDamage => GetMonsterData().MinAttackDamage;
    private protected override uint TotalPhysicalMaxDamage => GetMonsterData().MaxAttackDamage;
    private protected override uint TotalMagicalMinDamage => GetMonsterData().MinMagicDamage;
    private protected override uint TotalMagicalMaxDamage => GetMonsterData().MaxMagicDamage;
    private protected override uint TotalPhysicalDefense => GetMonsterData().PhysicalDefense;
    private protected override uint TotalMagicalDefense => GetMonsterData().MagicDefense;
    private protected override uint TotalPhysicalDamageReduction => GetMonsterData().PhysicalDamageReduction;
    private protected override uint TotalMagicalDamageReduction => GetMonsterData().MagicDamageReduction;
    private protected override uint TotalHit => GetMonsterData().Hit;
    private protected override uint TotalDodge => GetMonsterData().Dodge;
    private protected override uint Level => GetMonsterData().Level;
    private protected override Element AttackElement => GetMonsterData().Element;
    private protected override Element DefendElement => GetMonsterData().Element;

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
        sprite.TextureSize = new(animation.FrameSize.Width, animation.FrameSize.Height);
        sprite.BaseLineOffset = 0; // TODO
        sprite.PaletteIndex = (byte)mapScreen.GetMonsterPaletteIndices(monsterIndex)[monster.PaletteIndex];
        sprite.Visible = false;

        var scaleFactor = TotalScaleFactor;

        Size = new(MathUtil.Round(scaleFactor * animation.FrameSize.Width), MathUtil.Round(scaleFactor * animation.FrameSize.Height));
        Position = new Vector(position - RelativeCenter);
        VisualDirection = direction;

        sprite.SetFrameIndicesAndOrigin(GetAnimation, monsterIndex, direction, resetFrameIndex: true);

        Visible = true;
    }

    private Monster GetMonsterData() => monster ??= game.GameData.GetMonster(monsterIndex);

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
            // These play animation states always fall back to real states
            // which have valid animation data.
            MonsterState.PlayAttackAnimation => GetAnimation(MonsterState.Attacking),
            MonsterState.PlayCastAnimation => GetAnimation(MonsterState.Casting),
            MonsterState.PlayHurtAnimation => GetAnimation(MonsterState.ReceivingDamage),
            MonsterState.PlayDieAnimation => GetAnimation(MonsterState.Die),
            MonsterState.PlaySummonAnimation => GetAnimation(MonsterState.Summon),
            _ => GetAnimation(MonsterState.Idle)
        };
    }

    private Animation GetAnimation() => GetAnimation(currentState);

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

        var animation = GetAnimation();
        var newOrigin = sprite.FrameOrigin + directionDiff * (animation.DirectionOffset ?? Amber.Common.Position.Zero);
        sprite.FrameOrigin = newOrigin;

        CheckSpriteMirroring();
    }

    private protected override void DirectionChanged(Vector oldDirection, Vector newDirection)
    {
        base.DirectionChanged(oldDirection, newDirection);

        CheckSpriteMirroring();
    }

    private void CheckSpriteMirroring()
    {
        var animation = GetAnimation();
        bool wasMirrored = sprite.MirrorX;
        bool mirrorX = animation.DirectionOffset == null && (Direction.X < 0 || Direction.X == 0 && Direction.Y < 0);

        if (mirrorX != wasMirrored)
        {
            var relativeCenterX = RelativeCenter.X;
            var centerX = Center.X;
            float newX = mirrorX
                ? centerX - Size.Width + relativeCenterX
                : centerX - Size.Width + relativeCenterX;

            sprite.MirrorX = mirrorX;
            Position = new(newX, Position.Y);
        }
    }

    private void RunStateHandler()
    {
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
            default:
                // These should be animation play states
                HandlePlayAnimationState();
                break;
        }
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

        RunStateHandler();      
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

        bool canMove = monster.MoveRange != 0 && monster.Flags.CanMove();

        if (aggressive)
        {
            if (CanAttack())
            {
                if (monster.ProjectileIndex != 0 && monster.ProjectileEmitDelay != 0xffff)
                {
                    if (monster.ProjectileEmitDelay == 0)
                    {
                        CurrentState = MonsterState.Attacking;
                        lastAttackTicks = monsterTicks;
                    }
                    else if (animations.TryGetValue(MonsterState.Attacking, out var animation))
                    {
                        double animationDuration = animation.DurationInMinutes();
                        playAnimationDurationInTicks = Game.SecondsToTicks(monster.ProjectileEmitDelay / 1000.0);
                        lastAttackTicks = monsterTicks + Game.MinutesToTicks(animationDuration);
                        playAnimationStartTicks = monsterTicks;
                        CurrentState = MonsterState.PlayAttackAnimation;
                    }
                }
                else if (animations.TryGetValue(MonsterState.Attacking, out var animation))
                {
                    double animationDuration = animation.DurationInMinutes();
                    playAnimationDurationInTicks = Game.MinutesToTicks(animationDuration);
                    // Attack delay only starts counting after the animation has finished
                    lastAttackTicks = monsterTicks + playAnimationDurationInTicks;
                    playAnimationStartTicks = monsterTicks;
                    CurrentState = MonsterState.PlayAttackAnimation;
                }
                else
                {
                    // No animation, attack directly
                    CurrentState = MonsterState.Attacking;
                    lastAttackTicks = monsterTicks;
                }
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
            {
                var diff = new Vector(targetPosition ?? Amber.Common.Position.Zero) - Center;
                remainingTargetDistance = diff.Length();
                CurrentState = MonsterState.Walking;
            }
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
        float movedPixels = Math.Min((float)(moveTickMinutes * monster.MoveSpeed), remainingTargetDistance);

        if (!float.IsNaN(movedPixels) && movedPixels > 0)
        {
            Move(movedPixels);
            remainingTargetDistance -= movedPixels;
            var (centerX, centerY) = Center;

            if (remainingTargetDistance <= 0.0f)
            {
                // Done
                targetPosition = null;
                remainingTargetDistance = 0.0f;
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
        if (targetPosition == null)
        {
            targetPosition = game.Player.Center.Round();
            var diff = new Vector(targetPosition ?? Amber.Common.Position.Zero) - Center;
            remainingTargetDistance = diff.Length();
            Direction = diff.Normalized();
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
        var monster = GetMonsterData();

        if (monster.ProjectileIndex != 0)
        {
            var direction = (game.Player.Position - Position).Normalized();
            Direction = direction;
            mapScreen.SpawnProjectile(this, Center.Round(), direction, 1);
        }
        else
        {
            // TODO: REMOVE
            string monsterName = monsterIndex == 1 ? "Bat" : "Dragon";

            if (!TestHit(game.Player))
            {
                // TODO: Show "Missed"
                Console.WriteLine($"{monsterName} misses");
            }
            else
            {
                // Deal damage
                var damage = CalculcatePhysicalDamage(game.Player);

                if (damage == 0)
                {
                    // TODO: Show "No dmg"
                    Console.WriteLine($"{monsterName} does not deal damage");
                }
                else
                {
                    // TODO: Hurt player and show the damage
                    Console.WriteLine($"{monsterName} deals {damage} damage");
                }
            }
        }

        CurrentState = MonsterState.Idle;
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

    private void HandlePlayAnimationState()
    {
        void EndState()
        {
            playAnimationDurationInTicks = 0;
            playAnimationStartTicks = 0;
            playAnimationTriggeredNextStateAlready = false;
        }

        var elapsedAnimationTicks = monsterTicks - playAnimationStartTicks;

        if (elapsedAnimationTicks >= playAnimationDurationInTicks)
        {
            var fullAnimationDuration = Game.MinutesToTicks(GetAnimation().DurationInMinutes());

            if (elapsedAnimationTicks >= fullAnimationDuration)
            {
                if (playAnimationTriggeredNextStateAlready)
                    CurrentState = MonsterState.Idle;
                else
                    SetNextState();

                EndState();
            }
            else if (!playAnimationTriggeredNextStateAlready)
            {
                playAnimationTriggeredNextStateAlready = true;

                // Trigger the next state's action but keep the animation rolling.
                var oldState = CurrentState;
                var timeRemaining = fullAnimationDuration - playAnimationDurationInTicks;
                SetNextState();
                RunStateHandler();
                CurrentState = oldState;
                playAnimationDurationInTicks = timeRemaining;
            }

            void SetNextState()
            {
                CurrentState = CurrentState switch
                {
                    MonsterState.PlayAttackAnimation => MonsterState.Attacking,
                    MonsterState.PlayCastAnimation => MonsterState.Casting,
                    MonsterState.PlayHurtAnimation => MonsterState.ReceivingDamage,
                    MonsterState.PlayDieAnimation => MonsterState.Die,
                    MonsterState.PlaySummonAnimation => MonsterState.Summon,
                    _ => MonsterState.Idle, // Should not happen, but better safe than sorry :)
                };
            }
        }
    }

    private float GetDistanceToPlayer()
    {
        return (game.Player.Center - Center).Length() / MapScreen.TileWidth;
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

    private bool CanAttack()
    {
        if (!IsPlayerInAttackRange())
            return false;

        // TODO: later check conditions/ailments

        long attackTicks = monsterTicks - lastAttackTicks;
        double attackDelay = Game.TicksToMinutes(attackTicks);
        var monster = GetMonsterData();

        return monster.AttackSpeed * attackDelay >= 1;
    }
}
