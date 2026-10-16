using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Events
{
	internal class AttributeChangeEvent(IAttributeChangeEvent @event, int eventIndex) : Event(@event, eventIndex), IAttributeChangeEvent, ITextEvent
	{
		public byte Attribute => @event.Attribute;

		public bool Add => @event.Add;

		public bool Random => @event.Random;

		public byte TextIndex => @event.TextIndex;

		public bool AffectAllPlayers => @event.AffectAllPlayers;

		public word Amount => @event.Amount;

		public override bool Handle(EventTrigger trigger, Game game, IMapEventProvider eventProvider)
		{
			if (trigger != EventTrigger.Move)
				return false;

			var attribute = (GameData.Attribute)Attribute;

			void ChangeAttribute(IPartyMember partyMember)
			{
				if (!partyMember.Attributes.TryGetValue(attribute, out var value))
					return;

				int amount = Random ? Game.Random(1, Math.Max(1, (int)Amount)) : Amount;

				if (!Add)
					amount = -amount;

				// The original changes the current value without any limit. We avoid negative values.
				value.CurrentValue = (word)Math.Clamp(value.CurrentValue + amount, 0, word.MaxValue);
			}

			void ChangeAttributes()
			{
				if (AffectAllPlayers)
					game.ForeachPartyMember(ChangeAttribute, Condition.Dead | Condition.Ashes | Condition.Dust);
				else if (game.State.ActivePartyMember is IPartyMember activePartyMember)
					ChangeAttribute(activePartyMember);
			}

			if (this.HasText())
				game.ShowText(ChangeAttributes);
			else
				ChangeAttributes();

			return true;
		}
	}
}
