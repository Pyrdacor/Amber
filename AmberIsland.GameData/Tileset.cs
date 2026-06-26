using Amber.IO.Common.Serialization;

namespace AmberIsland.GameData;

[Flags]
public enum TileFlags : byte
{
    None                    = 0,
    WaveAnimation           = 1 << 0, // If set animation frames go back and forth instead of cycle
    IgnorePlayerBlocking    = 1 << 1, // If set, BlockedTravel is ignored for the player (also used for dropped items cause player must reach them)
    IgnoreMonsterBlocking   = 1 << 2, // If set, BlockedTravel is ignored for all monsters
    IgnoreNPCBlocking       = 1 << 3, // If set, BlockedTravel is ignored for all NPCs
    IgnoreObjectBlocking    = 1 << 4, // If set, BlockedTravel is ignored for all map objects (like moved blocks)
    BlocksSight             = 1 << 5, // Enemies cannot see you behind it
    RandomAnimationStart    = 1 << 6, // Animation starts randomly and pauses after one cycle (or 1 back and forth for wave animations)
    UseLowerLayerFlags      = 1 << 7, // If set, uses the flags of the layers below. If there is none below, still use it.
}

public enum TileType : byte
{
    Grass, // Default
    Stone, // Different walking sound
    Earth, // Different walking sound
    Wood, // Different walking sound
    ShallowWater, // Movement is slowed
    DeepWater, // Swim
    Mud, // Movement is slowed
    Sand, // Movement is slowed
    HotSand, // Movement is slowed, hot
    Snow, // Movement is slowed, cold
    ShallowLava, // Burn
    DeepLava, // Burn + swim
    ShallowIceWater, // Freeze
    DeepIceWater, // Freeze + swim
    Ice, // Slippery
    Poison, // Damage
    Swamp, // Mud + Poison
    HighGrass, // Slowed a bit
    QuickSand, // Slowed and pulled, die when not moving too long
    Cobweb, // Slowed or paralyzed
    Sky, // Fly
    WaterFlowDown,
    WaterFlowUp,
    WaterFlowRight,
    WaterFlowLeft,
    WindFlowDown,
    WindFlowUp,
    WindFlowRight,
    WindFlowLeft,
    ChairDown,
    ChairUp,
    ChairRight,
    ChairLeft,
    BedDown,
    BedUp,
    BedRight,
    BedLeft,
    BreakablePickup, // Pick up, throw, breaks
    UnbreakablePickup, // Pick up, put down
    Pushable, // Only push it
    HeavyPushable, // Only push it but it's heavy
    Pullable, // Only pull it
    HeavyPullable, // Only pull it  but it's heavy
    PushAndPullable, // Push or pull it
    HeavePushAndPullable, // Push or pull it but it's heavy
    Collectable, // Can be collected (consumed on walk over)
    Trampoline, // Entering the tile let's you jump high into the air and you can freely choose a direction
    TrampolineHigh, // Like Trampoline but with higher altitude
    CanonDown, // Hides player when tile is entered and shoots you downwards
    CanonUp, // Hides player when tile is entered and shoots you upwards
    CanonRight, // Hides player when tile is entered and shoots you to the right
    CanonLeft, // Hides player when tile is entered and shoots you to the left
    CatapultDown, // Like CanonDown but instead of hiding the player lies on it
    CatapultUp, // Like CanonUp but instead of hiding the player lies on it
    CatapultRight, // Like CanonRight but instead of hiding the player lies on it
    CatapultLeft, // Like CanonLeft but instead of hiding the player lies on it
    Ladder, // Also vines, rocks and stuff
}

public readonly record struct Tile
(
    TileType Type,
    TileFlags Flags,
    byte FrameCount,    // > 1 for animation, 1 = normal tile
    byte BlockedTravel, // 1 bit per TravelType (0 = not blocked, 1 = blocked)
    ushort ImageIndex   // Index into the tileset's graphic atlas
)
{
    public void Write(IDataWriter writer)
    {
        writer.Write((byte)Type);
        writer.Write((byte)Flags);
        writer.Write(FrameCount);
        writer.Write(BlockedTravel);
        writer.Write(ImageIndex);
    }

    public static Tile Read(IDataReader reader)
    {
        var type = (TileType)reader.ReadByte();
        var flags = (TileFlags)reader.ReadByte();
        var frameCount = reader.ReadByte();
        var blockedTravel = reader.ReadByte();
        var imageIndex = reader.ReadWord();

        return new(type, flags, frameCount, blockedTravel, imageIndex);
    }
}

public record Tileset(uint GraphicAtlasIndex, Tile[] Tiles)
{
    public void Write(IDataWriter writer)
    {
        writer.Write((ushort)GraphicAtlasIndex);
        writer.Write((ushort)Tiles.Length);

        foreach (var tile in Tiles)
            tile.Write(writer);
    }

    public static Tileset Read(IDataReader reader)
    {
        uint graphicAtlasIndex = reader.ReadWord();
        var tiles = new Tile[reader.ReadWord()];

        for (int i = 0; i < tiles.Length; i++)
            tiles[i] = Tile.Read(reader);

        return new(graphicAtlasIndex, tiles);
    }
}
