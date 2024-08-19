using Amber.Common;

namespace Amber.Renderer.OpenGL.Drawables
{
	internal class Sprite(Layer layer) : SizedDrawable(layer, layer.GetBufferForDrawable<Sprite>()
		?? throw new AmberException(ExceptionScope.Application, $"No render buffer found for drawable {nameof(Sprite)}")), ISprite
	{
		byte displayLayer;

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

		// TODO
		public Position TextureOffset { get; set; }
		public Size? TextureSize { get; set; }
		public byte PaletteIndex { get; set; }
		public byte? MaskColorIndex { get; set; }
		public bool MirrorX { get; set; }

		private protected override void UpdateVisibility()
		{
			if (Visible && DrawIndex == -1)
				DrawIndex = renderBuffer.GetDrawIndex(this, layer.PositionTransformation, layer.SizeTransformation);
		}
	}

	internal class SpriteFactory(Layer layer) : ISpriteFactory
	{
		public ISprite Create() => new Sprite(layer);
	}
}
