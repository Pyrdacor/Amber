using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Screens;

internal class TextBoxScreen : Screen
{
	const int WindowX = 16;
	const int WindowY = 52;
	const int WindowWidthInTiles = 18;
	const int WindowMinHeightInTiles = 4;
	const int WindowMaxHeightInTiles = 9;
	Game? game;
	Window? window;
	IRenderText? displayText;
	bool scrolling = false;
	bool closeOnNextInput = false;

	public override ScreenType Type { get; } = ScreenType.TextBox;

	public override bool Transparent => true;

	public override void Open(Game game, Action? closeAction)
	{
		base.Open(game, closeAction);

		this.game = game;

		// First check for text event
		var @event = game.EventHandler.CurrentEvent as IShowPictureTextEvent;

		if (@event == null)
		{
			InitText(game.CurrentText ?? throw new AmberException(ExceptionScope.Application, "TextBox screen opened without providing a text."));
			return;
		}

		var text = game.AssetProvider.TextLoader.LoadText(new(AssetType.MapText, game.State.MapIndex));
		text = text.GetTextBlock(@event.TextIndex);

		InitText(text);
	}

	private byte GetPalette()
	{
		var lastScreen = game!.ScreenHandler.LastScreen;

		if (lastScreen is Map2DScreen map2DScreen)
		{
			var map = map2DScreen.Map;
			return game.PaletteIndexProvider.GetTilesetPaletteIndex(map.TilesetIndex);
		}
		else if (lastScreen is Map3DScreen map3DScreen)
		{
			var map = map3DScreen.Map;
			var labData = game!.AssetProvider.LabDataLoader.LoadLabData(map.LabDataIndex);
			return game.PaletteIndexProvider.GetLabyrinthPaletteIndex(labData.PaletteIndex - 1);
		}

		// TODO: others?

		return game.PaletteIndexProvider.UIPaletteIndex;
	}

	private void InitText(IText text)
	{
		var palette = GetPalette();

		displayText = game!.TextManager.Create(text, (WindowWidthInTiles - 2) * Window.TileWidth, 15, TextManager.TransparentPaper, palette);

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
		window = new(game, WindowX, WindowY, WindowWidthInTiles, heightInTiles, dark: true, 100, palette);

		// Show the text
		var clientArea = window.ClientArea;
		int textY = clientArea.Top + Math.Max(0, (clientArea.Size.Height - numTextLines * displayText.LineHeight) / 2);
		displayText.ShowInArea(clientArea.Left, textY, clientArea.Size.Width, clientArea.Size.Height, 110);
		closeOnNextInput = !displayText.SupportsScrolling;
	}

	public override void Close(Game game)
	{
		displayText?.Delete();
		window?.Destroy();

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
