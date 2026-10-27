using Amberstar.Game.Screens;
using Amberstar.GameData;

namespace Amberstar.Game;

public delegate void ForeachPartyMemberAction(IPartyMember partyMember);
public delegate void ForeachPartyMemberWithIndexAction(int slotIndex, IPartyMember partyMember);
public delegate void ForeachPartyMemberSlotAction(int slotIndex, IPartyMember? partyMember);
public delegate void AsyncForeachPartyMemberAction(IPartyMember partyMember, Action finishedHandler);

partial class Game
{
    public void ForeachPartyMember(ForeachPartyMemberAction action, bool onlyAlive, Gender? allowedGenders = null)
    {
        ForeachPartyMember(action, onlyAlive ? Condition.Dead | Condition.Ashes | Condition.Dust : Condition.None, allowedGenders);
    }

    public void ForeachPartyMember(AsyncForeachPartyMemberAction action, Action? finishedHandler, bool onlyAlive, Gender? allowedGenders = null)
    {
        ForeachPartyMember(action, finishedHandler, onlyAlive ? Condition.Dead | Condition.Ashes | Condition.Dust : Condition.None, allowedGenders);
    }

    public void ForeachPartyMember(ForeachPartyMemberAction action, Condition disallowedConditions = Condition.None, Gender? allowedGenders = null)
    {
        foreach (var partyMember in State.PartyMembers)
        {
            if (!partyMember.HasAnyConditionOf(disallowedConditions) && (allowedGenders == null || partyMember.Gender == allowedGenders))
                action(partyMember);
        }
    }

    public void ForeachPartyMember(AsyncForeachPartyMemberAction action, Action? finishedHandler, Condition disallowedConditions = Condition.None, Gender? allowedGenders = null)
    {
        var partyMembers = new Queue<IPartyMember>(State.PartyMembers.Where(p => !p.HasAnyConditionOf(disallowedConditions) && (allowedGenders == null || p.Gender == allowedGenders)));

        void ProcessNext()
        {
            if (partyMembers.Count == 0)
            {
                finishedHandler?.Invoke();
                return;
            }

            var partyMember = partyMembers.Dequeue();
            action(partyMember, ProcessNext);
        }

        ProcessNext();
    }

    public void ForeachPartyMember(ForeachPartyMemberWithIndexAction action, Condition disallowedConditions = Condition.None)
    {
        foreach (var p in State.PartyMembersWithSlot.Where(p => p.PartyMember?.HasAnyConditionOf(disallowedConditions) == false))
            action(p.SlotIndex, p.PartyMember!);
    }

    public void ForeachPartyMember(ForeachPartyMemberWithIndexAction action, bool onlyAlive)
    {
        ForeachPartyMember(action, onlyAlive ? Condition.Dead | Condition.Ashes | Condition.Dust : Condition.None);
    }

    public void ForeachPartyMemberSlot(ForeachPartyMemberSlotAction action, Condition disallowedConditions = Condition.None)
    {
        foreach (var p in State.PartyMembersWithSlot.Where(p => p.PartyMember?.HasAnyConditionOf(disallowedConditions) != true))
            action(p.SlotIndex, p.PartyMember!);
    }

    public void ForeachPartyMemberSlot(ForeachPartyMemberSlotAction action, bool onlyAlive)
    {
        ForeachPartyMemberSlot(action, onlyAlive ? Condition.Dead | Condition.Ashes | Condition.Dust : Condition.None);
    }

    public IEnumerable<int> GetValidPartyMemberSlots() =>
        State.PartyMembersWithSlot.Where(p => p.PartyMember != null).Select(p => p.SlotIndex);

    /// <summary>
    /// Equipment of the current inventory which is destroyed when the inventory is opened (healers).
    /// </summary>
    internal List<EquipmentSlot>? EquipmentToDestroy { get; set; }

    public void OpenInventory(int characterSlotIndex)
    {
        State.SetCurrentInventory(characterSlotIndex);

        if (ScreenHandler.ActiveScreen is InventoryScreen inventoryScreen)
            inventoryScreen.SwitchToPartyMember(characterSlotIndex, true);
        else if (ScreenHandler.ActiveScreen is CharacterStatsScreen characterStatsScreen)
            characterStatsScreen.SwitchToPartyMember(characterSlotIndex, true);
        else
            ScreenHandler.PushScreen(ScreenType.Inventory);
    }

    private IPartyMember EnsurePartyMember(int slotIndex)
    {
        return State.GetPartyMember(slotIndex)
            ?? throw new IndexOutOfRangeException("No party member in slot {slotIndex}.");   
    }

    public void RemoveInventoryItem(int partyMemberSlotIndex, int itemSlotIndex, int count = 1)
    {
        var partyMember = EnsurePartyMember(partyMemberSlotIndex);

        RemoveInventoryItem(partyMember, itemSlotIndex, count);
    }

