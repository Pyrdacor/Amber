namespace AmberIsland.Game;

internal abstract class CombatMapActor(Game game, ActorType actorType) : MapActor(game, actorType)
{
    private protected abstract uint TotalPhysicalMinDamage { get; }
    private protected abstract uint TotalPhysicalMaxDamage { get; }
    private protected abstract uint TotalMagicalMinDamage { get; }
    private protected abstract uint TotalMagicalMaxDamage { get; }
    private protected abstract uint TotalPhysicalDefense { get; }
    private protected abstract uint TotalMagicalDefense { get; }
    private protected abstract uint TotalHit { get; }
    private protected abstract uint TotalDodge { get; }
    private protected abstract uint Level { get; }
}
