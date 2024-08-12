using Amber.Common;

namespace Amber.Assets.Common;

public interface IAssetProvider
{
	IAsset? GetAsset(AssetIdentifier identifier);
}
