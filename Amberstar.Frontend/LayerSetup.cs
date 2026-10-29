using Amber.Assets.Common;
using Amber.Renderer.Common;
using Amberstar.GameData;
using Amberstar.GameData.Legacy;
using Amberstar.GameData.Serialization;

namespace Amberstar.Frontend
{
	internal static class LayerSetup
	{
		public static void Run(AssetProvider assetProvider, IRenderer renderer,
			out GraphicIndexProvider graphicIndexProvider,
			out PaletteIndexProvider paletteIndexProvider,
			out PaletteColorProvider paletteColorProvider,
			out FontInfoProvider fontInfoProvider)
		{
			// Create the palette
			var builtinPalettes = Enum.GetValues<BuiltinPalette>().Select(assetProvider.PaletteLoader.LoadBuiltinPalette).ToArray();
            var generalPalettes = Enumerable.Range(1, 10).Select(assetProvider.PaletteLoader.LoadPalette).ToArray();
			var tilesetPalettes = Enumerable.Range(1, 2).Select(index => assetProvider.TilesetLoader.LoadTileset(index).Palette).ToArray();
			var image80x80Palettes = Enumerable.Range(1, 26).Select(index => assetProvider.GraphicLoader.Load80x80Graphic((Image80x80)index).Palette).ToArray();
			var combatPalettes = Enumerable.Range(1, ICombatData.CombatBackgroundCount).Select(assetProvider.CombatData.GetPalette).ToArray();
			// the loading screen palette with all fading steps
			var loadingScreenPalettes = Enumerable.Range(0, IGraphicLoader.LoadingScreenFadeSteps + 1)
				.Select(assetProvider.GraphicLoader.LoadLoadingScreenPalette).OfType<IGraphic>().ToArray();
			var palette = new Graphic(16, builtinPalettes.Length + 10 + 2 + 26 + combatPalettes.Length +
				loadingScreenPalettes.Length, GraphicFormat.RGBA);
            var generalPaletteIndices = new Dictionary<int, byte>();
			var palettes = new Dictionary<int, IGraphic>();
            byte y = 0;

            foreach (var builtinPalette in builtinPalettes)
            {
                palette.AddOverlay(0, y, builtinPalette);
                palettes.Add(y++, builtinPalette);
            }

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

			var combatPaletteIndices = new Dictionary<int, byte>();

			for (int i = 0; i < combatPalettes.Length; i++)
			{
				combatPaletteIndices.Add(i + 1, y);
				palettes.Add(y, combatPalettes[i]);
				palette.AddOverlay(0, y++, combatPalettes[i]);
			}

			var loadingScreenPaletteIndices = new List<byte>();

			foreach (var loadingScreenPalette in loadingScreenPalettes)
			{
				loadingScreenPaletteIndices.Add(y);
				palettes.Add(y, loadingScreenPalette);
				palette.AddOverlay(0, y++, loadingScreenPalette);
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
			foreach (var b in Enum.GetValues<ButtonType>().Distinct())
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
            var partyMemberPortraitIndices = new Dictionary<int, int>();
            foreach (var partyMemberPortrait in assetProvider.GraphicLoader.LoadPersonPortraits())
            {
                partyMemberPortraitIndices.Add(partyMemberPortrait.Key, graphics.Count);
                graphics.Add(graphics.Count, partyMemberPortrait.Value);
            }
			int puzzleGraphicOffset = graphics.Count;
			foreach (var graphic in assetProvider.GraphicLoader.LoadPuzzleGraphics())
				graphics.Add(graphics.Count, graphic);
			int combatBackgroundOffset = graphics.Count;
			for (int i = 1; i <= ICombatData.CombatBackgroundCount; i++)
				graphics.Add(graphics.Count, assetProvider.GraphicLoader.LoadCombatBackground(i));
			int tacticalIconOffset = graphics.Count;
			foreach (var graphic in assetProvider.GraphicLoader.LoadTacticalIcons())
				graphics.Add(graphics.Count, graphic);
			int spellAnimationOffset = graphics.Count;
			foreach (var graphic in assetProvider.GraphicLoader.LoadSpellAnimationIcons())
				graphics.Add(graphics.Count, graphic);
			int damageDigitOffset = graphics.Count;
			for (int i = 0; i < 10; i++)
				graphics.Add(graphics.Count, assetProvider.CombatData.GetDamageDigit(i));
			int? loadingScreenIndex = null;
			var loadingScreen = assetProvider.GraphicLoader.LoadLoadingScreen();
			if (loadingScreen != null && loadingScreenPaletteIndices.Count != 0)
			{
				loadingScreenIndex = graphics.Count;
				graphics.Add(graphics.Count, loadingScreen);
			}
			var monsterGraphicOffsets = new Dictionary<int, int>();
			foreach (int monsterGraphic in assetProvider.GetAssetKeys(Amber.Common.AssetType.MonsterGraphics))
			{
				monsterGraphicOffsets.Add(monsterGraphic, graphics.Count);
				foreach (var graphic in assetProvider.GraphicLoader.LoadMonsterGraphic(monsterGraphic))
					graphics.Add(graphics.Count, graphic);
			}
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
					runeGlyphTextureIndices.Add((char)i, glyphTextureIndex);
					graphics.Add(glyphTextureIndex++, glyph);
				}
			}
			layer = renderer.LayerFactory.Create(LayerType.Texture2D, new()
			{
				BaseZ = 0.7f,
				LayerFeatures = LayerFeatures.Transparency | LayerFeatures.DisplayLayers | LayerFeatures.Alpha,
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
				LayerFeatures = LayerFeatures.Transparency | LayerFeatures.DisplayLayers | LayerFeatures.Alpha,
				RenderTarget = LayerRenderTarget.VirtualScreen2D,
			});
			layer.Visible = true;
			renderer.AddLayer(layer);

