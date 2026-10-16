using Amberstar.GameData.Events;

namespace Amberstar.GameData.Serialization;

public interface IPlaceLoader
{
	IPlace LoadPlace(PlaceType placeType, int index);
	/// <summary>
	/// Loads the 12 wares of a merchant (index from the place event).
	/// Empty slots are null. The available amounts are stored in the savegame.
	/// </summary>
	IItem?[] LoadMerchantWares(int waresIndex);
}
