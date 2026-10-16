using Amberstar.GameData;

namespace Amberstar.Game;

/// <summary>
/// Item handling of the inventory (INVENT.ASM of the original).
/// </summary>
partial class Game
{
	const int MaxItemsPerSlot = 99;

	/// <summary>
	/// Equips an item of the backpack (Equip_item in the original).
	/// Returns the message to show if the item can't be equipped.
	/// </summary>
	internal static Message? EquipItem(IPartyMember partyMember, int inventorySlotIndex)
	{
		var sourceSlot = partyMember.Inventory[inventorySlotIndex];
		var item = sourceSlot.Item;

		if (item == null || sourceSlot.Count == 0 || item.Type == ItemType.MonsterItem)
			return null;

		// TODO: In battle, items with the flag NotEquipDuringFight can't be equipped.

		var targetSlot = item.EquipmentSlot;

		// Rings can be put on both ring fingers.
		if (targetSlot == null || (targetSlot != EquipmentSlot.RightFinger && partyMember.Equipment[targetSlot.Value].Count != 0))
			return Message.ItemNotEquippable;

		if (!item.UsableClasses.HasFlag((ClassFlags)(1 << (int)partyMember.Class)))
			return Message.WrongClass;

		if (item.Genders != GenderFlags.Both && !item.Genders.HasFlag((GenderFlags)(1 << (int)partyMember.Gender)))
			return Message.WrongGender;

		if (item.Hands > 2 - partyMember.UsedHands)
			return Message.NotEnoughFreeHands;

		if (item.Fingers > 2 - partyMember.UsedFingers)
			return Message.NotEnoughFreeFingers;

		if (targetSlot == EquipmentSlot.RightFinger && partyMember.Equipment[EquipmentSlot.RightFinger].Count != 0)
			targetSlot = EquipmentSlot.LeftFinger;

		ApplyEquipmentBoni(partyMember, item, true);
		partyMember.Equipment[targetSlot.Value].SetItem(item, 1);

		if (--sourceSlot.Count == 0)
			sourceSlot.ClearItem();

		return null;
	}

	/// <summary>
	/// Puts an equipped item into the backpack (Unequip_item in the original).
	/// Returns the message to show if the item can't be unequipped.
	/// </summary>
	internal static Message? UnequipItem(IPartyMember partyMember, EquipmentSlot equipmentSlot)
	{
		var sourceSlot = partyMember.Equipment[equipmentSlot];
		var item = sourceSlot.Item;

		if (item == null || sourceSlot.Count == 0 || item.Type == ItemType.MonsterItem)
			return null;

		// TODO: In battle, items with the flag NotEquipDuringFight can't be unequipped.

		if (item.Flags.HasFlag(ItemFlags.Cursed))
			return Message.ItemIsCursed;

		var targetSlot = FindBackpackSlot(partyMember, item);

		if (targetSlot == null)
			return Message.NoRoomForItem;

		ApplyEquipmentBoni(partyMember, item, false);
		sourceSlot.ClearItem();

		if (targetSlot.Count == 0)
			targetSlot.SetItem(item, 1);
		else
			targetSlot.Count++;

		return null;
	}

	/// <summary>
	/// Finds a backpack slot for one item: a slot with the same stackable item
	/// which is not full or else a free slot.
	/// </summary>
	internal static ItemSlot? FindBackpackSlot(IPartyMember partyMember, IItem item)
	{
		if (item.IsStackable())
		{
			var slot = partyMember.Inventory.FirstOrDefault(slot => slot.Count != 0 && slot.Item?.Index == item.Index && slot.Count < MaxItemsPerSlot);

			if (slot != null)
				return slot;
		}

		return partyMember.Inventory.FirstOrDefault(slot => slot.Count == 0 || slot.Item == null);
	}

	/// <summary>
	/// Gives or removes the boni of an equipped item (Positive_boni and Negative_boni
	/// in the original). Cursed items give their boni as mali.
	/// </summary>
	internal static void ApplyEquipmentBoni(IPartyMember partyMember, IItem item, bool equip)
	{
		int sign = equip ? 1 : -1;

		static word Add(word value, int amount) => unchecked((word)(value + amount));

		void AddSkillBonus(Skill? skill, int amount)
		{
			if (skill != null && partyMember.Skills.TryGetValue(skill.Value, out var value))
				value.BonusValue = Add(value.BonusValue, amount);
		}

		partyMember.UsedHands = (byte)(partyMember.UsedHands + sign * item.Hands);
		partyMember.UsedFingers = (byte)(partyMember.UsedFingers + sign * item.Fingers);
		partyMember.MagicBonusWeapon = unchecked((byte)(partyMember.MagicBonusWeapon + sign * item.MagicWeaponBonus));
		partyMember.MagicBonusArmor = unchecked((byte)(partyMember.MagicBonusArmor + sign * item.MagicArmorBonus));

		// skill taxes (e.g. swimming in heavy armor)
		AddSkillBonus(item.MalusSkill1, -sign * (sbyte)item.Malus1);
		AddSkillBonus(item.MalusSkill2, -sign * (sbyte)item.Malus2);

		if (item.Flags.HasFlag(ItemFlags.Cursed))
			sign = -sign;

		partyMember.HitPoints.BonusValue = Add(partyMember.HitPoints.BonusValue, sign * (sbyte)item.HitPoints);
		partyMember.SpellPoints.BonusValue = Add(partyMember.SpellPoints.BonusValue, sign * (sbyte)item.SpellPoints);

		if (partyMember.Attributes.TryGetValue(item.Attribute, out var attribute))
			attribute.BonusValue = Add(attribute.BonusValue, sign * (sbyte)item.AttributeValue);

		AddSkillBonus(item.Skill, sign * (sbyte)item.SkillValue);
		partyMember.BonusDefense = Add(partyMember.BonusDefense, sign * (sbyte)item.Defense);
		partyMember.BonusDamage = Add(partyMember.BonusDamage, sign * (sbyte)item.Damage);
	}
}
