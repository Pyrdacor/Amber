using Amberstar.Game.Screens;
using Amberstar.GameData;

namespace Amberstar.Game;

public delegate void ForeachPartyMemberAction(IPartyMember partyMember);
public delegate void ForeachPartyMemberWithIndexAction(int slotIndex, IPartyMember partyMember);
public delegate void ForeachPartyMemberSlotAction(int slotIndex, IPartyMember? partyMember);
public delegate void AsyncForeachPartyMemberAction(IPartyMember partyMember, Action finishedHandler);

partial class Game
{
    public void ForeachPartyMember(ForeachPartyMemberAction action, bool alive, Gender? allowedGenders = null)
    {
        ForeachPartyMember(action, alive ? Condition.Dead | Condition.Ashes | Condition.Dust : Condition.None, allowedGenders);
    }

    public void ForeachPartyMember(AsyncForeachPartyMemberAction action, Action? finishedHandler, bool alive, Gender? allowedGenders = null)
    {
        ForeachPartyMember(action, finishedHandler, alive ? Condition.Dead | Condition.Ashes | Condition.Dust : Condition.None, allowedGenders);
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

    public void ForeachPartyMember(ForeachPartyMemberWithIndexAction action, bool alive)
    {
        ForeachPartyMember(action, alive ? Condition.Dead | Condition.Ashes | Condition.Dust : Condition.None);
    }

    public void ForeachPartyMemberSlot(ForeachPartyMemberSlotAction action, Condition disallowedConditions = Condition.None)
    {
        foreach (var p in State.PartyMembersWithSlot.Where(p => p.PartyMember?.HasAnyConditionOf(disallowedConditions) != true))
            action(p.SlotIndex, p.PartyMember!);
    }

    public void ForeachPartyMemberSlot(ForeachPartyMemberSlotAction action, bool alive)
    {
        ForeachPartyMemberSlot(action, alive ? Condition.Dead | Condition.Ashes | Condition.Dust : Condition.None);
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
}