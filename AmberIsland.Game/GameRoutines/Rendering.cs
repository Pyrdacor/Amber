using Amber.Assets.Common;
using Amber.Common;
using Amber.Renderer.Common;
using AmberIsland.Game.UI;
using AmberIsland.GameData;
using Graphic = Amber.Assets.Common.Graphic;

namespace AmberIsland.Game;

public enum Layer
{
	MapBackground,
	Objects,
    //NPCs,
    Monsters,
    Player,
	Outfit,
    /*Capes,
	FaceAssets,*/
	Hair,
	Hats,
	PrimaryTool,
	SecondaryTool,
    Projectiles,
    MapForeground,
	MapFont,
    /*UI,
	UIFont,
	TopMost = UIFont*/
    TopMost = MapForeground
}

partial class Game
{
	public const int VirtualScreenWidth = 640;
    public const int VirtualScreenHeight = 400;

	IColoredRect? fadeArea;
	Color fadeColor = Color.Black;
	DateTime fadingStartTime = DateTime.MinValue;
	DateTime fadingEndTime = DateTime.MinValue;
	bool fadingOut = false;
	bool fadingIn = false;

    private void SetupLayers()
    {
        void AddLayer(LayerType type, LayerConfig config)
        {
            var layer = Renderer.LayerFactory.Create(type, config);
            Renderer.AddLayer(layer);
        }

        // TODO
        var tilesetAtlasSprite = gameData.GetTilesetAtlasSprite(1);
        var (tilesetAtlas, tilesetPalette) = CreateGraphicAtlasAndPalette(tilesetAtlasSprite, palette: null, new(16, 16));

        // MapBackground
        AddLayer(LayerType.Texture2D, new()
		{
            BaseZ = 0.2f,
            RenderTarget2D = LayerRenderTarget2D.VirtualScreen2D,
            LayerFeatures = LayerFeatures.Transparency,
			Texture = tilesetAtlas,
			Palette = tilesetPalette
        });

        // Objects
        AddLayer(LayerType.Texture2D, new()
        {
            BaseZ = 0.3f,
            RenderTarget2D = LayerRenderTarget2D.VirtualScreen2D,
            LayerFeatures = LayerFeatures.Transparency,
            Texture = tilesetAtlas,
            Palette = tilesetPalette
        });

        // NPCs
        /*AddLayer(LayerType.Texture2D, new()
        {
            BaseZ = 0.3f,
            RenderTarget = LayerRenderTarget.VirtualScreen2D,
            LayerFeatures = LayerFeatures.Transparency,
            // TODO
        });*/

        // Monsters
        var monsterSprites = gameData.GetMonsterAtlasSprites(1);
        var (monsterAtlas, monsterPalette) = CreateGraphicAtlasAndPalette(monsterSprites);

        AddLayer(LayerType.Texture2D, new()
        {
            BaseZ = 0.3f,
            RenderTarget2D = LayerRenderTarget2D.VirtualScreen2D,
            LayerFeatures = LayerFeatures.Transparency,
            Texture = monsterAtlas,
            Palette = monsterPalette
        });

        // Player
        var playerSpriteSheet = gameData.GetPlayerSpriteSheet();
		var (playerAtlas, playerPalette) = CreateGraphicAtlasAndPalette(playerSpriteSheet);

        AddLayer(LayerType.Texture2D, new()
		{
			BaseZ = 0.3f,
			RenderTarget2D = LayerRenderTarget2D.VirtualScreen2D,
			LayerFeatures = LayerFeatures.Transparency,
			Texture = playerAtlas,
			Palette = playerPalette
        });

        // Outfit
        var outfitSpriteSheet = gameData.GetOutfitSpriteSheet();
        var (outfitAtlas, outfitPalette) = CreateGraphicAtlasAndPalette(outfitSpriteSheet);

        AddLayer(LayerType.Texture2D, new()
        {
            BaseZ = 0.325f,
            RenderTarget2D = LayerRenderTarget2D.VirtualScreen2D,
            LayerFeatures = LayerFeatures.Transparency,
            Texture = outfitAtlas,
            Palette = outfitPalette
        });

        // Hair
        var hairSpriteSheet = gameData.GetHairSpriteSheet();
        var (hairAtlas, hairPalette) = CreateGraphicAtlasAndPalette(hairSpriteSheet);

        AddLayer(LayerType.Texture2D, new()
        {
            BaseZ = 0.35f,
            RenderTarget2D = LayerRenderTarget2D.VirtualScreen2D,
            LayerFeatures = LayerFeatures.Transparency,
            Texture = hairAtlas,
            Palette = hairPalette
        });

        // Hat
        var hatSpriteSheet = gameData.GetHatSpriteSheet();
        var (hatAtlas, hatPalette) = CreateGraphicAtlasAndPalette(hatSpriteSheet);

        AddLayer(LayerType.Texture2D, new()
        {
            BaseZ = 0.375f,
            RenderTarget2D = LayerRenderTarget2D.VirtualScreen2D,
            LayerFeatures = LayerFeatures.Transparency,
            Texture = hatAtlas,
            Palette = hatPalette
        });

        // Primary Tool
        var primaryToolSpriteSheet = gameData.GetPrimaryToolSpriteSheet();
        var (primaryToolAtlas, primaryToolPalette) = CreateGraphicAtlasAndPalette(primaryToolSpriteSheet);

        AddLayer(LayerType.Texture2D, new()
        {
            BaseZ = 0.4f,
            RenderTarget2D = LayerRenderTarget2D.VirtualScreen2D,
            LayerFeatures = LayerFeatures.Transparency,
            Texture = primaryToolAtlas,
            Palette = primaryToolPalette
        });

        // Secondary Tool
        var secondaryToolSpriteSheet = gameData.GetSecondaryToolSpriteSheet();
        var (secondaryToolAtlas, secondaryToolPalette) = CreateGraphicAtlasAndPalette(secondaryToolSpriteSheet);

        AddLayer(LayerType.Texture2D, new()
        {
            BaseZ = 0.425f,
            RenderTarget2D = LayerRenderTarget2D.VirtualScreen2D,
            LayerFeatures = LayerFeatures.Transparency,
            Texture = secondaryToolAtlas,
            Palette = secondaryToolPalette
        });

        // Projectiles
        var projectileSprites = gameData.GetProjectileAtlasSprites(1);
        var (projectileAtlas, projectilePalette) = CreateGraphicAtlasAndPalette(projectileSprites);

        AddLayer(LayerType.Texture2D, new()
        {
            BaseZ = 0.45f,
            RenderTarget2D = LayerRenderTarget2D.VirtualScreen2D,
            LayerFeatures = LayerFeatures.Transparency,
            Texture = projectileAtlas,
            Palette = projectilePalette
        });

        // MapForeground
        AddLayer(LayerType.Texture2D, new()
        {
            BaseZ = 0.5f,
            RenderTarget2D = LayerRenderTarget2D.VirtualScreen2D,
            LayerFeatures = LayerFeatures.Transparency,
            Texture = tilesetAtlas,
            Palette = tilesetPalette
        });

        // MapFont
        var fonts = gameData.GetFonts();
        var fontAtlas = CreateFontGraphicAtlas(fonts);
		var textPalette = CreatePaletteTexture(gameData.GetTextPalette());

        AddLayer(LayerType.Texture2D, new()
        {
            BaseZ = 0.6f,
            RenderTarget2D = LayerRenderTarget2D.Window, // Higher resolution looks nicer
            LayerFeatures = LayerFeatures.Transparency | LayerFeatures.Alpha | LayerFeatures.DisplayLayers,
            Texture = fontAtlas,
            Palette = textPalette
        });

        // UI
        /*AddLayer(LayerType.Texture2D, new()
        {
            BaseZ = 0.75f,
            RenderTarget = LayerRenderTarget.VirtualScreen2D,
            LayerFeatures = LayerFeatures.Transparency,
            // TODO
        });*/
    }

