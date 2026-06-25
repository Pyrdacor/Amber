namespace AmberIsland.GameData;

public enum MonsterState
{
    Idle,
    Walking,
    Chasing,
    Fleeing,
    Sleeping,
    Attacking,
    Casting,
    ReceivingDamage,
    Die,
    Summon,
}

public static class MonsterStateExtensions
{
    public static bool IsAggressive(this MonsterState state) => state switch
    {
        MonsterState.Chasing => true,
        MonsterState.Attacking => true,
        MonsterState.Casting => true,
        _ => false
    };
}
