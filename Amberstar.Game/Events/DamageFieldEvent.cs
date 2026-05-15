using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Events
{
	internal class DamageFieldEvent(IDamageFieldEvent @event) : Event(@event), IDamageFieldEvent, ITextEvent
    {
        public byte Damage { get; }

        public TargetGender TargetGender { get; }

        public byte TextIndex => @event.TextIndex;

        public override bool Handle(EventTrigger trigger, Game game, IEventProvider eventProvider)
		{
			Gender? allowedGenders = TargetGender switch
			{
				TargetGender.Male => Gender.Male,
				TargetGender.Female => Gender.Female,
				_ => null
			};

            void DamagePartyMember(IPartyMember partyMember, Action finishHandler) => partyMember.Damage((word)Game.Random(1, Damage), finishHandler);

            void DamageParty() => game.ForeachPartyMember(DamagePartyMember, null, true, allowedGenders);

            if (TextIndex != 0)
				game.ShowText(DamageParty);
			else
                DamageParty();

			return true;
		}
	}
}
