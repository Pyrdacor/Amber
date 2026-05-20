using System.Collections.ObjectModel;

namespace Amberstar.GameData;

public interface IBattleCharacter : ICharacter
{
    byte UsedHands { get; set; }
    byte UsedFingers { get; set; }
    byte Defense { get; set; }
    byte Damage { get; set; }
    word BonusDefense { get; set; }
    word BonusDamage { get; set; }
    PhysicalCondition PhysicalConditions { get; set; }
    MentalCondition MentalConditions { get; set; }
    public byte MagicBonusWeapon { get; set; }
    public byte MagicBonusArmor { get; set; }
    public byte AttacksPerRound { get; set; }
    CharacterValue HitPoints { get; }
    CharacterValue SpellPoints { get; }
    ReadOnlyDictionary<Skill, CharacterValue> Skills { get; }
    ReadOnlyDictionary<Attribute, CharacterValue> Attributes { get; }
}

public static class BattleCharacterExtensions
{
    public static bool IsDead(this IBattleCharacter character) =>
        character.PhysicalConditions.HasFlag(PhysicalCondition.Dead) ||
        character.PhysicalConditions.HasFlag(PhysicalCondition.Ashes) ||
        character.PhysicalConditions.HasFlag(PhysicalCondition.Dust);

    public static void Damage(this IBattleCharacter character, word amount, Action? finishHandler = null)
    {
        character.HitPoints.CurrentValue = (word)Math.Max(character.HitPoints.CurrentValue - amount, 0);

        if (character.HitPoints.CurrentValue == 0)
        {
            character.AddCondition(Condition.Dead);

            // TODO
        }

        finishHandler?.Invoke();
    }

    public static void HealHitPoints(this IBattleCharacter character, word amount)
    {
        character.HitPoints.CurrentValue = (word)Math.Min(character.HitPoints.CurrentValue + amount, character.HitPoints.TotalMax);
    }

    public static void HealSpellPoints(this IBattleCharacter character, word amount)
    {
        character.SpellPoints.CurrentValue = (word)Math.Min(character.SpellPoints.CurrentValue + amount, character.SpellPoints.TotalMax);
    }

    public static void FillHitPoints(this IBattleCharacter character)
    {
        character.HitPoints.CurrentValue = character.HitPoints.TotalMax;
    }

    public static void FillSpellPoints(this IBattleCharacter character)
    {
        character.SpellPoints.CurrentValue = character.SpellPoints.TotalMax;
    }

    public static void AddCondition(this IBattleCharacter battleCharacter, Condition condition)
    {
        if (condition == Condition.None)
            return;

        battleCharacter.PhysicalConditions |= condition.ToPhysical();
        battleCharacter.MentalConditions |= condition.ToMental();
    }

    public static void RemoveCondition(this IBattleCharacter battleCharacter, Condition condition)
    {
        battleCharacter.PhysicalConditions &= ~condition.ToPhysical();
        battleCharacter.MentalConditions &= ~condition.ToMental();
    }

    public static Condition GetConditions(this IBattleCharacter battleCharacter)
    {
        return battleCharacter.PhysicalConditions.MergeWith(battleCharacter.MentalConditions);
    }

    public static bool HasAnyConditionOf(this IBattleCharacter battleCharacter, Condition conditions)
    {
        return (battleCharacter.PhysicalConditions & conditions.ToPhysical()) != 0 ||
               (battleCharacter.MentalConditions & conditions.ToMental()) != 0;
    }

    public static bool CanMove(this IBattleCharacter battleCharacter, bool inBattle)
    {
        if (inBattle)
            return !battleCharacter.HasAnyConditionOf(Condition.Stunned | Condition.Sleeping | Condition.Petrified | Condition.Mad | Condition.Overloaded | Condition.Panicked);

        return !battleCharacter.HasAnyConditionOf(Condition.Overloaded);
    }
}