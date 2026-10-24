namespace Amberstar.GameData;

public record MonsterSpell(SpellSchool School, byte SpellIndex);

public interface IMonster : IBattleCharacter
{
    byte BattleGraphicIndex { get; init; }
    byte Morale { get; init; }
    byte SpellCastChance { get; init; }
    byte MagicHitBonus { get; init; }
    /// <summary>
    /// The distance from the front row which the monster tries to keep in battles.
    /// </summary>
    byte BattlePosition { get; init; }
    word DefeatExperience { get; init; }
    MonsterFlags MonsterFlags { get; init; }
    MonsterElementalFlags ElementalFlags { get; init; }
    /// <summary>
    /// Spells are randomly picked from this list.
    /// A spell can exist multiple times to increase it's chance to be picked.
    /// </summary>
    MonsterSpell[] Spells { get; init; }
    /// <summary>
    /// The 25 spell slots including the empty ones (null). Battles pick a random slot.
    /// </summary>
    MonsterSpell?[] SpellSlots { get; }

    IMonster Clone();
}