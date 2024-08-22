using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Events
{
	internal class ShowPictureTextEvent(IShowPictureTextEvent @event) : Event(@event), IShowPictureTextEvent
	{
		public byte Picture => @event.Picture;

		public byte TextIndex => @event.TextIndex;

		public ShowPictureTextTrigger Trigger => @event.Trigger;

		public ushort SetWordBit => @event.SetWordBit;

		public override bool Handle(EventTrigger trigger, Game game, IEventProvider eventProvider)
		{
			int expectedTriggerMask = (int)Trigger + 1;
			int triggerMask = ((int)trigger + 1) & 0x3;

			if ((triggerMask & expectedTriggerMask) == 0)
				return false;

			if (Picture == 0)
				game.ShowText();
			else
				game.ShowPictureWithText();

			return true;
		}
	}
}
