using Amberstar.GameData;
using Amberstar.GameData.Serialization;

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

	/// <summary>
	/// Removes used items (Destroy_used_item in the original). The boni of equipped items are removed.
	/// </summary>
	internal static void DestroyUsedItem(IPartyMember partyMember, ItemSlot itemSlot, bool equipped, int count = 1)
	{
		var item = itemSlot.Item!;

		count = Math.Min(count, itemSlot.Count);

		if (equipped)
			ApplyEquipmentBoni(partyMember, item, false);

		itemSlot.Count -= (byte)count;

		if (itemSlot.Count == 0)
			itemSlot.ClearItem();

		partyMember.TotalWeight -= (uint)(count * item.Weight);
	}

	// conditions of party members which can't take anything (like $e402 in the original)
	const Condition NoTransferConditions = Condition.Dead | Condition.Ashes | Condition.Dust | Condition.Petrified | Condition.Mad;

	/// <summary>
	/// Checks if a party member can take the given items (Scan_4_transfer_victims in the original).
	/// </summary>
	internal static bool CanReceiveItems(IPartyMember partyMember, IItem item, int count)
	{
		if (partyMember.Race > Race.HalfOrc || partyMember.HasAnyConditionOf(NoTransferConditions))
			return false;

		if (partyMember.TotalWeight + (long)count * item.Weight > partyMember.MaxWeight())
			return false;

		return FindBackpackSlot(partyMember, item) != null;
	}

	/// <summary>
	/// Checks if a party member can take the given gold (Scan_4_gold_victims in the original).
	/// </summary>
	internal static bool CanReceiveGold(IPartyMember partyMember, int amount)
	{
		if (partyMember.Race > Race.HalfOrc || partyMember.HasAnyConditionOf(NoTransferConditions))
			return false;

		return partyMember.Gold + amount <= short.MaxValue && partyMember.TotalWeight + amount * GoldWeight <= partyMember.MaxWeight();
	}

	/// <summary>
	/// Checks if a party member can take the given food (TransFood_Return in the original).
	/// Note: In contrast to items and gold, animals can take food.
	/// </summary>
	internal static bool CanReceiveFood(IPartyMember partyMember, int amount)
	{
		if (partyMember.HasAnyConditionOf(NoTransferConditions))
			return false;

		return partyMember.Food + amount <= short.MaxValue && partyMember.TotalWeight + amount * FoodWeight <= partyMember.MaxWeight();
	}

	/// <summary>
	/// Moves items of a backpack slot to another party member (Inv2_Do_TI in the original).
	/// Slots with the same item are filled first. Returns the amount of moved items.
	/// </summary>
	internal static int TransferItems(IPartyMember source, int inventorySlotIndex, IPartyMember target, int count)
	{
		var sourceSlot = source.Inventory[inventorySlotIndex];
		var item = sourceSlot.Item!;
		int moved = 0;

		count = Math.Min(count, sourceSlot.Count);

		while (moved < count)
		{
			var targetSlot = FindBackpackSlot(target, item);

			if (targetSlot == null)
				break;

			int amount = Math.Min(count - moved, item.IsStackable() ? MaxItemsPerSlot - targetSlot.Count : 1);

			if (targetSlot.Count == 0)
				targetSlot.SetItem(item, (byte)amount);
			else
				targetSlot.Count += (byte)amount;

			moved += amount;
		}

		sourceSlot.Count -= (byte)moved;

		if (sourceSlot.Count == 0)
			sourceSlot.ClearItem();

		source.TotalWeight -= (uint)(moved * item.Weight);
		target.TotalWeight += (uint)(moved * item.Weight);

		return moved;
	}

	internal static void TransferGold(IPartyMember source, IPartyMember target, int amount)
	{
		source.Gold -= (word)amount;
		target.Gold += (word)amount;
		source.TotalWeight -= (uint)amount * GoldWeight;
		target.TotalWeight += (uint)amount * GoldWeight;
	}

	internal static void TransferFood(IPartyMember source, IPartyMember target, int amount)
	{
		source.Food -= (word)amount;
		target.Food += (word)amount;
		source.TotalWeight -= (uint)amount * FoodWeight;
		target.TotalWeight += (uint)amount * FoodWeight;
	}

	/// <summary>
	/// Takes the gold of the other party members as far as the
	/// party member can carry it (Pool_gold_INV2 in the original).
	/// </summary>
	internal void GatherGold(IPartyMember receiver)
	{
		ForeachPartyMember(partyMember =>
		{
			if (partyMember == receiver)
				return;

			// Note: The original only checks the weight of each single
			// party member's gold so the receiver could get overloaded.
			long freeWeight = Math.Max(0, (long)receiver.MaxWeight() - receiver.TotalWeight);
			int gold = (int)Math.Min(Math.Min(partyMember.Gold, short.MaxValue - receiver.Gold), freeWeight / GoldWeight);

			if (gold > 0)
				TransferGold(partyMember, receiver, gold);
		}, Condition.Petrified | Condition.Mad);
	}

	/// <summary>
	/// Shows which party members can take something by the hand status icons.
	/// The given party member never takes anything (if any). Returns the amount of possible receivers.
	/// </summary>
	internal int SetHandIcons(IPartyMember? giver, Func<IPartyMember, bool> canReceive)
	{
		int receiverCount = 0;

		ForeachPartyMemberSlot((index, partyMember) =>
		{
			if (partyMember == null)
				HideStatusIcon(index);
			else if (partyMember != giver && canReceive(partyMember))
			{
				SetStatusIcon(index, StatusIcon.HandOpen, true, true);
				receiverCount++;
			}
			else
				SetStatusIcon(index, StatusIcon.HandStop, true, true);
		});

		return receiverCount;
	}
}
