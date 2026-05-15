using Amber.Common;
using Amberstar.Game.Events;
using Amberstar.Game.Screens;
using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game;

partial class Game
{
	internal void Teleport(int x, int y, Direction direction, int mapIndex, bool fade)
	{
		EnableInput(false);
		Pause();

		// Avoid triggering existing delayed actions from old screen
		ClearDelayedActions();

		if (State.GetIndexOfMapWithPlayer() != mapIndex)
			fade = true; // always fade when switching maps

		if (fade)
		{
			Fade(DefaultFadeTime, null, TransitionToMap);
		}
		else
		{
			TransitionToMap();
		}

		void TransitionToMap()
		{
			var map = AssetProvider.MapLoader.LoadMap(mapIndex);

			State.SetPartyPosition(x - 1, y - 1);
			State.PartyDirection = direction;
			State.MapIndex = mapIndex;

			EnableInput(true);
			Resume();

			if (map.Type == MapType.Map2D)
			{
				if (ScreenHandler.ActiveScreen?.Type == ScreenType.Map2D)
					(ScreenHandler.ActiveScreen as Map2DScreen)!.MapChanged();
				else
				{
					ScreenHandler.ClearAllScreens();
					ScreenHandler.PushScreen(ScreenType.Map2D);
				}
			}
			else // 3D
			{
				if (ScreenHandler.ActiveScreen?.Type == ScreenType.Map3D)
					(ScreenHandler.ActiveScreen as Map3DScreen)!.MapChanged();
				else
				{
					ScreenHandler.ClearAllScreens();
					ScreenHandler.PushScreen(ScreenType.Map3D);
				}
			}
		}
	}

	internal void ShowText(Action? nextAction = null)
	{
		CurrentText = null;
		ScreenHandler.PushScreen(ScreenType.TextBox, nextAction);
	}

	internal void ShowPictureWithText()
	{
		ScreenHandler.PushScreen(ScreenType.PictureText);
	}

	internal void OpenPlace(PlaceEvent placeEvent)
	{
		if (placeEvent.PlaceType == PlaceType.None || 
			placeEvent.PlaceType == PlaceType.Unused ||
			placeEvent.PlaceType >= PlaceType.Invalid)
			throw new AmberException(ExceptionScope.Data, "Invalid place type");

        if (!placeEvent.AlwaysOpen && (State.Hour < placeEvent.OpeningHour || State.Hour >= placeEvent.ClosingHour))
		{
            ShowTextMessage(GetCurrentMapText(placeEvent.ClosedTextIndex));
			return;
		}

		ScreenHandler.PushScreen(ScreenType.Place);
	}
}
