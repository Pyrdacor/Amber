using Amberstar.GameData.Events;

namespace Amberstar.GameData;

public interface IPlace
{
	IPlaceData Data { get; }

	string Name { get; }
}
