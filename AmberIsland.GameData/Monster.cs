using Amber.IO.Common.Serialization;

namespace AmberIsland.GameData;

public enum MonsterBehavior : byte
{
    /// <summary>
    /// Monster only attacks if it was hit by the player.
    /// </summary>
    Passive,
    /// <summary>
    /// Monster immediately chases the player if he is in vision range.
    /// </summary>
    Aggressive,
    /// <summary>
    /// Like passive, but cannot move.
    /// </summary>
    StationaryPassive,
    /// <summary>
    /// Like Aggressive, but cannot move.
    /// </summary>
    StationaryAggressive,
}

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

public readonly record struct Monster
(
    Race Race,
    Element Element,
    MonsterBehavior MonsterBehavior,
    MonsterLowHpBehavior MonsterLowHpBehavior,
    uint HitPoints,
    uint MinAttackDamage,
    uint MaxAttackDamage,
    uint MinMagicDamage,
    uint MaxMagicDamage,
    uint PhysicalDefense,
    uint MagicDefense,
    byte PhysicalDamageReduction, // %
    byte MagicDamageReduction, // %
    byte AttackRange,
    byte VisionRange,
    uint Hit,
    uint Dodge,
    uint Experience,
    ushort BossMonsterIndex, // 0 = none
    ushort MinionMonsterIndex, // 0 = none
    ushort MoveSpeed, // Pixels per (real) minute (0 or 0.0167 to 1092.25 pixel/s)
    ushort AttackSpeed // Swings per (real) minute (0 or 0.0167 to 1092.25 swings/s)
    // TODO: spells
)
{
    public void Write(IDataWriter writer)
    {
        writer.Write((byte)Race);
        writer.Write((byte)Element);
        writer.Write((byte)MonsterBehavior);
        writer.Write((byte)MonsterLowHpBehavior);
        writer.Write(HitPoints);        
        writer.Write(MinAttackDamage);
        writer.Write(MaxAttackDamage);
        writer.Write(MinMagicDamage);
        writer.Write(MaxMagicDamage);
        writer.Write(PhysicalDefense);
        writer.Write(MagicDefense);
        writer.Write(PhysicalDamageReduction);
        writer.Write(MagicDamageReduction);
        writer.Write(AttackRange);
        writer.Write(VisionRange);
        writer.Write(Hit);
        writer.Write(Dodge);
        writer.Write(Experience);
        writer.Write(BossMonsterIndex);
        writer.Write(MinionMonsterIndex);
        writer.Write(MoveSpeed);
        writer.Write(AttackSpeed);
    }

    public static Monster Read(IDataReader reader)
    {
        var race = (Race)reader.ReadByte();
        var element = (Element)reader.ReadByte();
        var behavior = (MonsterBehavior)reader.ReadByte();
        var lowHpBehavior = (MonsterLowHpBehavior)reader.ReadByte();
        var hitPoints = reader.ReadDword();
        var minAttackDamage = reader.ReadDword();
        var maxAttackDamage = reader.ReadDword();
        var minMagicDamage = reader.ReadDword();
        var maxMagicDamage = reader.ReadDword();
        var physicalDefense = reader.ReadDword();
        var magicDefense = reader.ReadDword();
        var physicalDamageReduction = reader.ReadByte();
        var magicDamageReduction = reader.ReadByte();
        var attackRange = reader.ReadByte();
        var visionRange = reader.ReadByte();
        var hit = reader.ReadDword();
        var dodge = reader.ReadDword();
        var exp = reader.ReadDword();
        var bossMonsterIndex = reader.ReadWord();
        var minionMonsterIndex = reader.ReadWord();
        var moveSpeed = reader.ReadWord();
        var attackSpeed = reader.ReadWord();

        return new(race, element, behavior, lowHpBehavior, hitPoints, minAttackDamage, maxAttackDamage,
            minMagicDamage, maxMagicDamage, physicalDefense, magicDefense, physicalDamageReduction, magicDamageReduction,
            attackRange, visionRange, hit, dodge, exp, bossMonsterIndex, minionMonsterIndex, moveSpeed, attackSpeed);
    }
}