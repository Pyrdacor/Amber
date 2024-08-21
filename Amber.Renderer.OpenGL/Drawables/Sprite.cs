using Amber.Common;

namespace Amber.Renderer.OpenGL.Drawables
{
	internal class Sprite(Layer layer) : SizedDrawable(layer, layer.GetBufferForDrawable<Sprite>()
		?? throw new AmberException(ExceptionScope.Application, $"No render buffer found for drawable {nameof(Sprite)}")), ISprite
	{
		byte displayLayer;
		Position textureOffset;
		Size? textureSize;
		byte paletteIndex;
		byte? maskColorIndex;
		bool mirrorX;
		bool opaque;

		public byte DisplayLayer
		{
			get => displayLayer;
			set
			{
				if (displayLayer != value)
				{
					displayLayer = value;

					if (Visible && DrawIndex != -1)
						renderBuffer.UpdateDisplayLayer(DrawIndex, displayLayer);
				}
			}
		}

		public Position TextureOffset
		{
			get => textureOffset;
			set
			{
				if (textureOffset != value)
				{
					textureOffset = value;

					if (Visible && DrawIndex != -1)
						renderBuffer.UpdateTextureOffset(DrawIndex, this);
				}
			}
		}

		public Size? TextureSize
		{
			get => textureSize;
			set
			{
				if (textureSize != value)
				{
					textureSize = value;

					if (Visible && DrawIndex != -1)
						renderBuffer.UpdateTextureOffset(DrawIndex, this);
				}
			}
		}

		public byte PaletteIndex
		{
			get => paletteIndex;
			set
			{
				if (paletteIndex != value)
				{
					paletteIndex = value;

					if (Visible && DrawIndex != -1)
						renderBuffer.UpdatePaletteIndex(DrawIndex, paletteIndex);
				}
			}
		}

		public byte? MaskColorIndex
		{
			get => maskColorIndex;
			set
			{
				if (maskColorIndex != value)
				{
					maskColorIndex = value;

					if (Visible && DrawIndex != -1)
						renderBuffer.UpdateMaskColor(DrawIndex, maskColorIndex);
				}
			}
		}

		public bool MirrorX
		{
			get => mirrorX;
			set
			{
				if (mirrorX != value)
				{
					mirrorX = value;

					if (Visible && DrawIndex != -1)
						renderBuffer.UpdateTextureOffset(DrawIndex, this);
				}
			}
		}

		public bool Opaque
		{
			get => opaque;
			set
			{
				if (opaque != value)
				{
					opaque = value;

					if (Visible && DrawIndex != -1)
						renderBuffer.UpdateOpaque(DrawIndex, (byte)(opaque ? 1 : 0));
				}
			}
		}

		private protected override void UpdateVisibility()
		{
			if (Visible && DrawIndex == -1)
				DrawIndex = renderBuffer.GetDrawIndex(this, layer.PositionTransformation, layer.SizeTransformation);
		}
	}

	internal class AnimatedSprite(Layer layer) : Sprite(layer), IAnimatedSprite
	{
		int currentFrameIndex;
		int frameCount = 1;

		public int CurrentFrameIndex
		{
			get => currentFrameIndex;
			set
			{
				value %= frameCount;

				if (value < 0)
					value += frameCount;

				if (currentFrameIndex != value)
				{
					currentFrameIndex = value;

					if (Visible && DrawIndex != -1)
						renderBuffer.UpdateTextureOffset(DrawIndex, this);
				}
			}
		}

		public int FrameCount
		{
			get => frameCount;
			set
			{
				if (frameCount != value)
				{
					if (value < 1)
						value = 1;

					frameCount = value;

					if (currentFrameIndex >= frameCount)
						currentFrameIndex = frameCount - 1;

					if (Visible && DrawIndex != -1)
						renderBuffer.UpdateTextureOffset(DrawIndex, this);
				}
			}
		}
	}

	internal class SpriteFactory(Layer layer) : ISpriteFactory
	{
		public ISprite Create() => new Sprite(layer);

		public IAnimatedSprite CreateAnimated() => new AnimatedSprite(layer);
	}
}
