using Amber.Common;
using Amberstar.GameData.Serialization;
using System;

namespace Amberstar.GameData.Legacy;

internal class LabDataLoader(AssetProvider assetProvider) : ILabDataLoader
{
	private readonly Dictionary<int, LabData> labDatas = [];
	private readonly Dictionary<int, ILabBlock> labBlocks = [];

	public ILabData LoadLabData(int index)
	{
		if (!labDatas.TryGetValue(index, out var labData))
		{
			var asset = assetProvider.GetAsset(new(AssetType.LabData, index));

			if (asset == null)
				throw new AmberException(ExceptionScope.Data, $"Lab data {index} not found.");

			labData = LabData.Load(asset, LoadAllLabBlocks());

			labDatas.Add(index, labData);
		}

		return labData;
	}

	public Dictionary<int, ILabBlock> LoadAllLabBlocks()
	{
		if (labBlocks.Count != 0)
			return labBlocks;

		var keys = assetProvider.GetAssetKeys(AssetType.LabBlock);

		foreach (var key in keys)
		{
			var asset = assetProvider.GetAsset(new(AssetType.LabBlock, key));

			if (asset == null)
				throw new AmberException(ExceptionScope.Data, $"Lab block {key} not found.");

			var labBlock = LabBlock.Load(asset);

			labBlocks.Add(key, labBlock);
		}

		return labBlocks;
	}
}
