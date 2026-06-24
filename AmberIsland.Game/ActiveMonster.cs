using Amber.Common;
using Amber.Renderer.Common;
using AmberIsland.Game.Extensions;
using AmberIsland.Game.Screens;
using AmberIsland.GameData;

namespace AmberIsland.Game;

internal class ActiveMonster
{
    private readonly Game game;
    private readonly MapScreen mapScreen;
    private readonly uint monsterIndex;
    // TODO: conditions
    private uint currentHitPoints = 0;
    private Position currentPosition = Position.Zero;
    private Direction direction = Direction.Down;
    private MonsterState currentState = MonsterState.Idle;
    private long lastMoveTicks = 0;
    private long lastAttackTicks = 0;
    private long lastAnimationTicks = 0;
    private long monsterTicks = 0;
    private readonly ISequencedSprite sprite;
    private readonly Dictionary<MonsterState, Animation> animations = [];

    public ActiveMonster(Game game, MapScreen mapScreen, uint monsterIndex, Position position, Direction direction)
    {
        this.game = game;
        this.mapScreen = mapScreen;
        this.monsterIndex = monsterIndex;
        currentPosition = position;

        animations = game.GameData.GetMonsterAnimations(monsterIndex);

        var monster = GetMonsterData();
        var layer = game.GetRenderLayer(Layer.Monsters);
        var animation = GetAnimation();
        sprite = layer.SpriteFactory!.CreateSequenced();
        sprite.TextureSize = new(64, 64);
        sprite.Position = position; // TODO: this is not the map position but the screen display position!
        sprite.Size = new(32, 32);
        sprite.MirrorX = direction == Direction.Left || direction == Direction.Up; // TODO
        sprite.BaseLineOffset = 0; // TODO

        sprite.SetFrameIndicesAndOrigin(GetAnimation, direction, resetFrameIndex: true);

        sprite.Visible = true;
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
            sprite.MirrorX = animation.DirectionOffset == null && (direction == Direction.Left || direction == Direction.Up);
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

    public void Update(long elapsedTicks)
    {
        monsterTicks += elapsedTicks;

        if (lastAnimationTicks == 0)
            lastAnimationTicks = monsterTicks;

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
                return;
            case MonsterState.Walking:
            {
                var monster = GetMonsterData();
                long moveTicks = monsterTicks - lastMoveTicks;
                double moveTickMinutes = Game.TicksToMinutes(moveTicks);
                int movedPixels = MathUtil.Floor(moveTickMinutes * monster.MoveSpeed);
                // TODO: adjust lastMoveTicks properly

                if (movedPixels > 0)
                {
                    // TODO
                }

                break;
            }
            case MonsterState.Attacking:
            {
                long attackTicks = monsterTicks - lastAttackTicks;
                // TODO ...
                break;
            }
            // TODO ...
        }        
    }
}
