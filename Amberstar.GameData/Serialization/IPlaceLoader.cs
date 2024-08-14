using Amber.Assets.Common;
using Amberstar.GameData.Events;

namespace Amberstar.GameData.Serialization
{
	public interface IPlaceLoader
	{
		IPlaceData LoadPlaceData(IAsset asset);
	}
}
