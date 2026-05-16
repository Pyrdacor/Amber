using Amberstar.GameData.Serialization;

namespace Amberstar.GameData;

public interface IItem
{
    uint Index { get; }
    ItemType Type { get; }
    ItemGraphic GraphicIndex { get; }
    AmmoType UsedAmmoType { get; }
    GenderFlags Genders { get; }
    byte Hands { get; }
    byte Fingers { get; }
    byte HitPoints { get; }
    byte SpellPoints { get; }
    Attribute? Attribute { get; }
    byte AttributeValue { get; }
    Skill? Skill { get; }
    byte SkillValue { get; }
    SpellSchool SpellSchool { get; }
    byte SpellIndex { get; }
    byte SpellCharges { get; }
    AmmoType AmmoType { get; }
    byte Defense { get; }
    byte Damage { get; }
    EquipmentSlot? EquipmentSlot { get; }
    byte MagicWeaponBonus { get; }
    byte MagicArmorBonus { get; }
    byte SpecialIndex { get; }
    byte InitialCharges { get; }
    byte MaxCharges { get; }
    ItemFlags Flags { get; }
    Skill? MalusSkill1 { get; }
    Skill? MalusSkill2 { get; }
    byte Malus1 { get; }
    byte Malus2 { get; }
    byte TextIndex { get; }
    ClassFlags UsableClasses { get; }
    word BuyPrice { get; }
    word Weight { get; }
    word NameIndex { get; }

    IItem Clone();
}

public static class ItemExtensions
{
    public static bool IsTwoHanded(this IItem item)
    {
        // TODO ...
        return false;
    }

    public static bool CanThrowAway(this IItem item)
    {
        // TODO ...
        return false;
    }

    public static bool IsStackable(this IItem item)
    {
        // TODO ...
        return false;
    }
}