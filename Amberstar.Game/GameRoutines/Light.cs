using Amberstar.Game.Screens;
using Amberstar.GameData;

namespace Amberstar.Game;

/// <summary>
/// Light and darkness on maps (M2_time_evaluate and M3_Dark_check in the original).
/// </summary>
partial class Game
{
	/// <summary>
	/// Above this radius the light circle of 2D maps is not shown at all.
	/// </summary>
	internal const int MaxLightRadius = 200;

	/// <summary>
	/// In complete darkness only the party is visible on 2D maps.
	/// </summary>
	internal const int DarknessLightRadius = 8;

	// radius of the light circle by hour on maps with changing light
	static readonly int[] LightRadiusByHour =
	[
		16, 16, 16, 16, 16, 16, 40, 64,
		200, 200, 200, 200, 200, 200, 200, 200,
		200, 64, 64, 40, 16, 16, 16, 16
	];

	/// <summary>
	/// The map of the map screen (the combined world map on world maps).
	/// </summary>
	internal IMap? ShownMap => ScreenHandler.FindMapScreen() switch
	{
		Map2DScreen map2DScreen => map2DScreen.Map,
		Map3DScreen map3DScreen => map3DScreen.Map,
		_ => null
	};

	ActiveSpell LightSpell => State.ActiveSpells[(int)ActiveSpellType.Light];

	/// <summary>
	/// Checks if the party can see anything (otherwise it is stygian darkness).
	/// The active party member must not be blind. Dark maps need a light spell.
	/// </summary>
	internal bool CanSee()
	{
		if (State.ActivePartyMember?.HasAnyConditionOf(Condition.Blind) == true)
			return false;

		var map = ShownMap;

		if (map == null)
			return true;

		// Note: 2D maps check the light bits first.
		if (map.Type == MapType.Map2D && (map.Flags.HasFlag(MapFlags.Light) || map.Flags.HasFlag(MapFlags.LightChange)))
			return true;

		if (!map.Flags.HasFlag(MapFlags.Darkness) || State.TravelType == TravelType.SuperChicken)
			return true;

		return LightSpell.Duration != 0;
	}

	/// <summary>
	/// Gets the radius of the light circle around the party on 2D maps in pixels.
	/// Null means that the whole map is visible.
	/// </summary>
	internal int? GetLightRadius(IMap2D map)
	{
		if (!CanSee())
			return DarknessLightRadius;

		// The super chicken has x-ray specs.
		if (State.TravelType == TravelType.SuperChicken || map.Flags.HasFlag(MapFlags.Light))
			return null;

		int lightSpellRadius = LightSpell.Duration == 0 ? 0 : LightSpell.Value * 8;

		if (map.Flags.HasFlag(MapFlags.LightChange))
			return LightRadiusByHour[State.Hour % 24] + lightSpellRadius;

		if (map.Flags.HasFlag(MapFlags.Darkness))
			return 16 + lightSpellRadius;

		return null;
	}
}
