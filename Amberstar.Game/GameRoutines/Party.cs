using Amberstar.GameData;

namespace Amberstar.Game;

public delegate void ForeachPartyMemberAction(IPartyMember partyMember);
public delegate void AsyncForeachPartyMemberAction(IPartyMember partyMember, Action finishedHandler);

partial class Game
{
    public IEnumerable<IPartyMember> PartyMembers => State.GetPartyMembers(this);

    public void ForeachPartyMember(ForeachPartyMemberAction action, bool alive)
    {
        ForeachPartyMember(action, alive ? Condition.Dead | Condition.Ashes | Condition.Dust : Condition.None);
    }

    public void ForeachPartyMember(ForeachPartyMemberAction action, Condition disallowedConditions = Condition.None)
    {
        foreach (var partyMember in PartyMembers)
        {
            if (!partyMember.HasAnyConditionOf(disallowedConditions))
                action(partyMember);
        }
    }

    public void ForeachPartyMember(AsyncForeachPartyMemberAction action, Action finishedHandler, Condition disallowedConditions = Condition.None)
    {
        var partyMembers = new Queue<IPartyMember>(PartyMembers.Where(p => !p.HasAnyConditionOf(disallowedConditions)));

        void ProcessNext()
        {
            if (partyMembers.Count == 0)
            {
                finishedHandler();
                return;
            }

            var partyMember = partyMembers.Dequeue();
            action(partyMember, ProcessNext);
        }

        ProcessNext();
    }
}