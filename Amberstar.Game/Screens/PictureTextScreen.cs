using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Events;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

internal class PictureTextScreen : Screen
{
	const int TextX = 112;
	const int TextY = 50;
	const int TextWidth = 192;
	const int TextHeight = 140;
    const int ControlDisplayLayer = 100;
    Image? image;
	Label? displayText;
	bool scrolling = false;
	bool closeOnNextInput = false;

	public override ScreenType Type { get; } = ScreenType.PictureText;

	public override void Open(Action? closeAction)
	{
		base.Open(closeAction);

		Game.Cursor.CursorType = CursorType.Zzz;

        var @event = (Game.EventHandler.CurrentEvent as IShowPictureTextEvent)!;
        var imageType = (Image80x80)@event.Picture;
        var palette = Game.PaletteIndexProvider.Get80x80ImagePaletteIndex(imageType);
        
		image = AddImage(16, 81, 80, 80, Game.GraphicIndexProvider.Get80x80ImageIndex(imageType), Layer.UI, ControlDisplayLayer, true);
		image.PaletteIndex = palette;
		image.Visible = true;

        Game.SetLayout(Layout.PictureText, palette);

        int mapIndex = Game.EventHandler.CurrentEventMapIndex;
        var text = Game.AssetProvider.TextLoader.LoadText(new(AssetType.MapText, mapIndex));

		text = text.GetTextBlock(@event.TextIndex);

		displayText = AddLabel(TextX, TextY, text, TextWidth, TextHeight, ControlDisplayLayer);
		displayText.PaletteIndex = palette;
		closeOnNextInput = !displayText.SupportsScrolling;
	}

    public override void Close()
    {
        Game.Cursor.CursorType = CursorType.Sword;

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
			if (!displayText.Text!.ScrollFullHeight())
			{
				closeOnNextInput = true;
			}
			else
			{
				scrolling = true;
				displayText.Text!.ScrollEnded += () => scrolling = false;
			}

			return true;
		}

		return scrolling;
	}
}
