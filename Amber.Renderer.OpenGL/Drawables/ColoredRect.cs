using Amber.Common;

namespace Amber.Renderer.OpenGL.Drawables
{
	internal class ColoredRect(Layer layer) : SizedDrawable(layer, layer.GetBufferForDrawable<ColoredRect>()
		?? throw new AmberException(ExceptionScope.Application, $"No render buffer found for drawable {nameof(ColoredRect)}")), IColoredRect
	{
		Color color;
		byte displayLayer;

		public Color Color
		{
			get => color;
			set
			{
				if (color != value)
				{
					color = value;

					if (Visible && DrawIndex != -1)
						renderBuffer.UpdateColor(DrawIndex, color);
				}
			}
		}

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

		private protected override void UpdateVisibility()
		{
			if (Visible && DrawIndex == -1)
				DrawIndex = renderBuffer.GetDrawIndex(this, layer.PositionTransformation, layer.SizeTransformation);
		}
	}

	internal class ColoredRectFactory(Layer layer) : IColoredRectFactory
	{
		public IColoredRect Create() => new ColoredRect(layer);
	}
}
