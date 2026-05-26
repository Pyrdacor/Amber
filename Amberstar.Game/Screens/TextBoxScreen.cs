using Amber.Common;
using Amberstar.Game.Events;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

internal sealed class TextBoxScreen() : WindowScreen(WindowX, WindowY, WindowWidthInTiles, WindowMinHeightInTiles, WindowDisplayLayer)
{
	const int WindowX = 16;
	const int WindowY = 52;
	const int WindowWidthInTiles = 18;
	const int WindowMinHeightInTiles = 4;
	const int WindowMaxHeightInTiles = 9;
    const byte WindowDisplayLayer = 200;
    const byte TextDisplayLayer = 210;
	Label? displayText;
	bool scrolling = false;
	bool closeOnNextInput = false;
    int heightInTiles = WindowMinHeightInTiles;
    byte paletteIndex = 0;

    public sealed override ScreenType Type { get; } = ScreenType.TextBox;

    public override bool Dark { get; } = true;

    public override bool CreateWindowInOpenHandler { get; } = true;

    public override int HeightInTiles => heightInTiles;

    public override byte? PaletteIndex => paletteIndex;

    public override void Open(Action? closeAction)
	{
        IText text;

        // First check for text event
        if (Game.EventHandler.CurrentEvent is not ITextEvent @event)
        {
            text = Game.CurrentText ?? throw new AmberException(ExceptionScope.Application, "TextBox screen opened without providing a text.");
        }
        else
        {
            int mapIndex = Game.EventHandler.CurrentEventMapIndex;
            text = Game.AssetProvider.TextLoader.LoadText(new(AssetType.MapText, mapIndex));
            text = text.GetTextBlock(@event.TextIndex);
        }

        paletteIndex = GetPalette();
        displayText = AddLabel(0, 0, text, width: (WindowWidthInTiles - 2) * Window.TileWidth, height: WindowMaxHeightInTiles * Window.TileHeight + 7, TextDisplayLayer);
        displayText.PaletteIndex = paletteIndex;

        // The width is fixed at 18*16 pixels.
        // The height can range from 4*16 pixels to 9*16 pixels.
        // The text area is 256x32 to 256x112 pixels large (at max 16 text lines).
        int numTextLines = displayText.TextLineCount;
        heightInTiles = MathUtil.Limit
        (
            WindowMinHeightInTiles,
            (numTextLines * displayText.LineHeight + Window.TileHeight - 1) / Window.TileHeight + 2,
            WindowMaxHeightInTiles
        );

        base.Open(closeAction);

        // Show the text
        var (x, y, width, height) = ClientArea;
        int textY = y + Math.Max(0, (height - numTextLines * displayText.LineHeight) / 2);
        displayText.Area = new(x, textY, width, height);
        closeOnNextInput = !displayText.SupportsScrolling;
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
