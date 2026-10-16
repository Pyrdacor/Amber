using Amber.Common;
using Amberstar.Game.Events;
using Amberstar.Game.Screens;
using Amberstar.GameData;

namespace Amberstar.Game;

file sealed record ScriptItemSlot(byte SpellCharges, ItemSlotFlags SlotFlags) : IItemSlot;

/// <summary>
/// Functionality for scenario scripts and automated tests.
///
/// Map positions and party member slots are 1-based like in the cheats
/// and the original data docs. Members return null or false if the
/// requested thing does not exist instead of throwing.
/// </summary>
partial class Game
{
	readonly List<string> scriptShownTexts = [];

	/// <summary>
	/// All texts which were shown in text boxes, picture texts or
	/// confirmation windows since the last call of <see cref="ScriptClearShownTexts"/>.
	/// </summary>
	public IReadOnlyList<string> ScriptShownTexts => scriptShownTexts.AsReadOnly();

	public void ScriptClearShownTexts() => scriptShownTexts.Clear();

	internal void ScriptTextShown(IText text)
	{
		// a large width avoids line breaks inside of the paragraphs
		var paragraphs = text.GetParagraphs(1000).Select(lines => string.Join(" ", lines));
		scriptShownTexts.Add(string.Join("\n", paragraphs));
	}

	/// <summary>
	/// Name of the active screen type (e.g. Map2D or TextBox).
	/// </summary>
	public string? ScriptActiveScreen => ScreenHandler.ActiveScreen?.Type.ToString();

	public bool ScriptInputEnabled => InputEnabled;

	public bool ScriptPaused => Paused;

	public bool ScriptFading => fadeArea != null;

	/// <summary>
	/// True if a map is shown, no fading is active and the player could move.
	/// </summary>
	public bool ScriptIdle => ScreenHandler.ActiveScreen is IScriptMapScreen && InputEnabled && !Paused && !ScriptFading;

	public int ScriptMapIndex => State.GetIndexOfMapWithPlayer();

	/// <summary>
	/// 1-based party position.
	/// </summary>
	public Position ScriptPartyPosition => State.PartyPosition + new Position(1, 1);

	public Direction ScriptPartyDirection => State.PartyDirection;

	public string ScriptState()
	{
		int mapIndex = ScriptMapIndex;
		var mapName = AssetProvider.MapLoader.LoadMap(mapIndex)?.Name ?? "?";
		var position = ScriptPartyPosition;

		return $"map {mapIndex} ({mapName}) at {position.X},{position.Y} facing {State.PartyDirection}, screen {ScriptActiveScreen ?? "none"}, " +
			$"input {(InputEnabled ? "on" : "off")}, paused {(Paused ? "yes" : "no")}, fading {(ScriptFading ? "yes" : "no")}, " +
			$"time {State.Hour:00}:{State.Minute:00} day {State.Day} month {State.Month} year {State.Year}";
	}

	/// <summary>
	/// Moves the party by one tile (offsets -1, 0 or 1) without the movement delay.
	/// Returns null on success or the reason why it failed.
	/// </summary>
	public string? ScriptMove(int x, int y)
	{
		if (ScreenHandler.ActiveScreen is not IScriptMapScreen mapScreen)
			return $"no map is shown but {ScriptActiveScreen}";

		if (!InputEnabled || Paused)
			return "input is disabled";

		return mapScreen.ScriptMove(x, y) ? null : "movement was blocked";
	}

	public string? ScriptTurn(Direction direction)
	{
		if (ScreenHandler.ActiveScreen is not IScriptMapScreen mapScreen)
			return $"no map is shown but {ScriptActiveScreen}";

		mapScreen.ScriptTurn(direction);

		return null;
	}

