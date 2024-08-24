using Amber.Common;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy;

internal class LabDataLoader(Amber.Assets.Common.IAssetProvider assetProvider) : ILabDataLoader
{
	private readonly Dictionary<int, LabData> labDatas = [];

	public ILabData LoadLabData(int index)
	{
		if (!labDatas.TryGetValue(index, out var labData))
		{
			var asset = assetProvider.GetAsset(new(AssetType.LabData, index));

			if (asset == null)
				throw new AmberException(ExceptionScope.Data, $"Lab data {index} not found.");

			labData = LabData.Load(asset, assetProvider);

			labDatas.Add(index, labData);
		}

		return labData;
	}
}
