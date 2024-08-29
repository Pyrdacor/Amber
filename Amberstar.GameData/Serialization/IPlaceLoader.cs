using Amberstar.GameData.Events;

namespace Amberstar.GameData.Serialization;

public interface IPlaceLoader
{
	IPlace LoadPlace(PlaceType placeType, int index);
}
