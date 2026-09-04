using Amber.Common;

namespace Amber.Assets.Common;

public interface IAssetProvider
{
	/// <summary>
	/// Looks up an asset by its identifier, or null if it doesn't exist.
	/// </summary>
	IAsset? GetAsset(AssetIdentifier identifier);
}
