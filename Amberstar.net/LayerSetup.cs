using Amber.Assets.Common;
using Amber.Renderer;
using Ambermoon.Renderer.OpenGL;
using Amberstar.GameData.Legacy;
using Amberstar.GameData.Serialization;

namespace Amberstar.net
{
	internal static class LayerSetup
	{
		public static void Run(AssetProvider assetProvider, Renderer renderer)
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

			// TODO: palettes (should be one palette texture for all layers which need them)
			byte[] testPal =
			[
				0x00, 0x00,
				0x07, 0x50,
				0x03, 0x33,
				0x02, 0x22,
				0x01, 0x11,
				0x07, 0x42,
				0x06, 0x31,
				0x02, 0x00,
				0x05, 0x66,
				0x03, 0x45,
				0x07, 0x54,
				0x06, 0x43,
				0x05, 0x32,
				0x04, 0x21,
				0x03, 0x10,
				0x07, 0x65
			];

			var palette = Graphic.FromPalette(testPal);

			// Layouts
			var graphics = new Dictionary<int, IGraphic>();
			graphics.Add(0, assetProvider.LayoutLoader.LoadPortraitArea());
			for (int i = 1; i <= 11; i++)
				graphics.Add(i, assetProvider.LayoutLoader.LoadLayout(i));
			var layer = renderer.LayerFactory.Create(LayerType.Texture2D, new()
			{
				//BaseZ = 0.70f, // TODO
				LayerFeatures = LayerFeatures.None,
				Palette = renderer.TextureFactory.Create(palette),
				RenderTarget = LayerRenderTarget.VirtualScreen2D,
				Texture = renderer.TextureFactory.CreateAtlas(graphics),
			});
			layer.Visible = true;
			renderer.AddLayer(layer);

			// UI
			graphics = new();
			foreach (var g in Enum.GetValues<UIGraphic>().Distinct())
				graphics.Add((int)g, assetProvider.UIGraphicLoader.LoadGraphic(g));
			int offset = graphics.Count;
			foreach (var b in Enum.GetValues<Button>().Distinct())
				graphics.Add(offset + (int)b, assetProvider.UIGraphicLoader.LoadButtonGraphic(b));
			offset = graphics.Count;
			foreach (var i in Enum.GetValues<StatusIcon>().Distinct())
				graphics.Add(offset + (int)i, assetProvider.UIGraphicLoader.LoadStatusIcon(i));
			layer = renderer.LayerFactory.Create(LayerType.ColorAndTexture2D, new()
			{
				//BaseZ = 0.70f, // TODO
				LayerFeatures = LayerFeatures.Transparency | LayerFeatures.DisplayLayers,
				Palette = renderer.TextureFactory.Create(palette),
				RenderTarget = LayerRenderTarget.VirtualScreen2D,
				Texture = renderer.TextureFactory.CreateAtlas(graphics),
			});
			layer.Visible = true;
			renderer.AddLayer(layer);

			// TODO ...
		}
	}
}
