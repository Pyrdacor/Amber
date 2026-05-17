using Amber.Common;
using Amberstar.Game.Events;
using Amberstar.Game.Screens;
using Amberstar.GameData;
using Amberstar.GameData.Events;

namespace Amberstar.Game;

partial class Game
{
	readonly Dictionary<TrapType, (bool ActivePlayerOnly, Condition Condition, Message Message)> TrapTable = new()
	{
		{ TrapType.DamageTrap, (false, Condition.None, Message.TrapExplosion) },
		{ TrapType.PoisonNeedle, (true, Condition.Poisoned, Message.TrapPoisonedArrows) },
		{ TrapType.PoisonGasCloud, (true, Condition.Poisoned, Message.TrapPoisonGas) },
		{ TrapType.BlindingFlash, (false, Condition.Blind, Message.TrapBlindingFlash) },
		{ TrapType.ParalyzingGasCloud, (false, Condition.Stunned, Message.TrapSleepGas) },
        { TrapType.StoneGaze, (false, Condition.Petrified, Message.TrapBasiliskEye) },
        { TrapType.Disease, (false, Condition.Diseased, Message.TrapSpores) },
    };

    internal void SaveEvent(int eventIndex)
	{
		// TODO: Is the map index always correct on world maps?
		State.SaveEvent(State.MapIndex, eventIndex);
	}

	internal bool IsEventActive(IMap map, int eventIndex, IEvent @event)
	{
		// Note: Saved (inactive) chest events just mean "Chest is open now". The event is never removed.
		// Note: Saved (inactive) door_exit events are still executed to trigger the exit event (mostly map exits).
		if (@event is ChestEvent || (@event is DoorEvent doorEvent && doorEvent.OpenedEventIndex != 0))
			return true;

		return State.IsEventActive(map.Index, eventIndex);
	}

	internal bool IsCurrentEventSaved()
	{
        if (EventHandler.CurrentEvent is not Event @event)
            return false;

        // TODO: world map?
        return !State.IsEventActive(State.MapIndex, @event.Index);
    }

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
                    activePartyMember.Damage(damage!.Value, ShowMessageAndFinish);
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
						partyMember.Damage(damage!.Value, next);
					else
						next();
				}, finishAndOptionallyShowMessage, disallowedConditions);
            }
        }
    }
}
