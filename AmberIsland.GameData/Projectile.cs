using Amber.IO.Common.Serialization;

namespace AmberIsland.GameData;

[Flags]
public enum ProjectileFlags : byte
{
    None                    = 0,
    ChasePlayer             = 1 << 0,
    ExplodeOnImpact         = 1 << 1,
    CanCollideWithTerrain   = 1 << 2, // Uses flying object block mode
    CanHitMonsters          = 1 << 3, // Uses flying monster block mode (this not automatically implies damaging them! only use it if it should stop at monsters but don't hurt them)
    CanDamageMonsters       = 1 << 4, // This implies hitting the monster
    CanDamagePlayer         = 1 << 5, // This implies hitting the player
}

public readonly record struct Projectile
(
    // Byte-sized
    Element Element,
    ProjectileFlags Flags,
    byte VisionRange, // As a radius in tiles (used for player chasing)
    byte PaletteIndex, // 0-based index inside the monster sprite palettes (mostly 0 or a very low integer)
    byte ScaleFactor, // 4.4 float which means 0x08 is 0.5 and 0x10 is 1.0 (which means ScaleFactor * Width / 16 gives the render width)
    // Word-sized
    ushort MoveSpeed, // Pixels per (real) minute (0 or 0.0167 to 1092.25 pixel/s)
    ushort MaxTravelDistance, // If ExplodeOnImpact is set, it will explode after that (and dealing damage to nearby targets), otherwise it just vanishes
    ushort CollisionOffsetX,
    ushort CollisionOffsetY,
    ushort CollisionRadius,
    // Dword-sized
    uint MinPhysicalDamage,
    uint MaxPhysicalDamage,
    uint MinMagicDamage,
    uint MaxMagicDamage,
    uint MinExplosionDamage,
    uint MaxExplosionDamage,
    uint HitBonus
)
{
    public void Write(IDataWriter writer)
    {
        // Byte-sized
        writer.Write((byte)Element);
        writer.Write((byte)Flags);
        writer.Write(VisionRange);
        writer.Write(PaletteIndex);
        writer.Write(ScaleFactor);

        // Word-sized
        writer.Write(MoveSpeed);
        writer.Write(MaxTravelDistance);
        writer.Write(CollisionOffsetX);
        writer.Write(CollisionOffsetY);
        writer.Write(CollisionRadius);

        // Dword-sized       
        writer.Write(MinPhysicalDamage);
        writer.Write(MaxPhysicalDamage);
        writer.Write(MinMagicDamage);
        writer.Write(MaxMagicDamage);
        writer.Write(MinExplosionDamage);
        writer.Write(MaxExplosionDamage);  
        writer.Write(HitBonus);        
    }

    public static Projectile Read(IDataReader reader)
    {
        // Byte-sized
        var element = (Element)reader.ReadByte();
        var flags = (ProjectileFlags)reader.ReadByte();
        var visionRange = reader.ReadByte();
        var paletteIndex = reader.ReadByte();
        var scaleFactor = reader.ReadByte();

        // Word-sized
        var moveSpeed = reader.ReadWord();
        var maxTravelDistance = reader.ReadWord();
        var collisionOffsetX = reader.ReadWord();
        var collisionOffsetY = reader.ReadWord();
        var collisionRadius = reader.ReadWord();

        // Dword-sized
        var minPhysicalDamage = reader.ReadDword();
        var maxPhysicalDamage = reader.ReadDword();
        var minMagicDamage = reader.ReadDword();
        var maxMagicDamage = reader.ReadDword();
        var minExplosionDamage = reader.ReadDword();
        var maxExplosionDamage = reader.ReadDword();
        var hitBonus = reader.ReadDword();

        return new
        (
            // Byte-sized
            element,
            flags,
            visionRange,
            paletteIndex,
            scaleFactor,
            // Word-sized
            moveSpeed,
            maxTravelDistance,
            collisionOffsetX,
            collisionOffsetY,
            collisionRadius,
            // Dword-sized
            minPhysicalDamage,
            maxPhysicalDamage,
            minMagicDamage,
            maxMagicDamage,
            minExplosionDamage,
            maxExplosionDamage,
            hitBonus
        );
    }
}