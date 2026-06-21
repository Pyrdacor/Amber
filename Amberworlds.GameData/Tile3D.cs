namespace Amberworlds.GameData;

public enum TileType3D : byte
{
    Empty               = 0,
    Wall                = 1,
    Object              = 2,
    Invalid             = 3, // Can't be entered, used for map areas which are outside the real map.
}

[Flags]
public enum TileFlags3D : uint
{
    None            = 0,
    Breakable       = 1u << 0, // Wall or object can be destroyed
    FloorBreakable  = 1u << 1, // Floor can be destroyed
    Sky             = 1u << 2, // The ceiling is a sky
    Physics         = 1u << 3, // Uses physics (can be moved, fall, etc)
    Interactable    = 1u << 4, // Can be interacted with by the player (pulled, pushed, etc)
    Bed             = 1u << 5, // Player can sleep
    BlocksSight     = 1u << 6, // Sight is blocked for players and monsters
    Opaque          = 1u << 7, // No transparency (mostly for walls)
    LadderUp        = 1u << 8, // Can climb to the upper layer
    LadderDown      = 1u << 9, // Can climb down the lower layer
}

public enum TileInteractionMode3D : byte
{
    None            = 0,
    Pull            = 1, // Can be pulled only
    Push            = 2, // Can be pushed only
    PushAndPull     = 3, // Can be pushed and pulled
    Carry           = 4, // Can be picked up, carried and put down
    DestroyOnTouch  = 5, // Can be destroyed (on touch)
    DestroyOnMove   = 6, // Can be destroyed (on move)
    Destroy         = 7, // Can be destroyed (on touch or move)
}

public struct Tile3D
{
    // First byte: tile type + ceiling and floor index.
    // Bit 0-1: Tile type
    // Bit 2-4: Floor index
    // Bit 5-7: Ceiling index
    public byte TileTypeAndFloorCeilingIndex;
    public TileInteractionMode3D InteractionMode;
    public TileFlags3D TileFlags;
}

public struct Wall3D
{
    // Includes Tile3D

    // 2 bits per corner.
    // 00: Normal edge (if all corners have it, normal block)
    // 01: Cut from side center to side center (if all corners have it, smaller 45 degree rotated block)
    // 10: Cut small edge
    // 11: Remove whole corner
    // Special: If all have 11 (= 11111111), this means smaller center clock.
    // Order of bits: ULURLLLR
    public byte CornerSettings;
}