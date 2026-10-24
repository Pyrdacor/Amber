using Amber.Common;
using Amberstar.GameData;

namespace Amberstar.Game.Characters;

internal class MapCharacter
{
    private readonly IMap map;
    private readonly GameData.MapCharacter data;
    private readonly Position[] positions;
    private readonly Func<int, int, int, bool> canMoveChecker;
    private readonly Func<int, int, bool> blocksSight;
    private int currentPathLength = 0;
    private Direction direction = Direction.North;
    private Position position; // this one is 1-based
    // A hunting monster which saw the party keeps its direction (bit 0 of VChar_flag in the original).
    private bool hunting = false;

    // Directions to approach the party (.Dir_table of Hunting_monster in the original).
    // The index is (1 - sgn(dx)) + 3 * (1 - sgn(dy)) + 9 * (sgn(|dx| - |dy|) + 1)
    // where dx and dy are the distances from the party to the monster.
    private static readonly Direction[]?[] HuntDirections =
    [
        // |dx| < |dy|
        [Direction.Up, Direction.Left, Direction.Right], [Direction.Up, Direction.Left, Direction.Right], [Direction.Up, Direction.Right, Direction.Left],
        [Direction.Left, Direction.Up, Direction.Down], null, [Direction.Right, Direction.Up, Direction.Down],
        [Direction.Down, Direction.Left, Direction.Right], [Direction.Down, Direction.Right, Direction.Left], [Direction.Down, Direction.Right, Direction.Left],
        // |dx| = |dy|
        [Direction.Up, Direction.Left], null, [Direction.Up, Direction.Right],
        null, null, null,
        [Direction.Down, Direction.Left], null, [Direction.Down, Direction.Right],
        // |dx| > |dy|
        [Direction.Left, Direction.Up, Direction.Down], [Direction.Up, Direction.Left, Direction.Right], [Direction.Right, Direction.Up, Direction.Down],
        [Direction.Left, Direction.Up, Direction.Down], null, [Direction.Right, Direction.Up, Direction.Down],
        [Direction.Left, Direction.Down, Direction.Up], [Direction.Down, Direction.Left, Direction.Right], [Direction.Right, Direction.Down, Direction.Up],
    ];

    public int Index { get; }

    public IMap Map => map;

    public int CharacterIndex => data.Index;

    public MapCharacterType Type => data.Type;

    public Position Position => new(position.X - 1, position.Y - 1);

    public int Icon => data.Icon;

    /// <param name="canMoveChecker">Checks if the character can move to a 0-based position with its collision class.</param>
    /// <param name="blocksSight">Checks if the tile at a 0-based position blocks the sight.</param>
    public MapCharacter(IMap map, int index, Position[] positions, GameState GameState,
        Func<int, int, int, bool> canMoveChecker, Func<int, int, bool> blocksSight)
    {
        this.map = map;
        Index = index;
        data = map.Characters[index];
        this.positions = positions;
        this.canMoveChecker = canMoveChecker;
        this.blocksSight = blocksSight;

        UpdatePosition(GameState);
    }

    /// <summary>
    /// Some persons are only there on a specific day of the year.
    /// </summary>
    public bool IsPresent(GameState gameState) => data.Day == 0xff || (data.Day == gameState.Day && data.Month == gameState.Month);

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

