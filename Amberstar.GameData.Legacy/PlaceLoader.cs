using Amber.Assets.Common;
using Amber.Common;
using Amberstar.GameData.Events;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy;

internal class PlaceLoader(Amber.Assets.Common.IAssetProvider assetProvider) : IPlaceLoader
{
	private readonly Dictionary<int, Place> places = [];

	public IPlace LoadPlace(PlaceType placeType, int index)
	{
		if (places.TryGetValue(index, out var place))
			return place;

		var asset = assetProvider.GetAsset(new(AssetType.Place, index));

		if (asset == null)
			throw new AmberException(ExceptionScope.Data, $"Place data {index} not found.");

		var placeData = PlaceData.ReadPlaceData(placeType, asset.GetReader());

		asset = assetProvider.GetAsset(new(AssetType.PlaceName, index));

		if (asset == null)
			throw new AmberException(ExceptionScope.Data, $"Place name {index} not found.");

		string name = asset.GetReader().ReadString();

		place = new Place(placeData, name);

		places.Add(index, place);

		return place;
	}
}
