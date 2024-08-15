using Amber.Assets.Common;
using Amber.Common;
using Amberstar.GameData.Legacy;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Atari;

internal class LayoutLoader(IAssetProvider assetProvider, Dictionary<int, Graphic> layoutBlocks) : ILayoutLoader
{
	private readonly Dictionary<int, Layout> layouts = [];

	public IGraphic LoadLayout(int index)
	{
		if (!layouts.TryGetValue(index, out var layout))
		{
			var asset = assetProvider.GetAsset(new AssetIdentifier(AssetType.Layout, index));

			if (asset == null)
				throw new AmberException(ExceptionScope.Data, $"Layout {index} not found.");

			layout = new Layout(asset.GetReader().ReadBytes(220), layoutBlocks);

			layouts.Add(index, layout);
		}

		return layout.Graphic;
	}
}
