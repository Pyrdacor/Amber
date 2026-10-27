using Amber.Common;
using Amber.Renderer;
using Amber.Renderer.Common;
using Amberstar.Game.UI;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game;

partial class Game
{
	internal const int VirtualScreenWidth = 320;
	internal const int VirtualScreenHeight = 200;
	IColoredRect? fadeArea;
	Color fadeColor = Color.Black;
	DateTime fadingStartTime = DateTime.MinValue;
	DateTime fadingEndTime = DateTime.MinValue;
	bool fadingOut = false;
	bool fadingHold = false;
	bool fadingIn = false;
	Action? afterFadeOutAction;
    Action? afterFadeInAction;
	long fadeActionIndex;
	// the loading screen of the original (a dwarf) which is shown on map changes instead of fading
	const int LoadingScreenX = 96;
	const int LoadingScreenY = 62;
	const double LoadingScreenFadeStepTime = 0.02; // 1 step per vertical blank
	static readonly Rect loadingScreenArea = new(0, 37, VirtualScreenWidth, VirtualScreenHeight - 37);
	IColoredRect? loadingScreenBackground;
	ISprite? loadingScreenSprite;
	DateTime loadingScreenStartTime;
	double loadingScreenDuration;

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
    private void FadeIn(long durationInMs)
	{
		if (durationInMs <= 0)
			return;

		fadeColor = new(fadeColor, 255);
		fadeArea ??= CreateColoredRect(Layer.TopMost, new(0, 0), new(VirtualScreenWidth, VirtualScreenHeight), fadeColor);
		fadingStartTime = Now;
		fadingEndTime = fadingStartTime + TimeSpan.FromMilliseconds(durationInMs);

		if (fadingOut && afterFadeOutAction != null)
		{
			afterFadeOutAction();
			afterFadeOutAction = null;
        }

		fadingIn = true;
		fadingOut = false;
		fadingHold = false;
	}

	/// <summary>
	/// Fades from the normal screen to a fully colored screen.
	/// 
	/// Defaults to black color fading.
	/// </summary>
	private void FadeOut(long durationInMs, Color? color = null)
	{
		if (fadingIn || durationInMs <= 0)
			return;

		fadeColor = new(color ?? Color.Black, 0);
		fadeArea ??= CreateColoredRect(Layer.TopMost, new(0, 0), new(VirtualScreenWidth, VirtualScreenHeight), fadeColor);
		fadingStartTime = Now;
		fadingEndTime = fadingStartTime + TimeSpan.FromMilliseconds(durationInMs);
		fadingOut = true;
	}

	internal void Fade(long durationInMs, Action? finishAction = null, Action? afterFadeOutAction = null, Color? color = null)
	{
		if (durationInMs <= 1)
		{
			afterFadeOutAction?.Invoke();
			finishAction?.Invoke();
			return;
		}


		if (fadingOut)
		{
			// Usually this happens if a Fade call is executed in a afterFadeOutAction action.
			// So in this case we can immediately execute the given afterFadeOutAction as well.
			afterFadeOutAction?.Invoke();

			if (finishAction != null)
			{
				if (afterFadeInAction != null)
				{
					var oldFadeInAction = afterFadeInAction;
					afterFadeInAction = () =>
					{
						oldFadeInAction();
						finishAction();
					};
                }
				else
				{
					afterFadeInAction = finishAction;
                }
			}

			return;
        }
		else if (fadingIn)
		{
            afterFadeInAction?.Invoke();

            fadingIn = false;
            fadingOut = false;
            fadingHold = false;
        }       

		color ??= Color.Black;

		long holdTime = durationInMs / 10;
		long remainingTime = durationInMs - holdTime;

		if (remainingTime % 2 == 1)
		{
			remainingTime--;
			holdTime++;
		}

		fadingHold = true;
		this.afterFadeOutAction = afterFadeOutAction;
		afterFadeInAction = finishAction;
        FadeOut(remainingTime / 2, color);
        fadeActionIndex = AddDelayedAction(TimeSpan.FromMilliseconds(remainingTime / 2 + holdTime), () => FadeIn(remainingTime / 2));
	}

	internal bool CanShowLoadingScreen => IsOptionSet(GameOptions.ShowDwarfLoadingScreen) && GraphicIndexProvider.GetLoadingScreenIndex() != null;

	/// <summary>
	/// Shows the loading screen like the original does while it loads a map (L194 and L208 in the program):
	/// the area below the portraits is cleared, the dwarf fades in, stays while loading and fades out.
	/// It only covers the screen, a fading which runs at the same time is not shown.
	/// </summary>
	internal void ShowLoadingScreen(long durationInMs)
	{
		if (loadingScreenSprite != null || GraphicIndexProvider.GetLoadingScreenIndex() is not int textureIndex)
			return;

		loadingScreenBackground = CreateColoredRect(Layer.UI, loadingScreenArea.Position, loadingScreenArea.Size, Color.Black)!;
		loadingScreenBackground.DisplayLayer = 253;
		loadingScreenSprite = CreateSprite(Layer.UI, new(LoadingScreenX, LoadingScreenY),
			new(IGraphicLoader.LoadingScreenWidth, IGraphicLoader.LoadingScreenHeight), textureIndex,
			PaletteIndexProvider.GetLoadingScreenPaletteIndex(IGraphicLoader.LoadingScreenFadeSteps), true)!;
		loadingScreenSprite.DisplayLayer = 254;
		loadingScreenStartTime = Now;
		loadingScreenDuration = durationInMs / 1000.0;
	}

	private void UpdateLoadingScreen()
	{
		if (loadingScreenSprite == null)
			return;

		double elapsed = (Now - loadingScreenStartTime).TotalSeconds;

		if (elapsed >= loadingScreenDuration)
		{
			loadingScreenSprite.Visible = false;
			loadingScreenSprite = null;
			loadingScreenBackground!.Visible = false;
			loadingScreenBackground = null;
			return;
		}

		int steps = IGraphicLoader.LoadingScreenFadeSteps;
		int fadeIn = (int)(elapsed / LoadingScreenFadeStepTime);
		int fadeOut = (int)((loadingScreenDuration - elapsed) / LoadingScreenFadeStepTime);
		int fadeStep = Math.Max(0, steps - Math.Min(fadeIn, fadeOut));
		loadingScreenSprite.PaletteIndex = PaletteIndexProvider.GetLoadingScreenPaletteIndex(fadeStep);
	}

	private void UpdateFading()
	{
		UpdateLoadingScreen();

		if (fadeArea != null)
			fadeArea.Visible = loadingScreenSprite == null;

		int Change()
		{
			var totalMs = (fadingEndTime - fadingStartTime).TotalMilliseconds;
			var elapsedMs = (Now - fadingStartTime).TotalMilliseconds;

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

                        afterFadeInAction?.Invoke();
                        afterFadeInAction = null;
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
                        afterFadeOutAction?.Invoke();
                        afterFadeOutAction = null;

                        fadingOut = false;						

						if (!fadingHold)
						{
							fadeArea.Visible = false;
							fadeArea = null;
						}
					}
				}
			}
		}
	}
}