    public void RemoveEquipment(int partyMemberSlotIndex, EquipmentSlot equipmentSlot, int count = 1)
    {
        var partyMember = EnsurePartyMember(partyMemberSlotIndex);

        RemoveEquipment(partyMember, equipmentSlot, count);
    }

    public void RemoveInventoryItem(IPartyMember partyMember, int itemSlotIndex, int count = 1)
    {
        var itemSlot = partyMember.Inventory[itemSlotIndex];

        ItemRemoved(partyMember, itemSlot, count, false);
    }

    public void RemoveEquipment(IPartyMember partyMember, EquipmentSlot equipmentSlot, int count = 1)
    {
        var itemSlot = partyMember.Equipment[equipmentSlot];

        ItemRemoved(partyMember, itemSlot, count, true);
    }

    private void ItemRemoved(IPartyMember partyMember, ItemSlot itemSlot, int count, bool wasEquipped)
    {
        if (count == 0)
            return;

        if (count < 0)
            throw new InvalidOperationException("Can't remove a negative amount of items.");

        if (itemSlot.Count < count)
            throw new InvalidOperationException($"Tried to remove {count} items but only {itemSlot.Count} were there.");

        var item = itemSlot.Item ?? throw new InvalidOperationException($"Unexpected null item in item slot.");

        if (itemSlot.Count == count)
            itemSlot.ClearItem();
        else 
            itemSlot.Count -= (byte)count;

        partyMember.TotalWeight -= (uint)count * item.Weight;

        // Removed equipment doesn't give its boni (or mali) anymore.
        if (wasEquipped)
        {
            for (int i = 0; i < count; i++)
                ApplyEquipmentBoni(partyMember, item, false);
        }
    }

    /// <summary>
    /// Adds a spell (1-based number inside the school) to the learned spells.
    /// </summary>
    internal static void LearnSpell(IPartyMember partyMember, SpellSchool school, int spellNumber)
    {
        dword bit = 1u << spellNumber;

        switch (school)
        {
            case SpellSchool.White:
                partyMember.LearnedWhiteSpells |= bit;
                break;
            case SpellSchool.Gray:
                partyMember.LearnedGraySpells |= bit;
                break;
            case SpellSchool.Black:
                partyMember.LearnedBlackSpells |= bit;
                break;
            case SpellSchool.Special:
                partyMember.LearnedSpecialSpells |= bit;
                break;
        }
    }

    internal static bool HasLearnedSpell(IPartyMember partyMember, SpellSchool school, int spellNumber)
    {
        dword spells = school switch
        {
            SpellSchool.White => partyMember.LearnedWhiteSpells,
            SpellSchool.Gray => partyMember.LearnedGraySpells,
            SpellSchool.Black => partyMember.LearnedBlackSpells,
            SpellSchool.Special => partyMember.LearnedSpecialSpells,
            _ => 0
        };

        return (spells & (1u << spellNumber)) != 0;
    }

    /// <summary>
    /// Marks an item as identified (e.g. by a sage) which shows more details.
    /// </summary>
    internal void IdentifyItem(ItemSlot itemSlot)
    {
        var item = itemSlot.Item;

        if (item == null)
            return;

        var identifiedItem = AssetProvider.ItemFactory.Create(item, new ItemSlotData(item.SpellCharges, item.SlotFlags | ItemSlotFlags.Identified));
        itemSlot.SetItem(identifiedItem, itemSlot.Count);
    }

    /// <summary>
    /// Counts an item in the inventories and equipment of all party members.
    /// </summary>
    internal int CountPartyItems(uint itemIndex)
    {
        int count = 0;

        foreach (var partyMember in State.PartyMembers)
        {
            count += partyMember.Inventory.Where(slot => slot.Item?.Index == itemIndex).Sum(slot => slot.Count);
            count += partyMember.Equipment.Values.Where(slot => slot.Item?.Index == itemIndex).Sum(slot => Math.Max(1, (int)slot.Count));
        }

        return count;
    }

