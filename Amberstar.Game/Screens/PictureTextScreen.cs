using Amber.Common;
using Amber.Renderer.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Events;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

// TODO: Rework with WindowScreen and controls
internal class PictureTextScreen : Screen
{
	const int TextX = 112;
	const int TextY = 50;
	const int TextWidth = 192;
	const int TextHeight = 140;
	ISprite? image;
	IRenderText? displayText;
	bool scrolling = false;
	bool closeOnNextInput = false;

	public override ScreenType Type { get; } = ScreenType.PictureText;

	public override void Open(Action? closeAction)
	{
		base.Open(closeAction);

		Game.Cursor.CursorType = CursorType.Sword;

		var @event = (Game.EventHandler.CurrentEvent as IShowPictureTextEvent)!;

		var layer = Game.GetRenderLayer(Layer.UI);
		image = layer.SpriteFactory!.Create();
		var textureAtlas = layer.Config.Texture!;

		Image80x80 imageType = (Image80x80)@event.Picture;
		image.Position = new(16, 81);
		image.Size = new(80, 80);
		image.Opaque = true;
		image.TextureOffset = textureAtlas.GetOffset(Game.GraphicIndexProvider.Get80x80ImageIndex(imageType));
		var palette = image.PaletteIndex = Game.PaletteIndexProvider.Get80x80ImagePaletteIndex(imageType);
		image.Visible = true;

        Game.SetLayout(Layout.PictureText, palette);

        int mapIndex = Game.EventHandler.CurrentEventMapIndex;
        var text = Game.AssetProvider.TextLoader.LoadText(new(AssetType.MapText, mapIndex));

		text = text.GetTextBlock(@event.TextIndex);

		displayText = Game.TextManager.Create(text, TextWidth, 15, TextManager.TransparentPaper, palette);
		displayText.ShowInArea(TextX, TextY, TextWidth, TextHeight, 100);
		closeOnNextInput = !displayText.SupportsScrolling;
	}

	public override void Close()
	{
		image!.Visible = false;
		displayText?.Delete();

		base.Close();
	}

	public override bool KeyDown(Key key, KeyModifiers keyModifiers)
	{
        return ScrollOrClose() || base.KeyDown(key, keyModifiers);
    }

	public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
        return ScrollOrClose() || base.MouseDown(position, buttons, keyModifiers);
    }

	private bool ScrollOrClose()
	{
		if (closeOnNextInput)
		{
            Game.ScreenHandler.PopScreen();
			return true;
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

			return true;
		}

		return scrolling;
	}
}
