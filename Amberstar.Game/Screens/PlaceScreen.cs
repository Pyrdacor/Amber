using Amber.Common;
using Amber.Renderer;
using Amberstar.Game.Events;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Events;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

internal class PlaceScreen : Screen
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

	public override ScreenType Type { get; } = ScreenType.Place;

	public override void Open(Game game, Action? closeAction)
	{
		base.Open(game, closeAction);

		this.game = game;

		var placeEvent = (game.EventHandler.CurrentEvent as PlaceEvent)!;

		var layer = game.GetRenderLayer(Layer.UI);
		image = layer.SpriteFactory!.Create();
		var textureAtlas = layer.Config.Texture!;

		Image80x80 imageType;
		Action initPlaceFunction;

        switch (placeEvent.PlaceType)
        {
            case PlaceType.Merchant:
                imageType = Image80x80.Merchant;
				// TODO
				initPlaceFunction = () => { };
                break;
            case PlaceType.FoodDealer:
                imageType = Image80x80.Merchant;
                // TODO
                initPlaceFunction = () => { };
                break;
            case PlaceType.HorseDealer:
                imageType = Image80x80.HorseStable;
                initPlaceFunction = () => OpenTransportDealer(TransportType.Horse, placeEvent);
                break;
            case PlaceType.Healer:
                imageType = Image80x80.Healer;
                // TODO
                initPlaceFunction = () => { };
                break;
            case PlaceType.Sage:
                imageType = Image80x80.Sage;
                // TODO
                initPlaceFunction = () => { };
                break;
            case PlaceType.RaftDealer:
                imageType = Image80x80.ShipDealer;
                initPlaceFunction = () => OpenTransportDealer(TransportType.Raft, placeEvent);
                break;
            case PlaceType.ShipDealer:
                imageType = Image80x80.ShipDealer;
                initPlaceFunction = () => OpenTransportDealer(TransportType.Ship, placeEvent);
                break;
            case PlaceType.Inn:
                imageType = Image80x80.Inn;
                // TODO
                initPlaceFunction = () => { };
                break;
            case PlaceType.Library:
                imageType = Image80x80.Library;
                // TODO
                initPlaceFunction = () => { };
                break;
            case >= PlaceType.FirstGuild and <= PlaceType.LastGuild:
            {
                imageType = Image80x80.Guild;
                Class @class = (Class)(1 + placeEvent.PlaceType - PlaceType.FirstGuild);
                initPlaceFunction = () => OpenGuild(@class, placeEvent);
                break;
            }
            default:
                throw new AmberException(ExceptionScope.Data, "Invalid place type");
        }

		image.Position = new(16, 49);
		image.Size = new(80, 80);
		image.Opaque = true;
		image.TextureOffset = textureAtlas.GetOffset(game.GraphicIndexProvider.Get80x80ImageIndex(imageType));
		var palette = image.PaletteIndex = game.PaletteIndexProvider.Get80x80ImagePaletteIndex(imageType);
		image.Visible = true;

		game.SetLayout(Layout.Place, palette);

		// TODO: text depends on place
		/*var text = game.AssetProvider.TextLoader.LoadText(new(AssetType.MapText, game.State.GetIndexOfMapWithPlayer()));

		text = text.GetTextBlock(placeEvent.);

		displayText = game.TextManager.Create(text, TextWidth, 15, TextManager.TransparentPaper, palette);
		displayText.ShowInArea(TextX, TextY, TextWidth, TextHeight, 100);
		closeOnNextInput = !displayText.SupportsScrolling;*/

		closeOnNextInput = true; // TODO

        initPlaceFunction();
    }

	private void OpenGuild(Class @class, PlaceEvent placeEvent)
	{
		// TODO
	}

	private void OpenTransportDealer(TransportType transportType, PlaceEvent placeEvent)
	{
		// TODO
	}

	public override void Close(Game game)
	{
		image!.Visible = false;
		displayText?.Delete();

		base.Close(game);
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
			game!.ScreenHandler.PopScreen();
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
