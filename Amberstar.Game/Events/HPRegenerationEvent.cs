using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Events
{
	internal class HPRegenerationEvent(IHPRegenerationEvent @event, int eventIndex) : Event(@event, eventIndex), IHPRegenerationEvent, ITextEvent
    {
		public byte Amount => @event.Amount;

        public byte TextIndex => @event.TextIndex;

        public bool Fill => @event.Fill;

        public override bool Handle(EventTrigger trigger, Game game, IMapEventProvider eventProvider)
		{
            ForeachPartyMemberAction regenerate = Fill
				? partyMember => partyMember.FillHitPoints()
				: partyMember => partyMember.HealHitPoints(Amount);

			void Regenerate() => game.ForeachPartyMember(regenerate, true);

            if (TextIndex != 0)
				game.ShowText(Regenerate);
			else
				Regenerate();

			return true;
		}
	}
}