    private (ITextureAtlas Atlas, ITexture Palette) CreateGraphicAtlasAndPalette(Sprite sprite, PaletteRgb? palette, Size? tileSize)
	{
		if (sprite.Colors.Length == 0)
		{
			if (palette == null)
				throw new InvalidOperationException("Sprite has no embedded palette and no palette was given.");

			// TODO ...
			throw new NotImplementedException();
		}
		else
		{
            var atlasGraphic = new Graphic(sprite.Width, sprite.Height, sprite.ColorIndices, GraphicFormat.PaletteIndices);
			ITextureAtlas atlas;
			
			if (tileSize == null || tileSize.Value.Empty)
				atlas = Renderer.TextureFactory.CreateAtlas(new() { { 0, atlasGraphic } });
			else
			{
				int tileWidth = tileSize.Value.Width;
				int tileHeight = tileSize.Value.Height;
				int tilesPerRow = sprite.Width / tileWidth;
				int tileRows = sprite.Height / tileHeight;
                var areas = new Dictionary<int, Rect>(tileRows * tilesPerRow);
				int index = 0;

                for (int y = 0; y < tileRows; y++)
				{
					for (int x = 0; x < tilesPerRow; x++)
					{
						areas.Add(index++, new(x * tileWidth, y * tileHeight, tileWidth, tileHeight));
                    }
				}

                atlas = Renderer.TextureFactory.CreateAtlas(areas, atlasGraphic);
            }

            int paletteWidth = 1 + sprite.Colors.Length;
            int paletteHeight = 1;
            var paletteData = new byte[paletteWidth * paletteHeight * 4];

            var embeddedPalette = new PaletteRgb(sprite.Colors);
            Buffer.BlockCopy(embeddedPalette.ToBytes(), 0, paletteData, 0, paletteWidth * 4);

            var paletteGraphic = new Graphic(paletteWidth, paletteHeight, paletteData, GraphicFormat.RGBA);

            return (atlas, Renderer.TextureFactory.Create(paletteGraphic));
        }
	}

