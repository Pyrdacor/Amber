using Amber.Common;

namespace Amber.Assets.Common
{
	public class AssetCache(IAssetProvider assetProvider) : IAssetProvider
	{
		readonly IAssetProvider assetProvider = assetProvider;
		readonly Dictionary<AssetType, Dictionary<int, IAsset?>> cache = [];

		public virtual IAsset? GetAsset(AssetIdentifier identifier)
		{
			IAsset? asset;

			if (!cache.TryGetValue(identifier.Type, out var assetList))
			{
				asset = assetProvider.GetAsset(identifier);
				cache.Add(identifier.Type, new() { { identifier.Index, asset } });
				return asset;
			}

			asset = assetList.GetOrAdd(identifier.Index, () => assetProvider.GetAsset(identifier));
			
			return asset;
		}
	}
}