			#endregion


			#region Cutscenes

			// The outro and the intro have their own palettes (the palettes of the pictures with all fading steps
			// need many rows and the Amiga palettes have more than 16 colors) and graphics.
			var outroData = assetProvider.OutroData;
			var cutscenePalettes = new List<IGraphic>();
			var outroPaletteIndices = new Dictionary<(OutroPalette, int), byte>();
			graphics = [];
			var outroGraphicOffsets = new Dictionary<OutroGraphic, int>();
			var outroGlyphIndices = new Dictionary<char, int>();
			if (outroData != null)
			{
				foreach (var outroPalette in Enum.GetValues<OutroPalette>())
				{
					for (int step = -outroData.FadeSteps; step < outroData.FadeSteps; step++)
					{
						outroPaletteIndices.Add((outroPalette, step), (byte)cutscenePalettes.Count);
						cutscenePalettes.Add(outroData.GetPalette(outroPalette, step));
					}
				}
				foreach (var outroGraphic in Enum.GetValues<OutroGraphic>())
				{
					outroGraphicOffsets.Add(outroGraphic, graphics.Count);
					foreach (var graphic in outroData.GetGraphics(outroGraphic))
						graphics.Add(graphics.Count, graphic);
				}
				foreach (char ch in outroData.UsedCharacters)
				{
					var glyph = outroData.GetGlyph(ch);
					if (glyph != null)
					{
						outroGlyphIndices.Add(ch, graphics.Count);
						graphics.Add(graphics.Count, glyph);
					}
				}
			}
			var introData = assetProvider.IntroData;
			var introPaletteIndices = new Dictionary<int, byte>();
			var introGraphicOffsets = new Dictionary<IntroGraphic, int>();
			if (introData != null)
			{
				for (int step = -IIntroData.FadeSteps; step < IIntroData.FadeSteps; step++)
				{
					introPaletteIndices.Add(step, (byte)cutscenePalettes.Count);
					cutscenePalettes.Add(introData.GetBackgroundPalette(step));
				}
				introGraphicOffsets.Add(IntroGraphic.Background, graphics.Count);
				graphics.Add(graphics.Count, introData.Background);
				introGraphicOffsets.Add(IntroGraphic.Logo, graphics.Count);
				graphics.Add(graphics.Count, introData.Logo);
				introGraphicOffsets.Add(IntroGraphic.TextPage, graphics.Count);
				foreach (var page in introData.TextPages)
					graphics.Add(graphics.Count, page);
			}
			if (graphics.Count == 0)
				graphics.Add(0, new Graphic(1, 1, GraphicFormat.PaletteIndices));
			if (cutscenePalettes.Count == 0)
				cutscenePalettes.Add(builtinPalettes[0]);
			var cutscenePalette = new Graphic(cutscenePalettes.Max(p => p.Width), cutscenePalettes.Count, GraphicFormat.RGBA);
			for (int i = 0; i < cutscenePalettes.Count; i++)
				cutscenePalette.AddOverlay(0, i, cutscenePalettes[i]);
			layer = renderer.LayerFactory.Create(LayerType.ColorAndTexture2D, new()
			{
				BaseZ = 0.8f,
				LayerFeatures = LayerFeatures.Transparency | LayerFeatures.DisplayLayers,
				Palette = renderer.TextureFactory.Create(cutscenePalette),
				RenderTarget = LayerRenderTarget.VirtualScreen2D,
				Texture = renderer.TextureFactory.CreateAtlas(graphics),
			});
			layer.Visible = true;
			renderer.AddLayer(layer);

