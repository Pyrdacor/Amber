using Amber.Common;
using Amberstar.Game.Events;
using Amberstar.Game.Screens;
using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game;

partial class Game
{
	// Same as the trap table of the original (Trap_table in EVENTS.ASM).
	readonly Dictionary<TrapType, (bool ActivePlayerOnly, Condition Condition, Message Message)> TrapTable = new()
	{
		{ TrapType.DamageTrap, (false, Condition.None, Message.TrapExplosion) },
		{ TrapType.PoisonNeedle, (true, Condition.Poisoned, Message.TrapPoisonedArrows) },
		{ TrapType.PoisonGasCloud, (false, Condition.Poisoned, Message.TrapPoisonGas) },
		{ TrapType.BlindingFlash, (false, Condition.Blind, Message.TrapBlindingFlash) },
		{ TrapType.ParalyzingGasCloud, (false, Condition.Stunned, Message.TrapSleepGas) },
        { TrapType.StoneGaze, (true, Condition.Petrified, Message.TrapBasiliskEye) },
        { TrapType.Disease, (true, Condition.Diseased, Message.TrapSpores) },
    };

    internal void SaveEvent(IEvent @event)
	{
		if (@event.SaveIndex == 0)
			return;

		// Note: This assumes the event is on the map of the current event
		// provider or as a fallback on the same map as the player.
		// Important for world maps and map transitions.
		State.SetEventBit(EventHandler.CurrentEventMapIndex, @event.SaveIndex, true);
	}

	internal bool IsEventActive(IMap map, IEvent @event)
	{
		// Note: Saved (inactive) chest events just mean "Chest is open now". The event is never removed.
		// Note: Saved (inactive) door_exit events are still executed to trigger the exit event (mostly map exits).
		if (@event is IChestEvent || @event is IDoorExitEvent)
			return true;

		return !IsEventSaved(map.Index, @event);
	}

	internal bool IsEventSaved(int mapIndex, IEvent @event) => @event.SaveIndex != 0 && State.IsEventBitSet(mapIndex, @event.SaveIndex);

	internal bool IsCurrentEventSaved()
	{
        if (EventHandler.CurrentEvent is not Event @event)
            return false;

        return IsEventSaved(EventHandler.CurrentEventMapIndex, @event);
    }

	public Size? GetMapSize(int mapIndex)
	{
        var map = AssetProvider.MapLoader.LoadMap(mapIndex);

		if (map == null)
			return null;

		return new(map.Width, map.Height);
    }

    public (int Index, string Name)[] GetMaps()
    {
        var maps = AssetProvider.MapLoader.LoadAllMaps();

		return maps.Select(map => (map.Key, map.Value.Name)).ToArray();
    }

	public (Position Position, int MapIndex, Direction Direction) GetCurrentLocation()
	{
		return (State.PartyPosition, State.GetIndexOfMapWithPlayer(), State.PartyDirection);
	}

