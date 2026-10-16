using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Events
{
	internal class AntiMagicEvent(IAntiMagicEvent @event, int eventIndex) : Event(@event, eventIndex), IAntiMagicEvent, ITextEvent
	{
		public ActiveSpellRemoval ActiveSpell => @event.ActiveSpell;

		public byte TextIndex => @event.TextIndex;

		public override bool Handle(EventTrigger trigger, Game game, IMapEventProvider eventProvider)
		{
			if (trigger != EventTrigger.Move)
				return false;

			void RemoveSpells()
			{
				var activeSpells = game.State.ActiveSpells;

				if (ActiveSpell == ActiveSpellRemoval.All)
					Array.Clear(activeSpells);
				else if ((int)ActiveSpell <= activeSpells.Length)
					activeSpells[(int)ActiveSpell - 1] = default;
			}

			if (this.HasText())
				game.ShowText(RemoveSpells);
			else
				RemoveSpells();

			return true;
		}
	}
}
