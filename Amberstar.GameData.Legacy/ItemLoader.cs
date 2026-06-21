using Amber.IO.Common.Serialization;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy
{
	internal class ItemLoader : IItemLoader
	{
        readonly Dictionary<uint, IStaticItemData> items = [];

        public ItemLoader()
        {

        }

        public ItemLoader(Lazy<IMonsterLoader> monsterLoader, Lazy<IPersonLoader> personLoader, Lazy<IChestLoader> chestLoader)
        {
            IEnumerable<ICharacter> monsters = monsterLoader.Value.LoadAllMonsters().Values;
            IEnumerable<ICharacter> persons = personLoader.Value.LoadAllPersons().Values;
            var chests = chestLoader.Value.LoadAllChests();

            foreach (ICharacter character in monsters.Concat(persons))
            {
                foreach (var itemSlot in character.Inventory)
                {
                    if (itemSlot?.Item == null || itemSlot.Count == 0)
                        continue;

                    if (!items.ContainsKey(itemSlot.Item.Index))
                    {
                        items.Add(itemSlot.Item.Index, itemSlot.Item);
                    }
                }
            }

            foreach (var chest in chests.Values)
            {
                foreach (var item in chest.Items)
                {
                    if (item == null)
                        continue;

                    if (!items.ContainsKey(item.Index))
                    {
                        items.Add(item.Index, item);
                    }
                }
            }
        }

        public IStaticItemData LoadItem(uint index) => items[index];

        public IItem ReadItem(IDataReader reader)
        {
            var graphicIndex = (ItemGraphic)reader.ReadByte();
            var itemType = (ItemType)reader.ReadByte();
            var usedAmmoType = (AmmoType)reader.ReadByte();
            var genders = (GenderFlags)reader.ReadByte();
            var numHands = reader.ReadByte();
            var numFingers = reader.ReadByte();
            var hitPoints = reader.ReadByte();
            var spellPoints = reader.ReadByte();
            var attribute = (Attribute)reader.ReadByte();
            var attributeValue = reader.ReadByte();
            var skill = (Skill)reader.ReadByte();
            var skillValue = reader.ReadByte();
            var spellSchool = (SpellSchool)reader.ReadByte();
            var spellIndex = reader.ReadByte();
            var spellCharges = reader.ReadByte();
            var ammoType = (AmmoType)reader.ReadByte();
            var defense = reader.ReadByte();
            var damage = reader.ReadByte();
            var equipmentSlot = (EquipmentSlot)reader.ReadByte();
            var magicWeaponBonus = reader.ReadByte();
            var magicArmorBonus = reader.ReadByte();
            var specialIndex = reader.ReadByte();
            var initialCharges = reader.ReadByte();
            var maxCharges = reader.ReadByte();
            var itemFlags = reader.ReadByte();
            var malusSkill1 = reader.ReadByte();
            var malusSkill2 = reader.ReadByte();
            var malus1 = reader.ReadByte();
            var malus2 = reader.ReadByte();
            var textIndex = reader.ReadByte();
            var usableClasses = (ClassFlags)reader.ReadWord();
            var buyPrice = reader.ReadWord();
            var weight = reader.ReadWord();
            var index = reader.ReadWord();
            var nameIndex = reader.ReadWord();

            return new Item()
            {
                Index = index,
                Type = itemType,
                GraphicIndex = graphicIndex,
                UsedAmmoType = usedAmmoType,
                Genders = genders,
                Hands = numHands,
                Fingers = numFingers,
                HitPoints = hitPoints,
                SpellPoints = spellPoints,
                Attribute = attribute,
                AttributeValue = attributeValue,
                Skill = skill,
                SkillValue = skillValue,
                SpellSchool = spellSchool,
                SpellIndex = spellIndex,
                SpellCharges = spellCharges,
                AmmoType = ammoType,
                Defense = defense,
                Damage = damage,
                EquipmentSlot = equipmentSlot == 0 ? null : (equipmentSlot - 1),
                MagicWeaponBonus = magicWeaponBonus,
                MagicArmorBonus = magicArmorBonus,
                SpecialIndex = specialIndex,
                InitialCharges = initialCharges,
                MaxCharges = maxCharges,
                Flags = (ItemFlags)(itemFlags & 0x7f),
                SlotFlags = (ItemSlotFlags)(itemFlags & 0x80),
                MalusSkill1 = malusSkill1 == 0 ? null : (Skill?)(malusSkill1 - 1),
                MalusSkill2 = malusSkill2 == 0 ? null : (Skill?)(malusSkill2 - 1),
                Malus1 = malus1,
                Malus2 = malus2,
                TextIndex = textIndex,
                UsableClasses = usableClasses,
                BuyPrice = buyPrice,
                Weight = weight,
                NameIndex = nameIndex
            };
        }
    }
}