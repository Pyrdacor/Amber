using Amber.Common;
using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game.Screens;

internal class TextBoxScreen : Screen
{
	const int TextX = 112;
	const int TextY = 50;
	const int TextWidth = 192;
	const int TextHeight = 140;
	Game? game;
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
		var @event = (game.EventHandler.CurrentEvent as IShowPictureTextEvent)!;

		if (@event == null)
		{
			InitText(game.CurrentText ?? throw new AmberException(ExceptionScope.Application, "TextBox screen opened without providing a text."));
			return;
		}

		var map = game.CurrentMap ?? throw new AmberException(ExceptionScope.Application, "TextBox screen opened with active event but no active map.");
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
		var layer = game!.GetRenderLayer(Layer.UI);

		displayText = game.TextManager.Create(text, TextWidth, 15, TextManager.TransparentPaper, GetPalette());
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
