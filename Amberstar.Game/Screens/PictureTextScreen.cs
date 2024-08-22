using Amber.Common;
using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Screens
{
	internal class PictureTextScreen : Screen
	{
		const int TextX = 112;
		const int TextY = 50;
		const int TextWidth = 192;
		const int TextHeight = 140;
		Game? game;
		IRenderText? displayText;
		bool scrolling = false;
		bool closeOnNextInput = false;

		public override ScreenType Type { get; } = ScreenType.PictureText;

		public override void Open(Game game, Action? closeAction)
		{
			base.Open(game, closeAction);

			this.game = game;
			game.SetLayout(Layout.PictureText);

			var @event = (game.EventHandler.CurrentEvent as IShowPictureTextEvent)!;
			var text = game.AssetProvider.TextLoader.LoadText(new AssetIdentifier(AssetType.MapText, game.State.MapIndex));

			text = text.GetTextBlock(@event.TextIndex);

			displayText = game.TextManager.Create(text, TextWidth);
			displayText.ShowInArea(TextX, TextY, TextWidth, TextHeight, 100);
			closeOnNextInput = !displayText.SupportsScrolling;
		}

		public override void Close(Game game)
		{
			displayText?.Delete();

			base.Close(game);
		}

		public override void KeyDown(Key key, KeyModifiers keyModifiers)
		{
			ScrollOrClose();
		}

		public override void MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
		{
			ScrollOrClose();
		}

		private void ScrollOrClose()
		{
			if (closeOnNextInput)
			{
				game!.ScreenHandler.PopScreen();
				return;
			}

			if (!scrolling && displayText?.SupportsScrolling == true)
			{
				if (!displayText.ScrollFullHeight())
				{
					closeOnNextInput = true;
				}
				else
				{
					scrolling = true;
					displayText.ScrollEnded += () => scrolling = false;
				}
			}
		}
	}
}