                // The first time the character is always placed, otherwise it would have no valid position.
                if (position == new Position())
                    position = positions[totalSteps];
                else
                    TryWalkTo(positions[totalSteps]);
                break;
            }
            case MapCharacterWalkType.Chase:
                if (position == new Position()) // first time
                    position = positions[0];
                else
                    Hunt(GameState, false);
                break;
            default: // random
                if (position == new Position()) // first time
                    position = positions[0];
                else
                    MoveRandomly(GameState);
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

    bool reachedParty = false;

    /// <summary>
    /// Checks if the character can move in the given direction (Character_movement_check in the original).
    /// Monsters which reach the party start a battle (Battle_check in the program which is disabled in
    /// the released source code). They don't move onto the party.
    /// </summary>
    private bool CanMove(GameState gameState, Direction direction)
    {
        var offset = direction.Offset();
        var target = new Position(Position.X + offset.X, Position.Y + offset.Y);

        if (Type == MapCharacterType.Monster && target == gameState.PartyPosition)
        {
            reachedParty = true;
            return false;
        }

        return canMoveChecker(target.X, target.Y, data.TravelType);
    }

    private void Move(Direction direction)
    {
        var offset = direction.Offset();
        position = new(position.X + offset.X, position.Y + offset.Y);
    }

    private void MoveRandomly(GameState gameState)
    {
        if (currentPathLength == 0)
            SetupNewRandomPath();

        for (int i = 0; i < 4; i++)
        {
            // 4 tries
            if (CanMove(gameState, direction))
            {
                currentPathLength--;
                Move(direction);
                return;
            }

            SetupNewRandomPath();
        }

        currentPathLength = 0;
    }

    /// <summary>
    /// Checks if a monster sees the party (Check_line_of_sight in the original).
    /// The tiles between the monster and the party must not block the sight.
    /// </summary>
    private bool CanSeeParty(GameState gameState)
    {
        // The party can't be seen while the invisibility spell is active.
        if (gameState.ActiveSpells[(int)Game.ActiveSpellType.Invisibility].Duration != 0)
            return false;

        var partyPosition = gameState.PartyPosition;
        int dx = Position.X - partyPosition.X;
        int dy = Position.Y - partyPosition.Y;
        int steps = Math.Max(Math.Abs(dx), Math.Abs(dy));

        for (int i = 1; i < steps; i++)
        {
            int x = partyPosition.X + (int)Math.Round((double)dx * i / steps);
            int y = partyPosition.Y + (int)Math.Round((double)dy * i / steps);

            if (blocksSight(x, y))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Hunting monsters move towards the party if they see it. Otherwise they keep their
    /// direction or move randomly (Hunting_monster in the original). If the party moved,
    /// only monsters which see the party move.
    /// </summary>
    private void Hunt(GameState gameState, bool partyMoved)
    {
        if (CanSeeParty(gameState))
        {
            hunting = true;

            var partyPosition = gameState.PartyPosition;
            int dx = Position.X - partyPosition.X;
            int dy = Position.Y - partyPosition.Y;
            int index = (1 - Math.Sign(dx)) + 3 * (1 - Math.Sign(dy)) + 9 * (Math.Sign(Math.Abs(dx) - Math.Abs(dy)) + 1);
            var directions = HuntDirections[index];

            if (directions == null)
                return;

            foreach (var huntDirection in directions)
            {
                if (CanMove(gameState, huntDirection))
                {
                    direction = huntDirection;
                    Move(huntDirection);
                    return;
                }
            }

            return;
        }

        if (partyMoved)
            return;

        if (hunting && CanMove(gameState, direction))
        {
            Move(direction);
            return;
        }

        hunting = false;
        MoveRandomly(gameState);
    }

    /// <summary>
    /// Updates the position every time step (5 minutes).
    /// </summary>
    public void Update(Game Game)
    {
        UpdatePosition(Game.State);
        CheckReachedParty(Game);
    }

    /// <summary>
    /// The monster group of the monster fights the party.
    /// </summary>
    private void CheckReachedParty(Game game)
    {
        if (!reachedParty)
            return;

        reachedParty = false;
        game.MonsterReachedParty(this);
    }

    /// <summary>
    /// Hunting monsters which see the party also move when the party moves.
    /// Returns true if the character moved.
    /// </summary>
    public bool PartyMoved(Game game)
    {
        if (data.WalkType != MapCharacterWalkType.Chase || position == new Position())
            return false;

        var oldPosition = position;
        Hunt(game.State, true);
        CheckReachedParty(game);

        return position != oldPosition;
    }
}
