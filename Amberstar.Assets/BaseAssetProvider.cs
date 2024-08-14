using Amber.Assets.Common;
using Amber.Common;
using Amber.IO.Common.FileSystem;
using Amber.Serialization;

namespace Amberstar.Assets;

public abstract class BaseAssetProvider : IAssetProvider
{
	readonly IReadOnlyFileSystem fileSystem;
	readonly Dictionary<AssetType, Dictionary<int, Asset>> assets = [];
	readonly Dictionary<AssetType, IFileContainer> assetContainers = [];

	public BaseAssetProvider(IReadOnlyFileSystem fileSystem)
	{
		this.fileSystem = fileSystem;
	}

	public virtual IAsset? GetAsset(AssetIdentifier identifier)
	{
		bool hasAssetList = assets.TryGetValue(identifier.Type, out var assetList);

		if (hasAssetList && assetList!.TryGetValue(identifier.Index, out var asset))
			return asset;

		if (!assetContainers.TryGetValue(identifier.Type, out var container))
		{
			var fileName = FileNameByAssetType(identifier.Type);
			var file = fileSystem.GetFile(fileName);

			if (file == null)
				return null;

			container = new FileReader().ReadFile(fileName, file.Stream.GetReader());
			assetContainers.Add(identifier.Type, container);
		}

		if (!hasAssetList)
		{
			assetList = [];
			assets.Add(identifier.Type, assetList);
		}

		if (!container.Files.TryGetValue(identifier.Index, out var reader))
			return null;

		asset = new Asset(identifier, reader);
		assetList!.Add(identifier.Index, asset);

		return asset;
	}

	protected virtual string FileNameByAssetType(AssetType assetType)
	{
		return assetType switch
		{
			AssetType.Map => "MAP_DATA.AMB",
			_ => throw new KeyNotFoundException($"Invalid asset type {assetType} for Amberstar"),
		};
	}
}
