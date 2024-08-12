namespace Amberstar.GameData.Events;

public enum PlaceType : byte
{
	// TODO (guilds are based on classes)
	Guild1, Guild2, Guild3, Guild4,
	Guild5, Guild6, Guild7, Guild8,
	Merchant,
	FoodDealer,
	Unused, // 0 pointer in orignal?
	HorseDealer,
	Healer,
	Sage,
	RaftDealer,
	ShipDealer,
	Inn,
	Library,
	Invalid // first invalid type
}

public interface IPlaceData
{
	word Price { get; }
}

public interface IFoodDealerData : IPlaceData
{

}

// Place pictures are based on the PlaceType. Use this table:
// 3,3,3,3,3,3,3,3,4,4,6,7,8,10,14,14,15,23

/// <summary>
/// Opens a place like a merchant, inn, etc.
/// </summary>
public interface IPlaceEvent : IEvent
{
	/// <summary>
	/// Byte 1
	/// 
	/// Note: If this is 0, the place is always open.
	/// </summary>
	byte OpeningHour { get; }

	/// <summary>
	/// Byte 2
	/// </summary>
	byte ClosingHour { get; }

	/// <summary>
	/// Byte 3
	/// </summary>
	PlaceType PlaceType { get; }

	/// <summary>
	/// Byte 4
	/// 
	/// Text is used if the place is closed.
	/// </summary>
	byte ClosedTextIndex { get; }

	/// <summary>
	/// Word 6
	/// </summary>
	word PlaceIndex { get; }

	/// <summary>
	/// Word 8
	/// 
	/// Only used for merchants and libraries.
	/// Index into WARESDAT.AMB.
	/// </summary>
	word WaresIndex { get; }

	bool AlwaysOpen { get; }
}