    private (ITextureAtlas Atlas, ITexture Palette) CreateGraphicAtlasAndPalette(SpriteWithPalettes sprite)
	{
		var atlasGraphic = new Graphic(sprite.Width, sprite.Height, sprite.ColorIndices, GraphicFormat.PaletteIndices);
		var atlas = Renderer.TextureFactory.CreateAtlas(new() { { 1, atlasGraphic } });

        return (atlas, CreatePaletteTexture(sprite.Palettes));
    }

    internal (ITextureAtlas Atlas, ITexture Palette) CreateGraphicAtlasAndPalette(PlayerSpriteSheet playerSpriteSheet)
    {
        var atlas = playerSpriteSheet.Atlas;
        var atlasGraphic = new Graphic(atlas.Width, atlas.Height, atlas.ColorIndices, GraphicFormat.PaletteIndices);
		var areas = new Dictionary<int, Rect>(playerSpriteSheet.StateSprites.Length);
		var frameSize = PlayerStateSprites.FrameSize;

        foreach (var stateSprites in playerSpriteSheet.StateSprites)
		{
            var spriteArea = new Rect(stateSprites.OffsetX, stateSprites.OffsetY,
                (1 + stateSprites.FrameIndices.Max()) * frameSize.Width, frameSize.Height);
            areas.Add((int)stateSprites.State, spriteArea);
        }

        var textureAtlas = Renderer.TextureFactory.CreateAtlas(areas, atlasGraphic);

        return (textureAtlas, CreatePaletteTexture(playerSpriteSheet.Atlas.Palettes));
    }

