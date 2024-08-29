using Amber.Assets.Common;
using Amber.Renderer;
using Ambermoon.Renderer.OpenGL;
using Amberstar.Game;
using Amberstar.GameData;
using Amberstar.GameData.Legacy;
using Amberstar.GameData.Serialization;

namespace Amberstar.net
{
	internal static class LayerSetup
	{
		public static void Run(AssetProvider assetProvider, Renderer renderer,
			out GraphicIndexProvider uiGraphicIndexProvider,
			out PaletteIndexProvider paletteIndexProvider,
			out PaletteColorProvider paletteColorProvider,
			out FontInfoProvider fontInfoProvider)
		{
			// Create the palette
			var uiPalette = assetProvider.PaletteLoader.LoadUIPalette();
			var generalPalettes = Enumerable.Range(1, 10).Select(assetProvider.PaletteLoader.LoadPalette).ToArray();
			var tilesetPalettes = Enumerable.Range(1, 2).Select(index => assetProvider.TilesetLoader.LoadTileset(index).Palette).ToArray();
			var image80x80Palettes = Enumerable.Range(1, 26).Select(index => assetProvider.GraphicLoader.Load80x80Graphic((Image80x80)index).Palette).ToArray();
			var palette = new Graphic(16, 1 + 10 + 2 + 26, GraphicFormat.RGBA);

			palette.AddOverlay(0, 0, uiPalette);
			byte y = 1;
			var generalPaletteIndices = new Dictionary<int, byte>();
			var palettes = new Dictionary<int, IGraphic>();
			palettes.Add(0, uiPalette);

			for (int i = 0; i < generalPalettes.Length; i++)
			{
				generalPaletteIndices.Add(i, y);
				palettes.Add(y, generalPalettes[i]);
				palette.AddOverlay(0, y++, generalPalettes[i]);
			}

			var tilesetPaletteIndices = new Dictionary<int, byte>();

			for (int i = 0; i < tilesetPalettes.Length; i++)
			{
				tilesetPaletteIndices.Add(i + 1, y);
				palettes.Add(y, tilesetPalettes[i]);
				palette.AddOverlay(0, y++, tilesetPalettes[i]);
			}

			var image80x80PaletteIndices = new Dictionary<Image80x80, byte>();

			for (int i = 0; i < image80x80Palettes.Length; i++)
			{
				image80x80PaletteIndices.Add((Image80x80)(i + 1), y);
				palettes.Add(y, image80x80Palettes[i]);
				palette.AddOverlay(0, y++, image80x80Palettes[i]);
			}

			var paletteTexture = renderer.TextureFactory.Create(palette);


			#region Layouts

			var graphics = new Dictionary<int, IGraphic>
			{
				{ 0, assetProvider.LayoutLoader.LoadPortraitArea() }
			};
			for (int i = 1; i <= 11; i++)
				graphics.Add(i, assetProvider.LayoutLoader.LoadLayout(i));
			var layer = renderer.LayerFactory.Create(LayerType.Texture2D, new()
			{
				BaseZ = 0.0f, // display in the back
				LayerFeatures = LayerFeatures.DisplayLayers,
				Palette = paletteTexture,
				RenderTarget = LayerRenderTarget.VirtualScreen2D,
				Texture = renderer.TextureFactory.CreateAtlas(graphics),
			});
			layer.Visible = true;
			renderer.AddLayer(layer);

			#endregion


			#region UI

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
			int windowGraphicOffset = graphics.Count;
			for (int i = 0; i < 2; i++)
				graphics.Add(windowGraphicOffset + i, assetProvider.GraphicLoader.LoadWindowGraphics(i == 0).ToGraphic());
			int cursorGraphicOffset = graphics.Count;
			foreach (var i in Enum.GetValues<CursorType>().Distinct())
				graphics.Add(cursorGraphicOffset + (int)i, assetProvider.CursorLoader.LoadCursor(i).Graphic);
			layer = renderer.LayerFactory.Create(LayerType.ColorAndTexture2D, new()
			{
				BaseZ = 0.7f,
				LayerFeatures = LayerFeatures.Transparency | LayerFeatures.DisplayLayers,
				Palette = paletteTexture,
				RenderTarget = LayerRenderTarget.VirtualScreen2D,
				Texture = renderer.TextureFactory.CreateAtlas(graphics),
			});
			layer.Visible = true;
			renderer.AddLayer(layer);

			#endregion


			#region Map2D

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
				BaseZ = 0.1f,
				LayerFeatures = LayerFeatures.Transparency,
				Palette = paletteTexture,
				RenderTarget = LayerRenderTarget.VirtualScreen2D,
				Texture = renderer.TextureFactory.CreateAtlas(graphics),
			});
			layer.Visible = true;
			renderer.AddLayer(layer);

