using Amber.Assets.Common;
using Amber.Common;
using Amber.Renderer.Common;
using AmberIsland.Game.UI;
using AmberIsland.GameData;
using Graphic = Amber.Assets.Common.Graphic;

namespace AmberIsland.Game;

public enum Layer
{
	/*MapBackground,
	Characters,*/
	Player,
	/*MapForeground,
	UI,
	TopMost = UI*/
	TopMost = Player
}

partial class Game
{
	internal const int VirtualScreenWidth = 320;
	internal const int VirtualScreenHeight = 200;

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

		// MapBackground
		/*AddLayer(LayerType.Texture2D, new()
		{
            BaseZ = 0.15f,
            RenderTarget = LayerRenderTarget.VirtualScreen2D,
            LayerFeatures = LayerFeatures.Transparency,
			// TODO
        });*/

        // Characters
        /*AddLayer(LayerType.Texture2D, new()
        {
            BaseZ = 0.3f,
            RenderTarget = LayerRenderTarget.VirtualScreen2D,
            LayerFeatures = LayerFeatures.Transparency,
            // TODO
        });*/

		// Player
		var playerSprite = gameData.GetPlayerSprite();
		var (playerAtlas, playerPalette) = CreateGraphicAtlasAndPalette(playerSprite);

        AddLayer(LayerType.Texture2D, new()
		{
			BaseZ = 0.45f,
			RenderTarget = LayerRenderTarget.VirtualScreen2D,
			LayerFeatures = LayerFeatures.Transparency,
			Texture = playerAtlas,
			Palette = playerPalette
        });

        // MapForeground
        /*AddLayer(LayerType.Texture2D, new()
        {
            BaseZ = 0.6f,
            RenderTarget = LayerRenderTarget.VirtualScreen2D,
            LayerFeatures = LayerFeatures.Transparency,
            // TODO
        });*/

		// UI
        /*AddLayer(LayerType.Texture2D, new()
        {
            BaseZ = 0.75f,
            RenderTarget = LayerRenderTarget.VirtualScreen2D,
            LayerFeatures = LayerFeatures.Transparency,
            // TODO
        });*/
    }

	private (ITextureAtlas Atlas, ITexture Palette) CreateGraphicAtlasAndPalette(SpriteWithPalettes sprite)
	{
		var atlasGraphic = new Graphic(sprite.Width, sprite.Height, sprite.ColorIndices, GraphicFormat.PaletteIndices);
		var atlas = Renderer.TextureFactory.CreateAtlas(new() { { 0, atlasGraphic } });

		int paletteWidth = sprite.Palettes[0].Colors.Length;
		int paletteHeight = sprite.Palettes.Length;
        var paletteData = new byte[paletteWidth * paletteHeight * 4];
		int index = 0;

		for (int y = 0; y < sprite.Palettes.Length; y++)
		{
			var palette = sprite.Palettes[y];
			Buffer.BlockCopy(palette.ToBytes() , 0, paletteData, index, paletteWidth * 4);
			index += paletteWidth * 4;
		}

        var paletteGraphic = new Graphic(paletteWidth, paletteHeight, paletteData, GraphicFormat.RGBA);

        return (atlas, Renderer.TextureFactory.Create(paletteGraphic));
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

    internal static void Destroy(IRenderText? text)
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
