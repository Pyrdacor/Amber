using Amber.Assets.Common;
using Amber.Common;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy;

internal class LayoutLoader(Amber.Assets.Common.IAssetProvider assetProvider, Dictionary<int, Graphic> layoutBlocks,
	List<word> layoutBottomCorners, List<word> layoutBottomCornerMasks, IGraphic portraitArea) : ILayoutLoader
{
	private readonly Dictionary<int, Layout> layouts = [];

	public IGraphic LoadLayout(int index)
	{
		if (!layouts.TryGetValue(index, out var layout))
		{
			var asset = assetProvider.GetAsset(new(AssetType.Layout, index));

			if (asset == null)
				throw new AmberException(ExceptionScope.Data, $"Layout {index} not found.");

			layout = new Layout(asset.GetReader().ReadBytes(220), layoutBlocks, layoutBottomCorners, layoutBottomCornerMasks);

			layouts.Add(index, layout);
		}

		return layout.Graphic;
	}

	public IGraphic LoadPortraitArea() => portraitArea;
}
