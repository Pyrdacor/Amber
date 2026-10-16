using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Events
{
	internal class EncounterEvent(IEncounterEvent @event, int eventIndex) : Event(@event, eventIndex), IEncounterEvent
	{
		public byte Chance => @event.Chance;

		public byte Quest => @event.Quest;

		public byte QuestTextIndex => @event.QuestTextIndex;

		public byte NoQuestTextIndex => @event.NoQuestTextIndex;

		public word MonsterGroupIndex => @event.MonsterGroupIndex;

		// TODO: The original saves the event when the battle starts. As long as there are
		// no battles, the event is not saved so the encounter is not lost.
		public override bool AutoSave => false;

		public override bool Handle(EventTrigger trigger, Game game, IMapEventProvider eventProvider)
		{
			if (trigger != EventTrigger.Move)
				return false;

			if (!game.Probe(Chance))
				return false;

			if (Quest != 0 && !game.State.IsQuestBitSet(Quest))
				return false;

			byte textIndex = Quest != 0 ? QuestTextIndex : NoQuestTextIndex;
			void StartBattle() => game.StartBattle(MonsterGroupIndex);

			if (textIndex < TextEventExtensions.NoText)
				game.ShowTextMessage(game.GetMapText(game.EventHandler.CurrentEventMapIndex, textIndex), StartBattle);
			else
				StartBattle();

			return true;
		}
	}
}
