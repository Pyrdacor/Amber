using Amber.Common;

namespace Amber.Renderer.OpenGL.Drawables
{
	internal abstract class Drawable : IDrawable
	{
		private protected readonly Layer layer;
		private protected readonly RenderBuffer renderBuffer;
		Position position = new(int.MaxValue, int.MaxValue); // not on screen
		bool visible = false;

		private protected Drawable(Layer layer, RenderBuffer renderBuffer)
		{
			this.layer = layer;
			this.renderBuffer = renderBuffer;
		}

		private protected int DrawIndex { get; set; } = -1;

		public Position Position
		{
			get => position;
			set
			{
				if (position != value)
				{
					position = value;
					UpdatePosition();

					if (position.X == int.MaxValue || position.Y == int.MaxValue)
						Visible = false;
					else if (VisibilityRequested)
						Visible = true;
				}
			}
		}

		public bool Visible
		{
			get => visible;
			set
			{
				if (visible != value)
				{
					if (value)
					{
						if (CanBeVisible())
						{
							visible = true;
							UpdateVisibility();
						}
						else
						{
							VisibilityRequested = true;
						}
					}
					else
					{
						visible = false;
						UpdateVisibility();
					}
				}
			}
		}

		private protected bool VisibilityRequested { get; private set; } = false;

		public ILayer Layer => layer;

		private protected abstract void UpdatePosition();
		private protected abstract void UpdateVisibility();
		private protected virtual bool CanBeVisible() => position.X != int.MaxValue && position.Y != int.MaxValue;
	}

	internal abstract class SizedDrawable : Drawable, ISizedDrawable
	{
		Size size = new(0, 0);
		int baseLineOffset;

		private protected SizedDrawable(Layer layer, RenderBuffer renderBuffer)
			: base(layer, renderBuffer)
		{
		}

		public Size Size
		{
			get => size;
			set
			{
				if (size != value)
				{
					size = value;
					UpdateSize();

					if (size.Empty)
						Visible = false;
					else if (VisibilityRequested)
						Visible = true;
				}
			}
		}

		public int BaseLineOffset
		{
			get => baseLineOffset;
			set
			{
				if (baseLineOffset != value)
				{
					baseLineOffset = value;

					if (Visible && DrawIndex != -1)
						renderBuffer.UpdateDisplayLayer(DrawIndex, (byte)MathUtil.Limit(0, Position.Y + Size.Height + baseLineOffset, 255));
				}
			}
		}

		private protected override void UpdatePosition()
		{
			if (DrawIndex != -1)
				renderBuffer.UpdatePosition(DrawIndex, this, 0, layer.PositionTransformation, layer.SizeTransformation);
		}

		private protected virtual void UpdateSize()
		{
			if (DrawIndex != -1)
				renderBuffer.UpdatePosition(DrawIndex, this, 0, layer.PositionTransformation, layer.SizeTransformation);
		}

		private protected override bool CanBeVisible() => base.CanBeVisible() && !size.Empty;
	}
}
