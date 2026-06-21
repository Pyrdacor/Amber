using Amber.IO.Common.Serialization;
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

    public required ItemSlotFlags SlotFlags { get; init; }

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
            SlotFlags = SlotFlags,
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

    public void Write(IDataWriter dataWriter)
    {
        dataWriter.Write((word)Index);
        dataWriter.Write((byte)Type);
        dataWriter.Write((byte)GraphicIndex);
        dataWriter.Write((byte)UsedAmmoType);
        dataWriter.Write((byte)Genders);
        dataWriter.Write((byte)Hands);
        dataWriter.Write((byte)Fingers);
        dataWriter.Write((byte)HitPoints);
        dataWriter.Write((byte)SpellPoints);
        dataWriter.Write((byte)Attribute);
        dataWriter.Write((byte)AttributeValue);
        dataWriter.Write((byte)Skill);
        dataWriter.Write((byte)SkillValue);
        dataWriter.Write((byte)SpellSchool);
        dataWriter.Write((byte)SpellIndex);
        dataWriter.Write((byte)SpellCharges);
        dataWriter.Write((byte)AmmoType);
        dataWriter.Write((byte)Defense);
        dataWriter.Write((byte)Damage);
        dataWriter.Write((byte)(EquipmentSlot == null ? 0 : EquipmentSlot.Value + 1));
        dataWriter.Write((byte)MagicWeaponBonus);
        dataWriter.Write((byte)MagicArmorBonus);
        dataWriter.Write((byte)SpecialIndex);
        dataWriter.Write((byte)InitialCharges);
        dataWriter.Write((byte)MaxCharges);
        dataWriter.Write((byte)((byte)Flags | (byte)SlotFlags));
        dataWriter.Write((byte)(MalusSkill1 == null ? 0 : MalusSkill1.Value + 1));
        dataWriter.Write((byte)(MalusSkill2 == null ? 0 : MalusSkill2.Value + 1));
        dataWriter.Write((byte)Malus1);
        dataWriter.Write((byte)Malus2);
        dataWriter.Write((byte)TextIndex);
        dataWriter.Write((word)UsableClasses);
        dataWriter.Write((word)BuyPrice);
        dataWriter.Write((word)Weight);
        dataWriter.Write((word)NameIndex);
    }
}

internal class ItemFactory : IItemFactory
{
    public IItem Create(IStaticItemData itemData, IItemSlot itemSlotData)
    {
        return new Item
        {
            Index = itemData.Index,
            Type = itemData.Type,
            GraphicIndex = itemData.GraphicIndex,
            UsedAmmoType = itemData.UsedAmmoType,
            Genders = itemData.Genders,
            Hands = itemData.Hands,
            Fingers = itemData.Fingers,
            HitPoints = itemData.HitPoints,
            SpellPoints = itemData.SpellPoints,
            Attribute = itemData.Attribute,
            AttributeValue = itemData.AttributeValue,
            Skill = itemData.Skill,
            SkillValue = itemData.SkillValue,
            SpellSchool = itemData.SpellSchool,
            SpellIndex = itemData.SpellIndex,
            SpellCharges = itemSlotData.SpellCharges,
            AmmoType = itemData.AmmoType,
            Defense = itemData.Defense,
            Damage = itemData.Damage,
            EquipmentSlot = itemData.EquipmentSlot,
            MagicWeaponBonus = itemData.MagicWeaponBonus,
            MagicArmorBonus = itemData.MagicArmorBonus,
            SpecialIndex = itemData.SpecialIndex,
            InitialCharges = itemData.InitialCharges,
            MaxCharges = itemData.MaxCharges,
            Flags = itemData.Flags,
            SlotFlags = itemSlotData.SlotFlags,
            MalusSkill1 = itemData.MalusSkill1,
            MalusSkill2 = itemData.MalusSkill2,
            Malus1 = itemData.Malus1,
            Malus2 = itemData.Malus2,
            TextIndex = itemData.TextIndex,
            UsableClasses = itemData.UsableClasses,
            BuyPrice = itemData.BuyPrice,
            Weight = itemData.Weight,
            NameIndex = itemData.NameIndex
        };
    }
}