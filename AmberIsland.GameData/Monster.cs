using Amber.IO.Common.Serialization;

namespace AmberIsland.GameData;

public enum MonsterLowHpBehavior : byte
{
    /// <summary>
    /// Nothing special.
    /// </summary>
    Normal,
    /// <summary>
    /// Try to run away when low.
    /// </summary>
    Flee,
    /// <summary>
    /// Teleport when low (cannot happen for some time again).
    /// </summary>
    Teleport,
    /// <summary>
    /// Spawn minions when low.
    /// </summary>
    Spawn,
}

[Flags]
public enum MonsterFlags : byte
{
    /// <summary>
    /// If set, the monster immediately chases the player if he is in vision range.
    /// If not, the monster only attacks if it was hit by the player.
    /// </summary>
    Aggressive  = 1 << 0,
    CanWalk     = 1 << 1,
    CanSwim     = 1 << 2,
    CanFly      = 1 << 3,
}

public static class MonsterFlagsExtensions
{
    public static bool CanMove(this MonsterFlags flags)
    {
        return ((byte)flags & 0xe) != 0;
    }
}

// TODO: Add collision area to monster data
public readonly record struct Monster
(
    // Byte-sized
    Race Race,
    Element Element,
    MonsterFlags Flags,
    MonsterLowHpBehavior LowHpBehavior,
    byte PhysicalDamageReduction, // %
    byte MagicDamageReduction, // %
    byte AttackRange, // As a radius in tiles
    byte VisionRange, // As a radius in tiles
    byte MoveRange, // In tiles (but each direction step counts as 1 tile)
    byte LowHitpointDivisor, // hitpoints below MaxHP / LowHitpointDivisor are considered as "low health" for MonsterLowHpBehavior
    // Word-sized
    ushort BossMonsterIndex, // 0 = none
    ushort MinionMonsterIndex, // 0 = none
    ushort MoveSpeed, // Pixels per (real) minute (0 or 0.0167 to 1092.25 pixel/s)
    ushort AttackSpeed, // Swings per (real) minute (0 or 0.0167 to 1092.25 swings/s)
    ushort MinDecisionDelay, // Minimum time in milliseconds before the next decision is evaluated (in idle state)
    ushort MaxDecisionDelay, // Maximum time in milliseconds before the next decision is evaluated (in idle state)
                             // Dword-sized
    uint HitPoints,
    uint MinAttackDamage,
    uint MaxAttackDamage,
    uint MinMagicDamage,
    uint MaxMagicDamage,
    uint PhysicalDefense,
    uint MagicDefense,    
    uint Hit,
    uint Dodge,
    uint Experience
    // TODO: spells
)
{
    public void Write(IDataWriter writer)
    {
        // Byte-sized
        writer.Write((byte)Race);
        writer.Write((byte)Element);
        writer.Write((byte)Flags);
        writer.Write((byte)LowHpBehavior);
        writer.Write(PhysicalDamageReduction);
        writer.Write(MagicDamageReduction);
        writer.Write(AttackRange);
        writer.Write(VisionRange);
        writer.Write(MoveRange);
        writer.Write(LowHitpointDivisor);

        // Word-sized
        writer.Write(BossMonsterIndex);
        writer.Write(MinionMonsterIndex);
        writer.Write(MoveSpeed);
        writer.Write(AttackSpeed);
        writer.Write(MinDecisionDelay);
        writer.Write(MaxDecisionDelay);

        // Dword-sized
        writer.Write(HitPoints);        
        writer.Write(MinAttackDamage);
        writer.Write(MaxAttackDamage);
        writer.Write(MinMagicDamage);
        writer.Write(MaxMagicDamage);
        writer.Write(PhysicalDefense);
        writer.Write(MagicDefense);        
        writer.Write(Hit);
        writer.Write(Dodge);
        writer.Write(Experience);
        
    }

    public static Monster Read(IDataReader reader)
    {
        // Byte-sized
        var race = (Race)reader.ReadByte();
        var element = (Element)reader.ReadByte();
        var flags = (MonsterFlags)reader.ReadByte();
        var lowHpBehavior = (MonsterLowHpBehavior)reader.ReadByte();
        var physicalDamageReduction = reader.ReadByte();
        var magicDamageReduction = reader.ReadByte();
        var attackRange = reader.ReadByte();
        var visionRange = reader.ReadByte();
        var moveRange = reader.ReadByte();
        var lowHitpointDivisor = reader.ReadByte();

        // Word-sized
        var bossMonsterIndex = reader.ReadWord();
        var minionMonsterIndex = reader.ReadWord();
        var moveSpeed = reader.ReadWord();
        var attackSpeed = reader.ReadWord();
        var minDecisionDelay = reader.ReadWord();
        var maxDecisionDelay = reader.ReadWord();

        // Dword-sized
        var hitPoints = reader.ReadDword();
        var minAttackDamage = reader.ReadDword();
        var maxAttackDamage = reader.ReadDword();
        var minMagicDamage = reader.ReadDword();
        var maxMagicDamage = reader.ReadDword();
        var physicalDefense = reader.ReadDword();
        var magicDefense = reader.ReadDword();
        var hit = reader.ReadDword();
        var dodge = reader.ReadDword();
        var exp = reader.ReadDword();

        return new
        (
            // Byte-sized
            race,
            element,
            flags,
            lowHpBehavior,
            physicalDamageReduction,
            magicDamageReduction,
            attackRange, 
            visionRange,
            moveRange,
            lowHitpointDivisor,
            // Word-sized
            bossMonsterIndex, 
            minionMonsterIndex,
            moveSpeed,
            attackSpeed,
            minDecisionDelay,
            maxDecisionDelay,
            // Dword-sized
            hitPoints,
            minAttackDamage,
            maxAttackDamage,
            minMagicDamage,
            maxMagicDamage,
            physicalDefense,
            magicDefense,
            hit,
            dodge,
            exp
        );
    }
}