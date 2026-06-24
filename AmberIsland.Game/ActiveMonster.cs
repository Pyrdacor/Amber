using Amber.Common;
using Amber.Renderer.Common;
using AmberIsland.Game.Extensions;
using AmberIsland.Game.Screens;
using AmberIsland.GameData;

namespace AmberIsland.Game;

internal class ActiveMonster
{
    private const int TicksPerAnimationFrame = 10; // TODO: maybe dependent on animation and monster
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
    private ISequencedSprite sprite;
    // TODO: We need some monster animation info
    private readonly Dictionary<MonsterState, Animation> animations = [];

    static ActiveMonster()
    {
        /*var frameSize = new Size(64, 64);

        void AddAnimation(MonsterState state, params uint[] frameIndices)
        {
            animations.Add(state, new Animation { FrameSize = frameSize, FrameIndices = frameIndices });
        }

        //AddAnimation(MonsterState.Idle, 0, 1, 4, 5, 4);
        AddAnimation(MonsterState.Idle, 2, 3, 4, 5, 6, 5, 4, 3);
        AddAnimation(MonsterState.Walking, 2, 3, 4, 5, 6, 5, 4, 3);
        AddAnimation(MonsterState.Chasing, 2, 3, 4, 5, 6, 5, 4, 3);
        AddAnimation(MonsterState.Fleeing, 2, 3, 4, 5, 6, 5, 4, 3);
        AddAnimation(MonsterState.Sleeping, 10, 11, 12, 13, 14, 13, 12, 11);*/
    }

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

    private Animation GetAnimation() => animations[currentState];

    public void Update(long elapsedTicks)
    {
        monsterTicks += elapsedTicks;

        if (lastAnimationTicks == 0)
            lastAnimationTicks = monsterTicks;

        if (sprite.FrameIndices.Length > 1)
        {
            long elapsed = monsterTicks - lastAnimationTicks;

            while (elapsed >= TicksPerAnimationFrame)
            {
                sprite.CurrentFrameIndex += 1;
                elapsed -= TicksPerAnimationFrame;
            }

            lastAnimationTicks = monsterTicks - elapsed;
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
