using Amberstar.Game.Screens;
using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Events
{
	internal class RiddleMouthEvent(IRiddleMouthEvent @event, int eventIndex) : Event(@event, eventIndex), IRiddleMouthEvent
	{
		public byte X => @event.X;

		public byte Y => @event.Y;

		public byte RiddleTextIndex => @event.RiddleTextIndex;

		public byte SolvedTextIndex => @event.SolvedTextIndex;

		public word WordIndex => @event.WordIndex;

		public word IconIndex => @event.IconIndex;

		// The event is saved by the riddlemouth screen when the riddle is solved.
		public override bool AutoSave => false;

		public override bool Handle(EventTrigger trigger, Game game, IMapEventProvider eventProvider)
		{
			if (trigger != EventTrigger.Move && trigger != EventTrigger.Eye)
				return false;

			game.ScreenHandler.PushScreen(ScreenType.Riddlemouth);

			return true;
		}
	}
}
