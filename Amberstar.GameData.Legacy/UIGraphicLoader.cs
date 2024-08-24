using Amber.Assets.Common;
using Amber.Common;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy;

internal class UIGraphicLoader(Amber.Assets.Common.IAssetProvider assetProvider, IGraphic emptyItemSlotGraphic) : IUIGraphicLoader
{
	private readonly Dictionary<StatusIcon, IGraphic> statusIcons = [];
	private readonly Dictionary<Button, IGraphic> buttons = [];
	private readonly Dictionary<UIGraphic, IGraphic> uiGraphics = [];

	public IGraphic LoadStatusIcon(StatusIcon icon)
	{
		if (!statusIcons.TryGetValue(icon, out var gfx))
		{
			var asset = assetProvider.GetAsset(new(AssetType.StatusIcon, (int)icon));

			if (asset == null)
				throw new AmberException(ExceptionScope.Data, $"Status icon {icon} not found.");

			gfx = Graphic.FromBitPlanes(16, 16, asset.GetReader().ReadBytes(16 * 16 / 2), 4);

			statusIcons.Add(icon, gfx);
		}

		return gfx;
	}

	public IGraphic LoadButtonGraphic(Button button)
	{
		if (!buttons.TryGetValue(button, out var gfx))
		{
			var asset = assetProvider.GetAsset(new(AssetType.Button, (int)button));

			if (asset == null)
				throw new AmberException(ExceptionScope.Data, $"Button {button} not found.");

			gfx = Graphic.FromBitPlanes(32, 16, asset.GetReader().ReadBytes(32 * 16 / 2), 4);

			buttons.Add(button, gfx);
		}

		return gfx;
	}

	public IGraphic LoadGraphic(UIGraphic graphic)
	{
		if (graphic == UIGraphic.EmptyItemSlot)
			return emptyItemSlotGraphic;

		if (!uiGraphics.TryGetValue(graphic, out var gfx))
		{
			var asset = assetProvider.GetAsset(new(AssetType.UIGraphic, (int)graphic));

			if (asset == null)
				throw new AmberException(ExceptionScope.Data, $"UI graphic {graphic} not found.");

			var size = graphic.GetSize();
			int frameCount = graphic.GetFrameCount();
			int width = (int)size.Width;
			int height = (int)size.Height;
			gfx = Graphic.FromBitPlanes(width, height, asset.GetReader().ReadBytes(frameCount * width * height / 2), 4, frameCount);

			uiGraphics.Add(graphic, gfx);
		}

		return gfx;
	}
}
