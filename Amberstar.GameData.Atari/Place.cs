using Amberstar.GameData.Events;

namespace Amberstar.GameData.Atari;

internal class Place(IPlaceData data, string name) : IPlace
{
	public IPlaceData Data { get; } = data;

	public string Name { get; } = name;
}