	/// <summary>
	/// Triggers the map events at a 1-based map position.
	/// Valid triggers are Eye, Move, Ear, Mouth and UseItem.
	/// </summary>
	public string? ScriptTrigger(string trigger, int x, int y)
	{
		if (!Enum.TryParse<EventTrigger>(trigger, true, out var eventTrigger))
			return $"invalid trigger '{trigger}'";

		if (ScreenHandler.ActiveScreen is not IScriptMapScreen mapScreen)
			return $"no map is shown but {ScriptActiveScreen}";

		return mapScreen.ScriptTrigger(eventTrigger, x - 1, y - 1) ? null : $"nothing to trigger at {x},{y}";
	}

	/// <summary>
	/// Teleports the party to a 1-based map position without fading.
	/// </summary>
	public string? ScriptTeleport(int mapIndex, int x, int y, Direction direction = Direction.Keep)
	{
		var mapSize = GetMapSize(mapIndex);

		if (mapSize == null)
			return $"map {mapIndex} does not exist";

		if (x < 1 || y < 1 || x > mapSize.Value.Width || y > mapSize.Value.Height)
			return $"position {x},{y} is outside of map {mapIndex} ({mapSize.Value.Width}x{mapSize.Value.Height})";

		Teleport(x, y, direction, mapIndex, false);

		return null;
	}

	/// <summary>
	/// Answers the shown confirmation window.
	/// </summary>
	public string? ScriptAnswer(bool yes)
	{
		if (ScreenHandler.ActiveScreen is not ConfirmationScreen confirmationScreen)
			return $"no confirmation is shown but {ScriptActiveScreen}";

		confirmationScreen.ScriptAnswer(yes);

		return null;
	}

	public bool ScriptIsEventActive(int mapIndex, int eventIndex) => State.IsEventActive(mapIndex, eventIndex);

	public void ScriptSetEventActive(int mapIndex, int eventIndex, bool active) => State.SetEventActive(mapIndex, eventIndex, active);

	public bool ScriptIsQuestBitSet(int bit) => State.IsQuestBitSet(bit);

	public void ScriptSetQuestBit(int bit, bool set) => State.SetQuestBit(bit, set);

	public bool ScriptIsMapCharacterActive(int mapIndex, int characterIndex) => State.IsMapCharacterActive(mapIndex, characterIndex);

	public bool ScriptIsWordKnown(int wordIndex) => State.IsWordKnown(wordIndex);

	public void ScriptSetTime(int hour, int minute)
	{
		State.Hour = MathUtil.Limit(0, hour, 23);
		State.Minute = MathUtil.Limit(0, minute, 59);
	}

	public int ScriptHour => State.Hour;

	public int ScriptPartySize => State.PartySize;

	public int ScriptActivePartyMember => State.ActivePartyMemberIndex;

	public bool ScriptSetActivePartyMember(int slot)
	{
		if (!State.HasPartyMemberInSlot(slot))
			return false;

		State.SetActivePartyMember(slot);
		UpdatePartyMembers();

		return true;
	}

	public string? ScriptPartyMemberName(int slot) => State.GetPartyMember(slot)?.Name;

	/// <summary>
	/// Gets a value of a party member. Valid names are gold, food, hp, maxhp, sp, maxsp, level,
	/// exp, slp, weight as well as all attribute and skill names (current total value).
	/// </summary>
	public int? ScriptGetPartyMemberValue(int slot, string name)
	{
		var partyMember = State.GetPartyMember(slot);

		if (partyMember == null)
			return null;

		switch (name.ToLowerInvariant())
		{
			case "gold":
				return partyMember.Gold;
			case "food":
				return partyMember.Food;
			case "hp":
				return partyMember.HitPoints.TotalCurrent;
			case "maxhp":
				return partyMember.HitPoints.TotalMax;
			case "sp":
				return partyMember.SpellPoints.TotalCurrent;
			case "maxsp":
				return partyMember.SpellPoints.TotalMax;
			case "level":
				return partyMember.Level;
			case "exp":
				return (int)partyMember.ExperiencePoints;
			case "slp":
				return partyMember.SpellLearningPoints;
			case "weight":
				return (int)partyMember.TotalWeight;
		}

		if (Enum.TryParse<GameData.Attribute>(name, true, out var attribute))
			return partyMember.Attributes[attribute].TotalCurrent;

		if (Enum.TryParse<Skill>(name, true, out var skill))
			return partyMember.Skills[skill].TotalCurrent;

		return null;
	}

