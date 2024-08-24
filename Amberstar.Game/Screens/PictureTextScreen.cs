using Amber.Common;
using Amber.Renderer;
using Amberstar.GameData;
using Amberstar.GameData.Events;
using Amberstar.GameData.Serialization;
using static System.Net.Mime.MediaTypeNames;

namespace Amberstar.Game.Screens
{
	internal class PictureTextScreen : Screen
	{
		const int TextX = 112;
		const int TextY = 50;
		const int TextWidth = 192;
		const int TextHeight = 140;
		Game? game;
		ISprite? image;
		IRenderText? displayText;
		bool scrolling = false;
		bool closeOnNextInput = false;

		public override ScreenType Type { get; } = ScreenType.PictureText;

		public override void Open(Game game, Action? closeAction)
		{
			base.Open(game, closeAction);

			this.game = game;

			var @event = (game.EventHandler.CurrentEvent as IShowPictureTextEvent)!;

			var layer = game.Renderer.Layers[(int)Layer.UI];
			image = layer.SpriteFactory!.Create();
			var textureAtlas = layer.Config.Texture!;

			Image80x80 imageType = (Image80x80)@event.Picture;
			image.Position = new(16, 81);
			image.Size = new(80, 80);
			image.Opaque = true;
			image.TextureOffset = textureAtlas.GetOffset(game.UIGraphicIndexProvider.Get80x80ImageIndex(imageType));
			var palette = image.PaletteIndex = game.PaletteIndexProvider.Get80x80ImagePaletteIndex(imageType);
			image.Visible = true;

			game.SetLayout(Layout.PictureText, palette);

			var text = game.AssetProvider.TextLoader.LoadText(new(AssetType.MapText, game.State.MapIndex));

			text = text.GetTextBlock(@event.TextIndex);

			displayText = game.TextManager.Create(text, TextWidth, 15, TextManager.TransparentPaper, palette);
			displayText.ShowInArea(TextX, TextY, TextWidth, TextHeight, 100);
			closeOnNextInput = !displayText.SupportsScrolling;
		}

		public override void Close(Game game)
		{
			image!.Visible = false;
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
