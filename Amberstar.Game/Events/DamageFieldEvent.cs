using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Events
{
	internal class DamageFieldEvent(IDamageFieldEvent @event, int eventIndex) : Event(@event, eventIndex), IDamageFieldEvent, ITextEvent
    {
        public byte Damage => @event.Damage;

        public TargetGender TargetGender => @event.TargetGender;

        public byte TextIndex => @event.TextIndex;

        public override bool Handle(EventTrigger trigger, Game game, IMapEventProvider eventProvider)
		{
            if (trigger != EventTrigger.Move)
                return false;

			Gender? allowedGenders = TargetGender switch
			{
				TargetGender.Male => Gender.Male,
				TargetGender.Female => Gender.Female,
				_ => null
			};

            void DamagePartyMember(IPartyMember partyMember, Action finishHandler) => game.DamagePartyMember(partyMember, Game.Random(1, Damage), finishHandler);

            void DamageParty() => game.ForeachPartyMember(DamagePartyMember, null, true, allowedGenders);

            if (this.HasText())
				game.ShowText(DamageParty);
			else
                DamageParty();

			return true;
		}
	}
}