    public bool TryAddItem(int partyMemberSlotIndex, IItem item, int count = 1)
    {
        var partyMember = State.GetPartyMember(partyMemberSlotIndex);

        if (partyMember == null)
            return false;

        if (item.Flags.HasFlag(ItemFlags.Stackable))
        {
            var itemSlotsWithSameItem = partyMember.Inventory.Where(slot => slot.Item?.Index == item.Index);
            int remaining = count;

            foreach (var itemSlotWithSameItem in itemSlotsWithSameItem)
            {
                int spaceInSlot = Math.Max(0, 99 - itemSlotWithSameItem.Count);
                remaining -= spaceInSlot;

                if (remaining <= 0)
                    break;
            }

            if (remaining > 0 && !partyMember.Inventory.Any(slot => slot.Item == null || slot.Count == 0))
                return false;

            partyMember.TotalWeight += (uint)count * item.Weight;

            foreach (var itemSlotWithSameItem in itemSlotsWithSameItem)
            {
                int spaceInSlot = Math.Max(0, 99 - itemSlotWithSameItem.Count);
                int addCount = Math.Min(count, spaceInSlot);
                count -= addCount;

                itemSlotWithSameItem.Count += (byte)addCount;

                if (count == 0)
                    return true;
            }
        }

        var emptySlot = partyMember.Inventory.FirstOrDefault(slot => slot.Item == null || slot.Count == 0);

        if (emptySlot == null)
            return false;

        emptySlot.SetItem(item, (byte)count);

        if (!item.Flags.HasFlag(ItemFlags.Stackable))
            partyMember.TotalWeight += (uint)count * item.Weight;

        return true;
    }

    public bool TryAddGold(int partyMemberSlotIndex, int gold)
    {
        if (gold < 0)
            return false;

        var partyMember = State.GetPartyMember(partyMemberSlotIndex);

        if (partyMember == null)
            return false;

        var weight = gold * GoldWeight;

        if (partyMember.Gold + gold <= short.MaxValue && partyMember.TotalWeight + weight <= partyMember.MaxWeight())
        {
            partyMember.Gold += (ushort)gold;
            partyMember.TotalWeight += (uint)weight;
            return true;
        }

        return false;
    }

    public bool TryAddFood(int partyMemberSlotIndex, int food)
    {
        if (food < 0)
            return false;

        var partyMember = State.GetPartyMember(partyMemberSlotIndex);

        if (partyMember == null)
            return false;

        var weight = food * FoodWeight;

        if (partyMember.Food + food <= short.MaxValue && partyMember.TotalWeight + weight <= partyMember.MaxWeight())
        {
            partyMember.Food += (word)food;
            partyMember.TotalWeight += (uint)weight;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Distributes food rations to the party members like Divide_food in the original.
    /// Returns the remaining food which nobody could carry.
    /// </summary>
    public int DistributeFood(int amount) => Distribute(amount, FoodWeight,
        partyMember => partyMember.Food, (partyMember, food) => partyMember.Food += (word)food);

    /// <summary>
    /// Distributes gold coins to the party members like Divide_gold in the original.
    /// Returns the remaining gold which nobody could carry.
    /// </summary>
    public int DistributeGold(int amount) => Distribute(amount, GoldWeight,
        partyMember => partyMember.Gold, (partyMember, gold) => partyMember.Gold += (word)gold);

    /// <summary>
    /// Takes the gold of all party members (like Pool_gold in the original).
    /// The given amount is already pooled and the total is limited to 32767.
    /// Returns the new total.
    /// </summary>
    internal int PoolGold(int pooledGold)
    {
        ForeachPartyMember(partyMember =>
        {
            int gold = Math.Min(partyMember.Gold, short.MaxValue - pooledGold);

            if (gold <= 0)
                return;

            partyMember.Gold -= (word)gold;
            partyMember.TotalWeight -= (uint)gold * GoldWeight;
            pooledGold += gold;
        }, Condition.Mad | Condition.Petrified);

        return pooledGold;
    }

    // Everyone who can carry something gets the same share. This is repeated
    // until everything is given away or nobody can carry any more.
    private int Distribute(int amount, uint weightPerUnit, Func<IPartyMember, int> getCurrent, Action<IPartyMember, int> add)
    {
        if (amount <= 0)
            return amount;

        var takers = new List<IPartyMember>();
        var capacities = new List<int>();

        ForeachPartyMember(partyMember =>
        {
            // Animals and monsters don't take anything.
            if (partyMember.Race > Race.HalfOrc)
                return;

            long freeWeight = (long)partyMember.MaxWeight() - partyMember.TotalWeight;
            int capacity = (int)Math.Min(Math.Max(0, freeWeight) / weightPerUnit, short.MaxValue - getCurrent(partyMember));

            if (capacity > 0)
            {
                takers.Add(partyMember);
                capacities.Add(capacity);
            }
        }, Condition.Mad | Condition.Petrified | Condition.Dead | Condition.Ashes | Condition.Dust);

        int interested = takers.Count;

        while (amount > 0 && interested > 0)
        {
            for (int i = 0; i < takers.Count && amount > 0; i++)
            {
                if (capacities[i] == 0)
                    continue;

                int share = amount / interested;

                if (share == 0)
                    share = amount;

                int taken = Math.Min(share, capacities[i]);
                add(takers[i], taken);
                takers[i].TotalWeight += (uint)taken * weightPerUnit;
                capacities[i] -= taken;
                amount -= taken;

                if (capacities[i] == 0)
                    interested--;
            }
        }

        return amount;
    }
}