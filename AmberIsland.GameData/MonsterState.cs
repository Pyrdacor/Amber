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
    // Note: The following states have no own animations.
    // They are partial states of other states with animations
    // and fallback to those states when it comes to animation
    // information retrieval via GetAnimation().
    // They use indices over ushort.MaxValue so the file container
    // can never provide a valid animation file for them.
    PlayAttackAnimation = ushort.MaxValue + 1,
    PlayCastAnimation,
    PlayHurtAnimation,
    PlayDieAnimation,
    PlaySummonAnimation,
}

public static class MonsterStateExtensions
{
    public static bool IsAggressive(this MonsterState state) => state switch
    {
        MonsterState.Chasing => true,
        MonsterState.Attacking => true,
        MonsterState.Casting => true,
        MonsterState.PlayAttackAnimation => true,
        MonsterState.PlayCastAnimation => true,
        _ => false
    };
}
