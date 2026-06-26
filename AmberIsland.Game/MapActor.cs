using Amber.Common;
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
                position = value;
                center = position + size;
                area = new(position.Round(), size);
                PositionChanged();
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
                size = value;
                center = position + size;
                area = new(area.Position, size);
                PositionChanged();
            }
        }
    }

    public Vector Center => center;

    public Rect Area => area;

    public Vector Direction
    {
        get => direction;
        set
        {
            if (direction != value)
            {
                direction = value;
                DirectionChanged();
            }
        }
    }

    public bool VisibleOnMap
    {
        get => visibleOnMap;
        set
        {
            if (visibleOnMap != value)
            {
                visibleOnMap = value;
                MapVisibilityChanged();
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
                VisibilityChanged();
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
                mapOffset = value;
                MapOffsetChanged();
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
                travelType = value;
                TravelTypeChanged();
            }
        }
    }

    private protected virtual void PositionChanged()
    {

    }

    private protected virtual void DirectionChanged()
    {

    }

    private protected virtual void MapVisibilityChanged()
    {

    }

    private protected virtual void VisibilityChanged()
    {

    }

    private protected virtual void MapOffsetChanged()
    {

    }

    private protected virtual void TravelTypeChanged()
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
}