	/// <summary>
	/// Sets a value of a party member. See <see cref="ScriptGetPartyMemberValue"/> for valid names.
	/// For hp, sp, attributes and skills the current value is set.
	/// </summary>
	public string? ScriptSetPartyMemberValue(int slot, string name, int value)
	{
		var partyMember = State.GetPartyMember(slot);

		if (partyMember == null)
			return $"no party member in slot {slot}";

		word wordValue = (word)MathUtil.Limit(0, value, word.MaxValue);

		switch (name.ToLowerInvariant())
		{
			case "gold":
				partyMember.Gold = wordValue;
				break;
			case "food":
				partyMember.Food = wordValue;
				break;
			case "hp":
				partyMember.HitPoints.CurrentValue = wordValue;
				break;
			case "maxhp":
				partyMember.HitPoints.MaxValue = wordValue;
				break;
			case "sp":
				partyMember.SpellPoints.CurrentValue = wordValue;
				break;
			case "maxsp":
				partyMember.SpellPoints.MaxValue = wordValue;
				break;
			case "level":
				partyMember.Level = (byte)Math.Min(wordValue, byte.MaxValue);
				break;
			case "exp":
				partyMember.ExperiencePoints = (uint)Math.Max(0, value);
				break;
			case "slp":
				partyMember.SpellLearningPoints = wordValue;
				break;
			default:
				if (Enum.TryParse<GameData.Attribute>(name, true, out var attribute))
					partyMember.Attributes[attribute].CurrentValue = wordValue;
				else if (Enum.TryParse<Skill>(name, true, out var skill))
					partyMember.Skills[skill].CurrentValue = wordValue;
				else
					return $"unknown value '{name}'";
				break;
		}

		UpdatePartyMember(slot - 1);

		return null;
	}

	public Condition? ScriptGetConditions(int slot) => State.GetPartyMember(slot)?.GetConditions();

	public string? ScriptSetCondition(int slot, string condition, bool set)
	{
		var partyMember = State.GetPartyMember(slot);

		if (partyMember == null)
			return $"no party member in slot {slot}";

		if (!Enum.TryParse<Condition>(condition, true, out var parsedCondition))
			return $"unknown condition '{condition}'";

		if (set)
			partyMember.AddCondition(parsedCondition);
		else
			partyMember.RemoveCondition(parsedCondition);

		ResetStatusIcons();
		UpdatePartyMember(slot - 1);

		return null;
	}

	/// <summary>
	/// Counts an item in the inventories and equipment of all party members.
	/// </summary>
	public int ScriptCountItem(uint itemIndex)
	{
		int count = 0;

		foreach (var partyMember in State.PartyMembers)
		{
			count += partyMember.Inventory.Where(slot => slot.Item?.Index == itemIndex).Sum(slot => slot.Count);
			count += partyMember.Equipment.Values.Where(slot => slot.Item?.Index == itemIndex).Sum(slot => Math.Max(1, (int)slot.Count));
		}

		return count;
	}

	/// <summary>
	/// Adds an item to the inventory of a party member.
	/// </summary>
	public string? ScriptGiveItem(int slot, uint itemIndex, int count)
	{
		if (!State.HasPartyMemberInSlot(slot))
			return $"no party member in slot {slot}";

		IStaticItemData itemData;

		try
		{
			itemData = AssetProvider.ItemLoader.LoadItem(itemIndex);
		}
		catch (Exception ex)
		{
			return $"item {itemIndex} could not be loaded: {ex.Message}";
		}

		var item = AssetProvider.ItemFactory.Create(itemData, new ScriptItemSlot(0, 0));

		if (!TryAddItem(slot, item, count))
			return $"the inventory of slot {slot} is full";

		var partyMember = State.GetPartyMember(slot)!;
		partyMember.TotalWeight += (uint)count * item.Weight;

		return null;
	}
}
