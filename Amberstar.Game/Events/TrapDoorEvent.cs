using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Events
{
	internal class TrapDoorEvent(ITrapDoorEvent @event, int eventIndex) : Event(@event, eventIndex), ITrapDoorEvent, ITextEvent
	{
		public byte X => @event.X;

		public byte Y => @event.Y;

		public bool Floor => @event.Floor;

		public byte TextIndex => @event.TextIndex;

		public word MapIndex => @event.MapIndex;

		public word MaxFallDamage => @event.MaxFallDamage;

		// The original only saves the event if the party falls down.
		public override bool AutoSave => Floor;

		public override bool Handle(EventTrigger trigger, Game game, IMapEventProvider eventProvider)
		{
			if (trigger != EventTrigger.Move)
				return false;

			void Fall()
			{
				// A trap door in the ceiling does nothing in the original.
				if (!Floor)
					return;

				void DamagePartyMember(IPartyMember partyMember, Action finishHandler) =>
					game.DamagePartyMember(partyMember, Game.Random(1, Math.Max(1, (int)MaxFallDamage)), finishHandler);

				void FallDown()
				{
					game.ResetStatusIcons();
					game.UpdatePartyMembers();
					// the view direction is kept
					game.Teleport(X, Y, Direction.Keep, MapIndex, true);
				}

				game.ForeachPartyMember(DamagePartyMember, FallDown, Condition.Petrified | Condition.Dead | Condition.Ashes | Condition.Dust);
			}

			if (this.HasText())
				game.ShowText(Fall);
			else
				Fall();

			return true;
		}
	}
}