			// pictures with their own colors (RGBA)
			graphics = [];
			int thalionLogoIndex = 0;
			var thalionLogo = assetProvider.ThalionLogo;
			if (thalionLogo != null)
			{
				thalionLogoIndex = graphics.Count;
				graphics.Add(graphics.Count, thalionLogo.Frame);
			}
			if (introData != null)
			{
				introGraphicOffsets.Add(IntroGraphic.TitlePicture, graphics.Count);
				foreach (var picture in introData.TitlePictures)
					graphics.Add(graphics.Count, picture);
			}
			if (graphics.Count == 0)
				graphics.Add(0, new Graphic(1, 1, GraphicFormat.RGBA));
			layer = renderer.LayerFactory.Create(LayerType.Texture2D, new()
			{
				BaseZ = 0.8f,
				LayerFeatures = LayerFeatures.Transparency | LayerFeatures.DisplayLayers,
				RenderTarget = LayerRenderTarget.VirtualScreen2D,
				Texture = renderer.TextureFactory.CreateAtlas(graphics),
			});
			layer.Visible = true;
			renderer.AddLayer(layer);

			#endregion

			// TODO ...

			var builtinPaletteIndices = Enum.GetValues<BuiltinPalette>().ToDictionary(p => p, p => (byte)p);

            graphicIndexProvider = new(buttonOffset, statusIconOffset, uiGraphicOffset,
				image80x80Offset, itemGraphicOffset, windowGraphicOffset, cursorGraphicOffset,
				backgroundGraphicIndices, cloudGraphicIndices, labBlockImageIndices,
				partyMemberPortraitIndices, puzzleGraphicOffset, combatBackgroundOffset, tacticalIconOffset, monsterGraphicOffsets,
				spellAnimationOffset, damageDigitOffset, outroGraphicOffsets, outroGlyphIndices, introGraphicOffsets, thalionLogoIndex, loadingScreenIndex);
			paletteIndexProvider = new(builtinPaletteIndices, image80x80PaletteIndices, tilesetPaletteIndices, generalPaletteIndices, combatPaletteIndices,
				outroPaletteIndices, introPaletteIndices, loadingScreenPaletteIndices);
			paletteColorProvider = new(palettes);
			fontInfoProvider = new(textGlyphTextureIndices, runeGlyphTextureIndices);
		}
	}
}
