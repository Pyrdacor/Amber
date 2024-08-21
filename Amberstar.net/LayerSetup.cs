using Amber.Assets.Common;
using Amber.Renderer;
using Ambermoon.Renderer.OpenGL;
using Amberstar.GameData.Legacy;
using Amberstar.GameData.Serialization;

namespace Amberstar.net
{
	internal static class LayerSetup
	{
		public static void Run(AssetProvider assetProvider, Renderer renderer,
			out UIGraphicIndexProvider uiGraphicIndexProvider,
			out PaletteIndexProvider paletteIndexProvider)
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

			// Create the palette
			var uiPalette = assetProvider.PaletteLoader.LoadUIPalette();
			var generalPalettes = Enumerable.Range(1, 10).Select(assetProvider.PaletteLoader.LoadPalette).ToArray();
			var tilesetPalettes = Enumerable.Range(1, 2).Select(index => assetProvider.TilesetLoader.LoadTileset(index).Palette).ToArray();
			var image80x80Palettes = Enumerable.Range(1, 26).Select(index => assetProvider.GraphicLoader.Load80x80Graphic((Image80x80)index).Palette).ToArray();
			var palette = new Graphic(16, 1 + 10 + 2 + 26, GraphicFormat.RGBA);

			palette.AddOverlay(0, 0, uiPalette);
			byte y = 1;
			var generalPaletteIndices = new Dictionary<int, byte>();

			for (int i = 0; i < generalPalettes.Length; i++)
			{
				generalPaletteIndices.Add(i, y);
				palette.AddOverlay(0, y++, generalPalettes[i]);				
			}

			var tilesetPaletteIndices = new Dictionary<int, byte>();

			for (int i = 0; i < tilesetPalettes.Length; i++)
			{
				tilesetPaletteIndices.Add(i + 1, y);
				palette.AddOverlay(0, y++, tilesetPalettes[i]);
			}

			var image80x80PaletteIndices = new Dictionary<Image80x80, byte>();

			for (int i = 0; i < image80x80Palettes.Length; i++)
			{
				image80x80PaletteIndices.Add((Image80x80)(i + 1), y);
				palette.AddOverlay(0, y++, image80x80Palettes[i]);
			}

			var paletteTexture = renderer.TextureFactory.Create(palette);

			// Layouts
			var graphics = new Dictionary<int, IGraphic>();
			graphics.Add(0, assetProvider.LayoutLoader.LoadPortraitArea());
			for (int i = 1; i <= 11; i++)
				graphics.Add(i, assetProvider.LayoutLoader.LoadLayout(i));
			var layer = renderer.LayerFactory.Create(LayerType.Texture2D, new()
			{
				//BaseZ = 0.70f, // TODO
				LayerFeatures = LayerFeatures.DisplayLayers,
				Palette = paletteTexture,
				RenderTarget = LayerRenderTarget.VirtualScreen2D,
				Texture = renderer.TextureFactory.CreateAtlas(graphics),
			});
			layer.Visible = true;
			renderer.AddLayer(layer);

			// UI
			graphics = [];
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
				Palette = paletteTexture,
				RenderTarget = LayerRenderTarget.VirtualScreen2D,
				Texture = renderer.TextureFactory.CreateAtlas(graphics),
			});
			layer.Visible = true;
			renderer.AddLayer(layer);

			// Map underlay
			graphics = [];
			var tileset1Graphics = assetProvider.TilesetLoader.LoadTileset(1).Graphics;
			var tileset2Graphics = assetProvider.TilesetLoader.LoadTileset(2).Graphics;
			int index = 1;
			foreach (var graphic in tileset1Graphics)
				graphics.Add(index++, graphic);
			index++;
			foreach (var graphic in tileset2Graphics)
				graphics.Add(index++, graphic);
			layer = renderer.LayerFactory.Create(LayerType.Texture2D, new()
			{
				//BaseZ = 0.01f, // TODO
				LayerFeatures = LayerFeatures.DisplayLayers,
				Palette = paletteTexture,
				RenderTarget = LayerRenderTarget.VirtualScreen2D,
				Texture = renderer.TextureFactory.CreateAtlas(graphics),
			});
			layer.Visible = true;
			renderer.AddLayer(layer);

			// Map overlay
			layer = renderer.LayerFactory.Create(LayerType.Texture2D, new()
			{
				//BaseZ = 0.02f, // TODO
				LayerFeatures = LayerFeatures.Transparency | LayerFeatures.DisplayLayers,
				Palette = paletteTexture,
				RenderTarget = LayerRenderTarget.VirtualScreen2D,
				Texture = renderer.TextureFactory.CreateAtlas(graphics),
			});
			layer.Visible = true;
			renderer.AddLayer(layer);

			// TODO ...

			uiGraphicIndexProvider = new(buttonOffset, statusIconOffset, uiGraphicOffset, image80x80Offset, itemGraphicOffset);
			paletteIndexProvider = new(0, image80x80PaletteIndices, tilesetPaletteIndices, generalPaletteIndices);
		}
	}
}
