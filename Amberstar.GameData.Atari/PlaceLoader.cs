using Amber.Assets.Common;
using Amber.Common;
using Amberstar.GameData.Events;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Atari;

public class PlaceLoader : IPlaceLoader
{
	readonly Dictionary<AssetIdentifier, IPlaceData> places = [];

	public IPlaceData LoadPlaceData(IAsset asset)
	{
		throw new NotImplementedException(); // TODO
	}
}
