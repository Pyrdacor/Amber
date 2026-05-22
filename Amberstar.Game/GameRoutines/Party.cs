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

    public void OpenInventory(int characterSlotIndex)
    {
        State.SetCurrentInventory(characterSlotIndex);

        if (ScreenHandler.ActiveScreen is InventoryScreen inventoryScreen)
            inventoryScreen.SwitchToPartyMember(characterSlotIndex, false);
        else if (ScreenHandler.ActiveScreen is CharacterStatsScreen characterStatsScreen)
            characterStatsScreen.SwitchToPartyMember(characterSlotIndex, false);
        else
            ScreenHandler.PushScreen(ScreenType.Inventory);
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

            if (remaining <= 0 && !partyMember.Inventory.Any(slot => slot.Item == null || slot.Count == 0))
                return false;

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

        return true;
    }

    // TODO: Needs testing with bigger party and different constellations
    public int DistributeGold(int amount)
    {
        List<(IPartyMember Taker, int MaxAmount)> takers = [];

        ForeachPartyMember(partyMember =>
        {
            if (partyMember.Race > Race.HalfOrc)
                return;

            int maxGoldToTake = Math.Min((int)((partyMember.MaxWeight() - partyMember.TotalWeight) / GoldWeight), short.MaxValue - partyMember.Gold);

            if (maxGoldToTake > 0)
            {
                takers.Add((partyMember, maxGoldToTake));
            }
        }, Condition.Mad | Condition.Petrified | Condition.Dead | Condition.Ashes | Condition.Dust);

        if (takers.Count == 0)
            return amount;

        if (takers.Count == 1)
        {
            var taker = takers[0];
            int takenGold = Math.Min(amount, taker.MaxAmount);
            taker.Taker.Gold += (ushort)takenGold;
            taker.Taker.TotalWeight += (uint)takenGold * GoldWeight;
            amount -= takenGold;

            return amount;
        }

        while (amount > 0)
        {
            int takerCount = takers.Count;

            for (int i = 0; i < takers.Count; i++)
            {
                var taker = takers[0];
                int goldToTake = amount / takerCount;

                if (goldToTake == 0)
                    goldToTake = amount;

                int takenGold = Math.Min(goldToTake, taker.MaxAmount);
                taker.Taker.Gold += (ushort)takenGold;
                taker.Taker.TotalWeight += (uint)takenGold * GoldWeight;
                taker.MaxAmount -= takenGold;
                amount -= takenGold;

                if (taker.MaxAmount == 0)
                    takerCount--;
            }

            // Remove takers who can't take any more gold
            takers = takers.Where(taker => taker.MaxAmount > 0).ToList();

            if (takers.Count == 0)
                break;
        }

        return amount;
    }
}