    internal (ITextureAtlas Atlas, ITexture Palette) CreateGraphicAtlasAndPalette(MapSpriteAtlas mapSpriteAtlas)
    {
		var atlas = mapSpriteAtlas.Atlas;
        var atlasGraphic = new Graphic(atlas.Width, atlas.Height, atlas.ColorIndices, GraphicFormat.PaletteIndices);
		var areas = mapSpriteAtlas.Sprites.ToDictionary(sprite => (int)sprite.Key, sprite => new Rect(sprite.Value.Position, sprite.Value.Size));
        var textureAtlas = Renderer.TextureFactory.CreateAtlas(areas, atlasGraphic);

        int paletteWidth = 1 + mapSpriteAtlas.Palettes.Max(palette => palette.Colors.Length);
        int paletteHeight = mapSpriteAtlas.Palettes.Length;
        var paletteData = new byte[paletteWidth * paletteHeight * 4];
		int offset = 0;

		foreach (var palette in mapSpriteAtlas.Palettes)
		{
			var paletteBytes = palette.ToBytes();
            Buffer.BlockCopy(paletteBytes, 0, paletteData, offset, paletteBytes.Length);
			offset += paletteWidth * 4;
		}

        var paletteGraphic = new Graphic(paletteWidth, paletteHeight, paletteData, GraphicFormat.RGBA);

        return (textureAtlas, CreatePaletteTexture(mapSpriteAtlas.Palettes));
    }

    internal ITexture CreatePaletteTexture(params PaletteRgb[] palettes)
    {
        int paletteWidth = 1 + palettes.Max(palette => palette.Colors.Length);
        int paletteHeight = palettes.Length;
        var paletteData = new byte[paletteWidth * paletteHeight * 4];
		int offset = 0;

		foreach (var palette in palettes)
		{
			var paletteBytes = palette.ToBytes();
            Buffer.BlockCopy(paletteBytes, 0, paletteData, offset, paletteBytes.Length);
			offset += paletteWidth * 4;
		}

        var paletteGraphic = new Graphic(paletteWidth, paletteHeight, paletteData, GraphicFormat.RGBA);

        return Renderer.TextureFactory.Create(paletteGraphic);
    }

    private ITextureAtlas CreateFontGraphicAtlas(Dictionary<uint, Font> fonts)
    {
		// TODO: Better packing later
		int x = 0;
		int y = 0;
		int width = 0;
		var areas = new Dictionary<int, Rect>(fonts.Count);

		foreach (var font in fonts.OrderBy(font => font.Key))
		{
			areas.Add((int)font.Key, new(x, y, (int)font.Value.AtlasWidth, (int)font.Value.AtlasHeight));

            if (font.Value.AtlasWidth > width)
				width = (int)font.Value.AtlasWidth;

			y += (int)font.Value.AtlasHeight;
		}

		int height = y;
		byte[] colorIndices = new byte[width * height];
		y = 0;

        foreach (var font in fonts.OrderBy(font => font.Key))
        {
			int atlasWidth = (int)font.Value.AtlasWidth;

            for (int ay = 0; ay < font.Value.AtlasHeight; ay++)
			{
				Buffer.BlockCopy(font.Value.AtlasAlphaValues, ay * atlasWidth, colorIndices, y++ * width, atlasWidth);
			}
        }

        var atlasGraphic = new Graphic(width, height, colorIndices, GraphicFormat.Alpha);
        var atlas = Renderer.TextureFactory.CreateAtlas(areas, atlasGraphic);

        return atlas;
    }

    internal ILayer GetRenderLayer(Layer layer) => Renderer.Layers[(int)layer];

	internal ISprite? CreateSprite(Layer layer, Position position, Size size, int textureIndex, int paletteIndex, bool opaque = false)
	{
		var renderLayer = GetRenderLayer(layer);
		var textureAtlas = renderLayer.Config.Texture!;
		var sprite = renderLayer.SpriteFactory?.Create();

		if (sprite != null)
		{
			sprite.TextureOffset = textureAtlas.GetOffset(textureIndex);
			sprite.Position = position;
			sprite.Size = size;
			sprite.PaletteIndex = (byte)paletteIndex;
			sprite.Opaque = opaque;
			sprite.Visible = true;
		}

		return sprite;
	}

