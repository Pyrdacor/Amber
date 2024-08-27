using Amber.Common;
using Amber.Renderer;

namespace Amberstar.Game
{
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
}
