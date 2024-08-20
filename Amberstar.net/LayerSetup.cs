using Amber.Assets.Common;
using Amber.Renderer;
using Ambermoon.Renderer.OpenGL;
using Amberstar.GameData.Legacy;
using Amberstar.GameData.Serialization;

namespace Amberstar.net
{
	internal static class LayerSetup
	{
		public static UIGraphicIndexProvider Run(AssetProvider assetProvider, Renderer renderer)
		{
			/*
			Layout, // opaque, drawn in the back
			MapUnderlay, // all tilsets
			MapOverlay, // all tilsets
			Map3D, // freely 3D map (floor and walls)
			Billboard3D, // freely 3D map (billboards)
			Map3DLegay, // images, block movement
			UI, // UI elements, supports transparency
			Text
			*/

			var uiPalette = assetProvider.PaletteLoader.LoadUIPalette();

			// Layouts
			var graphics = new Dictionary<int, IGraphic>();
			graphics.Add(0, assetProvider.LayoutLoader.LoadPortraitArea());
			for (int i = 1; i <= 11; i++)
				graphics.Add(i, assetProvider.LayoutLoader.LoadLayout(i));
			var layer = renderer.LayerFactory.Create(LayerType.Texture2D, new()
			{
				//BaseZ = 0.70f, // TODO
				LayerFeatures = LayerFeatures.None,
				Palette = renderer.TextureFactory.Create(uiPalette),
				RenderTarget = LayerRenderTarget.VirtualScreen2D,
				Texture = renderer.TextureFactory.CreateAtlas(graphics),
			});
			layer.Visible = true;
			renderer.AddLayer(layer);

			// UI
			graphics = new();
			int uiGraphicOffset = graphics.Count;
			foreach (var g in Enum.GetValues<UIGraphic>().Distinct())
				graphics.Add(uiGraphicOffset + (int)g, assetProvider.UIGraphicLoader.LoadGraphic(g));
			int buttonOffset = graphics.Count;
			foreach (var b in Enum.GetValues<Button>().Distinct())
				graphics.Add(buttonOffset + (int)b, assetProvider.UIGraphicLoader.LoadButtonGraphic(b));
			int statusIconOffset = graphics.Count;
			foreach (var i in Enum.GetValues<StatusIcon>().Distinct())
				graphics.Add(statusIconOffset + (int)i, assetProvider.UIGraphicLoader.LoadStatusIcon(i));
			int image80x80Offset = graphics.Count - 1; // the enum is 1-based
			foreach (var i in Enum.GetValues<Image80x80>().Distinct())
				graphics.Add(image80x80Offset + (int)i, assetProvider.GraphicLoader.Load80x80Graphic(i));
			int itemGraphicOffset = graphics.Count;
			foreach (var i in Enum.GetValues<ItemGraphic>().Distinct())
				graphics.Add(itemGraphicOffset + (int)i, assetProvider.GraphicLoader.LoadItemGraphic(i));
			layer = renderer.LayerFactory.Create(LayerType.ColorAndTexture2D, new()
			{
				//BaseZ = 0.70f, // TODO
				LayerFeatures = LayerFeatures.Transparency | LayerFeatures.DisplayLayers,
				Palette = renderer.TextureFactory.Create(uiPalette),
				RenderTarget = LayerRenderTarget.VirtualScreen2D,
				Texture = renderer.TextureFactory.CreateAtlas(graphics),
			});
			layer.Visible = true;
			renderer.AddLayer(layer);

			// TODO ...

			return new(buttonOffset, statusIconOffset, uiGraphicOffset, image80x80Offset, itemGraphicOffset);
		}
	}
}
