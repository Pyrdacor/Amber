using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy;

internal class Item : IItem
{
    public required uint Index { get; init; }

    public required ItemType Type { get; init; }

    public required ItemGraphic GraphicIndex { get; init; }    

    public required AmmoType UsedAmmoType { get; init; }

    public required GenderFlags Genders { get; init; }

    public required byte Hands { get; init; }

    public required byte Fingers { get; init; }

    public required byte HitPoints { get; init; }

    public required byte SpellPoints { get; init; }

    public required Attribute Attribute { get; init; }

    public required byte AttributeValue { get; init; }

    public required Skill Skill { get; init; }

    public required byte SkillValue { get; init; }

    public required SpellSchool SpellSchool { get; init; }

    public required byte SpellIndex { get; init; }

    public required byte SpellCharges { get; init; }

    public required AmmoType AmmoType { get; init; }

    public required byte Defense { get; init; }

    public required byte Damage { get; init; }

    public required EquipmentSlot? EquipmentSlot { get; init; }

    public required byte MagicWeaponBonus { get; init; }

    public required byte MagicArmorBonus { get; init; }

    public required byte SpecialIndex { get; init; }

    public required byte InitialCharges { get; init; }

    public required byte MaxCharges { get; init; }

    public required ItemFlags Flags { get; init; }

    public required Skill? MalusSkill1 { get; init; }

    public required Skill? MalusSkill2 { get; init; }

    public required byte Malus1 { get; init; }

    public required byte Malus2 { get; init; }

    public required byte TextIndex { get; init; }

    public required ClassFlags UsableClasses { get; init; }

    public required word BuyPrice { get; init; }

    public required word Weight { get; init; }

    public required word NameIndex { get; init; }

    public IItem Clone()
    {
        return new Item
        {
            Index = Index,
            Type = Type,
            GraphicIndex = GraphicIndex,
            UsedAmmoType = UsedAmmoType,
            Genders = Genders,
            Hands = Hands,
            Fingers = Fingers,
            HitPoints = HitPoints,
            SpellPoints = SpellPoints,
            Attribute = Attribute,
            AttributeValue = AttributeValue,
            Skill = Skill,
            SkillValue = SkillValue,
            SpellSchool = SpellSchool,
            SpellIndex = SpellIndex,
            SpellCharges = SpellCharges,
            AmmoType = AmmoType,
            Defense = Defense,
            Damage = Damage,
            EquipmentSlot = EquipmentSlot,
            MagicWeaponBonus = MagicWeaponBonus,
            MagicArmorBonus = MagicArmorBonus,
            SpecialIndex = SpecialIndex,
            InitialCharges = InitialCharges,
            MaxCharges = MaxCharges,
            Flags = Flags,
            MalusSkill1 = MalusSkill1,
            MalusSkill2 = MalusSkill2,
            Malus1 = Malus1,
            Malus2 = Malus2,
            TextIndex = TextIndex,
            UsableClasses = UsableClasses,
            BuyPrice = BuyPrice,
            Weight = Weight,
            NameIndex = NameIndex
        };
    }
}
