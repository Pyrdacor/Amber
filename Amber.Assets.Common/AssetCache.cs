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

			if (!assetList.TryGetValue(identifier.Index, out asset))
			{
				asset = assetProvider.GetAsset(identifier);
				assetList.Add(identifier.Index, asset);
			}
			
			return asset;
		}
	}
}