    public void Teleport(int x, int y, Direction direction, int mapIndex, bool fade)
	{
		EnableInput(false);
		Pause();

		// Avoid triggering existing delayed actions from old screen
		ClearDelayedActions(fadeActionIndex);

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
			bool mapChange = State.GetIndexOfMapWithPlayer() != mapIndex;

			State.SetPartyPosition(x - 1, y - 1);

			if (mapChange)
				State.ResetLastPosition();
			if (direction != Direction.Keep)
				State.PartyDirection = direction;
			State.MapIndex = mapIndex;

			EnableInput(true);
			Resume();
			ScriptMapTransitions++;

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

	internal IMap2D CreateMapInstance(IMap2D map) =>
		new MapInstance2D(map, AssetProvider.TilesetLoader.LoadTileset(map.TilesetIndex), State.SavedTileChanges);

	internal IMap3D CreateMapInstance(IMap3D map) => new MapInstance3D(map, State.SavedTileChanges);

	/// <summary>
	/// Changes a tile of the map of the current event at a 1-based position.
	/// If save is true, the change is stored in the savegame. Otherwise it is
	/// lost when the map is left (like in the original).
	/// </summary>
	internal void ChangeMapTile(int x, int y, int tileIndex, bool save)
	{
		int mapIndex = EventHandler.CurrentEventMapIndex;

		if (save)
			State.SaveTileChange(mapIndex, x, y, tileIndex);

		(ScreenHandler.ActiveScreen as IMapScreen ?? ScreenHandler.FindMapScreen())?.ChangeTile(mapIndex, x, y, tileIndex);
	}

	/// <summary>
	/// Changes the party direction on the shown map.
	/// </summary>
	internal void TurnParty(Direction direction)
	{
		State.PartyDirection = direction;
		(ScreenHandler.ActiveScreen as IMapScreen ?? ScreenHandler.FindMapScreen())?.Turn(direction);
	}

	/// <summary>
	/// Uses an item on the map event at the party position (like in the original inventory).
	/// Returns true if the item had an effect.
	/// </summary>
	internal bool UseItemOnMap(IItem item)
	{
		var mapScreen = ScreenHandler.FindMapScreen();

		if (mapScreen == null)
			return false;

		var position = State.PartyPosition;
		CurrentItem = item;

		try
		{
			return mapScreen.TriggerEvent(EventTrigger.UseItem, position.X, position.Y);
		}
		finally
		{
			CurrentItem = null;
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

		var screenType = placeEvent.PlaceType switch
		{
			PlaceType.Inn => ScreenType.Inn,
			PlaceType.Healer => ScreenType.Healer,
			PlaceType.Sage => ScreenType.Sage,
			PlaceType.Merchant or PlaceType.Library => ScreenType.Merchant,
			>= PlaceType.FirstGuild and <= PlaceType.LastGuild => ScreenType.Guild,
			PlaceType.HorseDealer or PlaceType.RaftDealer or PlaceType.ShipDealer => ScreenType.TransportDealer,
			PlaceType.FoodDealer => ScreenType.FoodDealer,
			_ => throw new AmberException(ExceptionScope.Data, $"Unsupported place type {placeEvent.PlaceType}")
		};

		ScreenHandler.PushScreen(screenType);
	}

	/// <summary>
	/// Inn data while the party camps in an inn (guestroom), otherwise null.
	/// </summary>
	internal IInnData? CampInInn { get; set; }

	/// <summary>
	/// Opens the camp. In an inn the party rests in a guestroom with full regeneration.
	/// </summary>
	internal void OpenCamp(IInnData? innData = null)
	{
		CampInInn = innData;
		ScreenHandler.PushScreen(ScreenType.Camp);
	}

	internal void ShowDoor()
	{
		ScreenHandler.PushScreen(ScreenType.Door);
    }

    internal void ShowChest()
    {
        ScreenHandler.PushScreen(ScreenType.Chest);
    }

	internal void TriggerTrap(TrapType trapType, word? damage, Action? finishHandler = null)
	{
		if (trapType == TrapType.None)
			return;

		if (trapType == TrapType.DamageTrap && (damage == null || damage <= 0))
			return;

		bool ProbeLuck(IPartyMember partyMember)
		{
			int totalLuck = partyMember.Attributes[GameData.Attribute.Luck].TotalCurrent;
			return Probe(totalLuck);
		}

        if (TrapTable.TryGetValue(trapType, out var trapInfo))
		{
            void ShowMessageAndFinish()
            {
                ShowTextMessage(trapInfo.Message, finishHandler);
            }

            if (trapInfo.ActivePlayerOnly)
			{
				var activePartyMember = State.ActivePartyMember!;

				if (ProbeLuck(activePartyMember))
					return;

                activePartyMember.AddCondition(trapInfo.Condition);

                if (trapType == TrapType.DamageTrap)
				{
                    DamagePartyMember(activePartyMember, damage!.Value, ShowMessageAndFinish);
                }
				else
				{
                    ShowMessageAndFinish();
                }
			}
			else
			{
                var disallowedConditions = Condition.Petrified | Condition.Dead | Condition.Ashes | Condition.Dust;

				Action finishAndOptionallyShowMessage = () => finishHandler?.Invoke();

                ForeachPartyMember((partyMember, next) =>
				{
                    if (ProbeLuck(partyMember))
					{
                        next();
						return;
                    }

					// If at least one party member was affected by the trap, show the message at the end of the loop.
					finishAndOptionallyShowMessage = ShowMessageAndFinish;

                    partyMember.AddCondition(trapInfo.Condition);

					if (trapType == TrapType.DamageTrap)
						DamagePartyMember(partyMember, damage!.Value, next);
					else
						next();
				}, finishAndOptionallyShowMessage, disallowedConditions);
            }
        }
    }
}
