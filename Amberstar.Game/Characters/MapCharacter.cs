using Amber.Common;
using Amberstar.GameData;

namespace Amberstar.Game.Characters;

internal class MapCharacter
{
    private readonly IMap map;
    private readonly GameData.MapCharacter data;
    private readonly Position[] positions;
    private readonly Func<int, int, int, bool> canMoveChecker;
    private int currentPathLength = 0;
    private Direction direction = Direction.North;
    private Position position; // this one is 1-based

    public int Index { get; }

    public IMap Map => map;

    public int CharacterIndex => data.Index;

    public MapCharacterType Type => data.Type;

    public Position Position => new(position.X - 1, position.Y - 1);

    public int Icon => data.Icon;

    public MapCharacter(IMap map, int index, Position[] positions, GameState GameState,
        Func<int, int, int, bool> canMoveChecker)
    {
        this.map = map;
        Index = index;
        data = map.Characters[index];
        this.positions = positions;
        this.canMoveChecker = canMoveChecker;

        UpdatePosition(GameState);
    }

    private void UpdatePosition(GameState GameState)
    {
        void TryWalkTo(Position position)
        {
            if (canMoveChecker(position.X - 1, position.Y - 1, data.TravelType))
                this.position = position;
        }

        switch (data.WalkType)
        {
            case MapCharacterWalkType.Stationary:
                position = positions[0];
                break;
            case MapCharacterWalkType.Path:
            {
                int totalSteps = GameState.Hour * 12 + GameState.Minute / 5;
                TryWalkTo(positions[totalSteps]);
                break;
            }
            case MapCharacterWalkType.Chase:
                // TODO
                break;
            default: // random
                if (position == new Position()) // first time
                    position = positions[0];
                else
                    MoveRandomly();
                break;
        }
    }

    private void SetupNewRandomPath()
    {
        currentPathLength = Game.Random(1, 4);
        int dir = (int)direction + 1;
        dir += Game.Random(0, 1);
        dir &= 0x3;
        direction = (Direction)dir;

        switch (direction)
        {
            case Direction.North:
                if (currentPathLength <= position.Y)
                    currentPathLength += position.Y - currentPathLength - 1;
                break;
            case Direction.East:
                if (currentPathLength > map.Width - position.X)
                    currentPathLength += map.Width - position.X - currentPathLength - 1;
                break;
            case Direction.South:
                if (currentPathLength > map.Height - position.Y)
                    currentPathLength += map.Height - position.Y - currentPathLength - 1;
                break;
            case Direction.West:
                if (currentPathLength <= position.X)
                    currentPathLength += position.X - currentPathLength - 1;
                break;
        }
    }

    private void MoveRandomly()
    {
        if (currentPathLength == 0)
            SetupNewRandomPath();

        for (int i = 0; i < 4; i++)
        {
            // 4 tries
            var offset = direction.Offset();

            if (canMoveChecker(Position.X + offset.X, Position.Y + offset.Y, data.TravelType))
            {
                currentPathLength--;
                position = new(position.X + offset.X, position.Y + offset.Y);
                return;
            }

            SetupNewRandomPath();
        }

        currentPathLength = 0;
    }

    public void Update(Game Game)
    {
        UpdatePosition(Game.State);
    }
}
