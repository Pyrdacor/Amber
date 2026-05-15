using Amberstar.GameData;

namespace Amberstar.Game;

public delegate void ForeachPartyMemberAction(IPartyMember partyMember);
public delegate void AsyncForeachPartyMemberAction(IPartyMember partyMember, Action finishedHandler);

partial class Game
{
    public IEnumerable<IPartyMember> PartyMembers => State.GetPartyMembers(this);

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
        foreach (var partyMember in PartyMembers)
        {
            if (!partyMember.HasAnyConditionOf(disallowedConditions) && (allowedGenders == null || partyMember.Gender == allowedGenders))
                action(partyMember);
        }
    }

    public void ForeachPartyMember(AsyncForeachPartyMemberAction action, Action? finishedHandler, Condition disallowedConditions = Condition.None, Gender? allowedGenders = null)
    {
        var partyMembers = new Queue<IPartyMember>(PartyMembers.Where(p => !p.HasAnyConditionOf(disallowedConditions) && (allowedGenders == null || p.Gender == allowedGenders)));

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
}