	internal IColoredRect? CreateColoredRect(Layer layer, Position position, Size size, Color color)
	{
		var renderLayer = GetRenderLayer(layer);
		var coloredRect = renderLayer.ColoredRectFactory?.Create();

		if (coloredRect != null)
		{
			coloredRect.Color = color;
			coloredRect.Position = position;
			coloredRect.Size = size;
			coloredRect.Visible = true;
		}

		return coloredRect;
	}

	internal static void Destroy(IDrawable? drawable)
	{
		if (drawable != null)
			drawable.Visible = false;
	}

    internal static void Destroy(RenderText? text)
    {
		if (text != null)
			text.Visible = false;
    }

    /// <summary>
    /// Fades from a fully colored screen to the normal screen.
    /// This only works if FadeOut was use before.
    /// </summary>
    internal void FadeIn(long durationInMs, Action? finishAction = null)
	{
		if (durationInMs <= 0)
			return;

		fadeColor = new(fadeColor, 255);
		fadeArea ??= CreateColoredRect(Layer.TopMost, new(0, 0), new(VirtualScreenWidth, VirtualScreenHeight), fadeColor);
		fadingStartTime = DateTime.Now;
		fadingEndTime = fadingStartTime + TimeSpan.FromMilliseconds(durationInMs);
		fadingIn = true;
		fadingOut = false;

		if (finishAction != null)
			AddDelayedAction(TimeSpan.FromMilliseconds(durationInMs), finishAction);
	}

	/// <summary>
	/// Fades from the normal screen to a fully colored screen.
	/// This only works if FadeOut was not use before.
	/// 
	/// Defaults to black color fading.
	/// </summary>
	internal void FadeOut(long durationInMs, Action? finishAction = null, Color? color = null)
	{
		if (fadingIn || durationInMs <= 0 || fadeArea != null)
			return;

		fadeColor = new(color ?? Color.Black, 0);
		fadeArea = CreateColoredRect(Layer.TopMost, new(0, 0), new(VirtualScreenWidth, VirtualScreenHeight), fadeColor);
		fadingStartTime = DateTime.Now;
		fadingEndTime = fadingStartTime + TimeSpan.FromMilliseconds(durationInMs);
		fadingOut = true;

		if (finishAction != null)
			AddDelayedAction(TimeSpan.FromMilliseconds(durationInMs), finishAction);
	}

	internal void Fade(long durationInMs, Action? finishAction = null, Action? afterFadeOutAction = null, Color? color = null)
	{
		if (durationInMs <= 1)
			return;

		if (fadingIn)
		{
			// Wait for completion and then execute the fade
			AddDelayedAction((fadingEndTime - DateTime.Now + TimeSpan.FromMilliseconds(100)), () => Fade(durationInMs, finishAction, afterFadeOutAction, color));
			return;
		}

		color ??= Color.Black;

		FadeOut(durationInMs / 2, afterFadeOutAction, color);
		AddDelayedAction(TimeSpan.FromMilliseconds(durationInMs / 2), () => FadeIn(durationInMs / 2, finishAction));
	}

	private void UpdateFading()
	{
		int Change()
		{
			var totalMs = (fadingEndTime - fadingStartTime).TotalMilliseconds;
			var elapsedMs = (DateTime.Now - fadingStartTime).TotalMilliseconds;

			return MathUtil.Limit(0, MathUtil.Round(elapsedMs * 255 / totalMs), 255);
		}

		if (fadeArea != null)
		{
			if (fadingIn)
			{
				byte alpha = (byte)(255 - Change());

				if (fadeArea.Color.A != alpha)
				{
					fadeArea.Color = new(fadeArea.Color, alpha);

					if (alpha == 0)
					{
						fadingIn = false;
						fadeArea.Visible = false;
						fadeArea = null;
					}
				}
			}
			else if (fadingOut)
			{
				byte alpha = (byte)Change();

				if (fadeArea.Color.A != alpha)
				{
					fadeArea.Color = new(fadeArea.Color, alpha);

					if (alpha == 255)
					{
						fadingOut = false;
						fadeArea.Visible = false;
						fadeArea = null;
					}
				}
			}				
		}
	}
}
