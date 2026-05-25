using Amber.Common;
using Amberstar.Game.Events;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

// TODO: Rework with WindowScreen and controls
internal sealed class TextBoxScreen : Screen
{
	const int WindowX = 16;
	const int WindowY = 52;
	const int WindowWidthInTiles = 18;
	const int WindowMinHeightInTiles = 4;
	const int WindowMaxHeightInTiles = 9;
	Window? window;
	IRenderText? displayText;
	bool scrolling = false;
	bool closeOnNextInput = false;

	public sealed override ScreenType Type { get; } = ScreenType.TextBox;

	public sealed override bool Transparent => true;

	public override void Open(Action? closeAction)
	{
		base.Open(closeAction);

        // First check for text event
        if (Game.EventHandler.CurrentEvent is not ITextEvent @event)
        {
            InitText(Game.CurrentText ?? throw new AmberException(ExceptionScope.Application, "TextBox screen opened without providing a text."));
            return;
        }

        int mapIndex = Game.EventHandler.CurrentEventMapIndex;
        var text = Game.AssetProvider.TextLoader.LoadText(new(AssetType.MapText, mapIndex));
		text = text.GetTextBlock(@event.TextIndex);

		InitText(text);
	}

	private byte GetPalette()
	{
		var lastScreen = Game.ScreenHandler.LastScreen;

		if (lastScreen is Map2DScreen map2DScreen)
		{
			var map = map2DScreen.Map;
			return Game.PaletteIndexProvider.GetTilesetPaletteIndex(map.TilesetIndex);
		}
		else if (lastScreen is Map3DScreen map3DScreen)
		{
			var map = map3DScreen.Map;
			var labData = Game.AssetProvider.LabDataLoader.LoadLabData(map.LabDataIndex);
			return Game.PaletteIndexProvider.GetLabyrinthPaletteIndex(labData.PaletteIndex - 1);
		}

		// TODO: others?

		return Game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI];
	}

	private void InitText(IText text)
	{
		var palette = GetPalette();

		displayText = Game.TextManager.Create(text, (WindowWidthInTiles - 2) * Window.TileWidth, 15, TextManager.TransparentPaper, palette);

		// The width is fixed at 18*16 pixels.
		// The height can range from 4*16 pixels to 9*16 pixels.
		// The text area is 256x32 to 256x112 pixels large (at max 16 text lines).
		int numTextLines = displayText.TextLineCount;
		int heightInTiles = MathUtil.Limit
		(
			WindowMinHeightInTiles,
			(numTextLines * displayText.LineHeight + Window.TileHeight - 1) / Window.TileHeight + 2,
			WindowMaxHeightInTiles
		);

		// Create the window
		window?.Destroy();
		window = new(Game, WindowX, WindowY, WindowWidthInTiles, heightInTiles, dark: true, 100, palette);

		// Show the text
		var clientArea = window.ClientArea;
		int textY = clientArea.Top + Math.Max(0, (clientArea.Size.Height - numTextLines * displayText.LineHeight) / 2);
		displayText.ShowInArea(clientArea.Left, textY, clientArea.Size.Width, clientArea.Size.Height, 110);
		closeOnNextInput = !displayText.SupportsScrolling;
	}

	public override void Close()
	{
		displayText?.Delete();
		window?.Destroy();

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
