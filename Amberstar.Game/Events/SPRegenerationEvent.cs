using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Events
{
	internal class SPRegenerationEvent(ISPRegenerationEvent @event, int eventIndex) : Event(@event, eventIndex), ISPRegenerationEvent, ITextEvent
    {
		public byte Amount => @event.Amount;

        public byte TextIndex => @event.TextIndex;

        public bool Fill => @event.Fill;

        public override bool Handle(EventTrigger trigger, Game game, IMapEventProvider eventProvider)
		{
            ForeachPartyMemberAction regenerate = Fill
				? partyMember => partyMember.FillSpellPoints()
				: partyMember => partyMember.HealSpellPoints(Amount);

			void Regenerate() => game.ForeachPartyMember(regenerate, true);

            if (TextIndex != 0)
				game.ShowText(Regenerate);
			else
				Regenerate();

			return true;
		}
	}
}
