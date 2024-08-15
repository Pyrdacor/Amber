namespace Amberstar.GameData.Serialization
{
	public interface IPlaceLoader
	{
		Dictionary<int, IPlace> LoadPlaces();
	}
}