			#endregion


			#region Text

			graphics = [];
			var font = assetProvider.FontLoader.LoadFont();
			var textGlyphTextureIndices = new Dictionary<char, int>();
			var runeGlyphTextureIndices = new Dictionary<char, int>();
			int glyphTextureIndex = 0;
			for (int i = 33; i < 256; i++)
			{
				var glyph = font.GetGlyph((char)i, false);

				if (glyph != null)
				{
					textGlyphTextureIndices.Add((char)i, glyphTextureIndex);
					graphics.Add(glyphTextureIndex++, glyph);
				}					
			}
			for (int i = 33; i < 256; i++)
			{
				var glyph = font.GetGlyph((char)i, true);

				if (glyph != null)
				{
					runeGlyphTextureIndices.Add((char)i, glyphTextureIndex++);
					graphics.Add(glyphTextureIndex++, glyph);
				}
			}
			layer = renderer.LayerFactory.Create(LayerType.Texture2D, new()
			{
				BaseZ = 0.7f,
				LayerFeatures = LayerFeatures.Transparency | LayerFeatures.DisplayLayers,
				Palette = paletteTexture,
				RenderTarget = LayerRenderTarget.VirtualScreen2D,
				Texture = renderer.TextureFactory.CreateAtlas(graphics),
			});
			layer.Visible = true;
			renderer.AddLayer(layer);

			#endregion


			#region Map3D

			graphics = [];
			index = 0;
			var labBlocks = assetProvider.LabDataLoader.LoadAllLabBlocks();
			var labBlockImageIndices = new Dictionary<int, Dictionary<PerspectiveLocation, Dictionary<BlockFacing, int>>>();
			foreach (var labBlock in labBlocks)
			{
				var labBlockImages = new Dictionary<PerspectiveLocation, Dictionary<BlockFacing, int>>();
				
				foreach (var perspective in labBlock.Value.Perspectives)
				{
					if (!labBlockImages.TryGetValue(perspective.Location, out var entry))
					{
						entry = [];
						labBlockImages[perspective.Location] = entry;
					}

					entry.Add(perspective.Facing, index);
					graphics.Add(index++, perspective.Frames.ToGraphic());
				}

				labBlockImageIndices.Add(labBlock.Key, labBlockImages);
			}
			var backgroundGraphicIndices = new Dictionary<int, int>();
			foreach (var backgroundGraphic in assetProvider.GraphicLoader.LoadAllBackgroundGraphics())
			{
				backgroundGraphicIndices.Add(backgroundGraphic.Key, index);
				graphics.Add(index++, backgroundGraphic.Value);
			}
			var cloudGraphicIndices = new Dictionary<int, int>();
			foreach (var cloudGraphic in assetProvider.GraphicLoader.LoadAllCloudGraphics())
			{
				cloudGraphicIndices.Add(cloudGraphic.Key, index);
				graphics.Add(index++, cloudGraphic.Value);
			}
			layer = renderer.LayerFactory.Create(LayerType.ColorAndTexture2D, new()
			{
				BaseZ = 0.1f,
				LayerFeatures = LayerFeatures.Transparency | LayerFeatures.DisplayLayers,
				Palette = paletteTexture,
				RenderTarget = LayerRenderTarget.VirtualScreen2D,
				Texture = renderer.TextureFactory.CreateAtlas(graphics),
			});
			layer.Visible = true;
			renderer.AddLayer(layer);

			#endregion


			#region TopMost

			layer = renderer.LayerFactory.Create(LayerType.Color2D, new()
			{
				BaseZ = 0.9f,
				LayerFeatures = LayerFeatures.Transparency | LayerFeatures.DisplayLayers,
				RenderTarget = LayerRenderTarget.VirtualScreen2D,
			});
			layer.Visible = true;
			renderer.AddLayer(layer);

			#endregion

			// TODO ...

			uiGraphicIndexProvider = new(buttonOffset, statusIconOffset, uiGraphicOffset,
				image80x80Offset, itemGraphicOffset, windowGraphicOffset, cursorGraphicOffset,
				backgroundGraphicIndices, cloudGraphicIndices, labBlockImageIndices);
			paletteIndexProvider = new(0, image80x80PaletteIndices, tilesetPaletteIndices, generalPaletteIndices);
			paletteColorProvider = new(palettes);
			fontInfoProvider = new(textGlyphTextureIndices, runeGlyphTextureIndices);
		}
	}
}
