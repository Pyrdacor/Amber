using AmberIsland.Game.Map;
using AmberIsland.GameData;

namespace AmberIsland.Game;

internal abstract class CombatMapActor(Game game, ActorType actorType) : MapActor(game, actorType)
{
    private protected abstract uint TotalPhysicalMinDamage { get; }
    private protected abstract uint TotalPhysicalMaxDamage { get; }
    private protected abstract uint TotalMagicalMinDamage { get; }
    private protected abstract uint TotalMagicalMaxDamage { get; }
    private protected abstract uint TotalPhysicalDefense { get; }
    private protected abstract uint TotalMagicalDefense { get; }
    private protected abstract uint TotalPhysicalDamageReduction { get; } // %
    private protected abstract uint TotalMagicalDamageReduction { get; } // %
    private protected abstract uint TotalHit { get; }
    private protected abstract uint TotalDodge { get; }
    private protected abstract uint Level { get; }
    // TODO: Use elements
    private protected abstract Element AttackElement { get; }
    private protected abstract Element DefendElement { get; }

    public bool TestHit(CombatMapActor target)
    {
        uint chance = Math.Clamp(Level + TotalHit + 50 - target.TotalDodge - target.Level, 5, 100);

        return Game.TestChance(chance);
    }

    public uint CalculcatePhysicalDamage(CombatMapActor target)
    {
        if (target.TotalPhysicalDamageReduction >= 100)
            return 0;

        int damage = Game.Random((int)TotalPhysicalMinDamage, (int)TotalPhysicalMaxDamage);
        damage -= (int)target.TotalPhysicalDefense;

        if (damage <= 0)
            return 0;

        damage = (100 - (int)target.TotalPhysicalDamageReduction) * damage / 100;

        return (uint)damage;
    }

    public uint CalculcateMagicalDamage(CombatMapActor target)
    {
        if (target.TotalMagicalDamageReduction >= 100)
            return 0;

        int damage = Game.Random((int)TotalMagicalMinDamage, (int)TotalMagicalMaxDamage);
        damage -= (int)target.TotalMagicalDefense;

        if (damage <= 0)
            return 0;

        damage = (100 - (int)target.TotalMagicalDamageReduction) * damage / 100;

        return (uint)damage;
    }

    // TODO
}
