using Amber.Assets.Common;
using Amber.Common;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy;

internal class UIGraphicLoader(Amber.Assets.Common.IAssetProvider assetProvider, IGraphic emptyItemSlotGraphic) : IUIGraphicLoader
{
	private readonly Dictionary<StatusIcon, IGraphic> statusIcons = [];
	private readonly Dictionary<ButtonType, IGraphic> buttons = [];
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

	public IGraphic LoadButtonGraphic(ButtonType buttonType)
	{
		if (!buttons.TryGetValue(buttonType, out var gfx))
		{
			if (buttonType > ButtonType.LastOriginalButton)
			{
                gfx = CreateCustomButton(buttonType);

                buttons.Add(buttonType, gfx);

                return gfx;
            }

			var asset = assetProvider.GetAsset(new(AssetType.Button, (int)buttonType));

			if (asset == null)
				throw new AmberException(ExceptionScope.Data, $"Button {buttonType} not found.");

			gfx = Graphic.FromBitPlanes(32, 16, asset.GetReader().ReadBytes(32 * 16 / 2), 4);

			buttons.Add(buttonType, gfx);
		}

		return gfx;
	}

	private Graphic CreateCustomButton(ButtonType buttonType)
	{
		switch (buttonType)
		{
			case ButtonType.DistributeItems:
			{
				var distributeFoodButton = (LoadButtonGraphic(ButtonType.DistributeFood) as Graphic)!;
				var giveItemButton = (LoadButtonGraphic(ButtonType.GiveItem) as Graphic)!;

				// Prepare the base button
				var button = distributeFoodButton.FillRectsWithColor([new(12, 2, 8, 5), new(15, 7, 8, 7)], distributeFoodButton.GetColorIndexAt(2, 1));
				var itemImage = giveItemButton.GetPart(3, 4, 7, 7);
                button.AddOverlay(13, 3, itemImage, false);
                button.AddOverlay(11, 6, itemImage, false);

				return button;
			}
			default:
				throw new InvalidOperationException($"Button type '{buttonType}' is no valid custom button type.");
		}
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
