using Amber.Common;
using AmberIsland.Game.Screens;
using AmberIsland.GameData;

namespace AmberIsland.Game;

public enum ActorType
{
    Player,
    Monster,
    NPC,
    Item,
    Object
}

internal class MapActor(Game game, ActorType actorType)
{
    private Vector position = Vector.Zero;
    private Size size = Size.Zero;
    private Vector center = Vector.Zero;
    private Rect area = new();
    private Vector direction = Vector.Zero;
    private Direction visualDirection = GameData.Direction.Down;
    private Position mapOffset = Amber.Common.Position.Zero;
    private bool visibleOnMap = false;
    private bool visible = false;
    private TravelType travelType = TravelType.Walk;

    public ActorType Type => actorType;

    public Vector Position
    {
        get => position;
        set
        {
            if (position != value)
            {
                var old = position;
                position = value;
                center = position + size;
                area = new(position.Round(), size);
                PositionChanged(old, position, size, size);
            }
        }
    }

    public Size Size
    {
        get => size;
        set
        {
            if (size != value)
            {
                var old = size;
                size = value;
                center = position + size;
                area = new(area.Position, size);
                PositionChanged(position, position, old, size);
            }
        }
    }

    public Vector Center => center;

    public Rect Area => area;

    public virtual Rect CollisionArea => Area;

    public Vector Direction
    {
        get => direction;
        set
        {
            if (direction != value)
            {
                var old = direction;
                direction = value;
                DirectionChanged(old, direction);

                if (Math.Abs(direction.X) > Math.Abs(direction.Y))
                {
                    if (direction.X < 0)
                        VisualDirection = GameData.Direction.Left;
                    else
                        VisualDirection = GameData.Direction.Right;
                }
                else
                {
                    if (direction.Y < 0)
                        VisualDirection = GameData.Direction.Up;
                    else
                        VisualDirection = GameData.Direction.Down;
                }
            }
        }
    }

    public Direction VisualDirection
    {
        get => visualDirection;
        set
        {
            if (visualDirection != value)
            {
                var old = visualDirection;
                visualDirection = value;
                VisualDirectionChanged(old, visualDirection);

                Direction = visualDirection switch
                {
                    GameData.Direction.Down => new(0, 1),
                    GameData.Direction.Up => new(0, -1),
                    GameData.Direction.Right => new(1, 0),
                    GameData.Direction.Left => new(-1, 0),
                    _ => new(0, 0)
                };
            }
        }
    }

    public bool VisibleOnMap
    {
        get => visibleOnMap;
        private set
        {
            if (visibleOnMap != value)
            {
                visibleOnMap = value;
                MapVisibilityChanged(!visibleOnMap, visibleOnMap);
            }
        }
    }

    public bool Visible
    {
        get => visible;
        set
        {
            if (visible != value)
            {
                visible = value;
                VisibilityChanged(!visible, visible);
            }
        }
    }

    public Position MapOffset
    {
        get => mapOffset;
        set
        {
            if (mapOffset != value)
            {
                var old = mapOffset;
                mapOffset = value;
                MapOffsetChanged(old, mapOffset);
            }
        }
    }

    public TravelType TravelType
    {
        get => travelType;
        private set
        {
            if (travelType != value)
            {
                var old = travelType;
                travelType = value;
                TravelTypeChanged(old, travelType);
            }
        }
    }

    private protected virtual void PositionChanged(Vector oldPosition, Vector newPosition, Size oldSize, Size newSize)
    {

    }

    private protected virtual void DirectionChanged(Vector oldDirection, Vector newDirection)
    {

    }

    private protected virtual void VisualDirectionChanged(Direction oldDirection, Direction newDirection)
    {

    }

    private protected virtual void MapVisibilityChanged(bool oldMapVisibility, bool newMapVisibility)
    {

    }

    private protected virtual void VisibilityChanged(bool oldVisibility, bool newVisibility)
    {

    }

    private protected virtual void MapOffsetChanged(Position oldMapOffset, Position newMapOffset)
    {

    }

    private protected virtual void TravelTypeChanged(TravelType oldTravelType, TravelType newTravelType)
    {

    }

    private protected virtual void UpdateActor(long elapsedTicks)
    {

    }

    public void Update(Rect mapArea, long elapsedTicks)
    {
        UpdateActor(elapsedTicks);

        VisibleOnMap = mapArea.Overlaps(area);
        MapOffset = mapArea.Position;
    }

    public bool CollidesWith(MapActor actor) => area.Overlaps(actor.area);

    public void Move(float amount)
    {
        Position += amount * Direction;
    }

    protected bool IsTileBlocking(Map map, int x, int y) => game.IsTileBlocking(map, x, y, Type, TravelType);

    public Position GetCurrentTile()
    {
        var (positionX, positionY) = Position.Round();
        var (width, height) = Size;
        int x = (positionX + width / 2) / MapScreen.TileWidth;
        int y = (positionY + height / 2) / MapScreen.TileHeight;

        return new(x, y);
    }

    public Position GetTileForPositionOffset(Position offset)
    {
        var (positionX, positionY) = (Position.Round() + offset);
        var (width, height) = Size;
        int x = (positionX + width / 2) / MapScreen.TileWidth;
        int y = (positionY + height / 2) / MapScreen.TileHeight;

        return new(x, y);
    }
}
