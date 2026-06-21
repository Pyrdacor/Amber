using System.Numerics;
using Amber.Common;
using Amber.Renderer.Common;

namespace Amber.Renderer.OpenGL.Drawables
{
    internal class Surface3D(Layer layer) : Drawable(layer, layer.GetBufferForDrawable<Surface3D>()
		?? throw new AmberException(ExceptionScope.Application, $"No render buffer found for drawable {nameof(Surface3D)}")), ISurface3D
	{
        SurfaceFace face;
        Vector3 position = new(float.MaxValue); // not on screen
        FloatSize size;
		Position textureOffset;
		Size? textureSize;
		byte paletteIndex;
		bool mirrorX = false;
		byte alpha = 255;
        int currentFrameIndex;
        int frameCount = 1;
        private Vector2 textureSizeFactor = new(1.0f, 1.0f);

        public SurfaceFace Face
        {
            get => face;
            set
            {
                if (face != value)
                {
                    face = value;
                    UpdatePosition();
                }
            }
        }

        public Vector3 Position
        {
            get => position;
            set
            {
                if (position != value)
                {
                    position = value;
                    UpdatePosition();

                    if (position.X == float.MaxValue || position.Y == float.MaxValue || position.Z == float.MaxValue)
                        Visible = false;
                    else if (VisibilityRequested)
                        Visible = true;
                }
            }
        }

        public FloatSize Size
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

        public byte Alpha
        {
            get => alpha;
            set
            {
                if (alpha != value)
                {
                    alpha = value;

                    if (Visible && DrawIndex != -1)
                        renderBuffer.UpdateAlpha(DrawIndex, alpha);
                }
            }
        }

        public Vector2 TextureSizeFactor 
        { 
            get => textureSizeFactor;
            set
            {
                if (textureSizeFactor != value)
                {
                    textureSizeFactor = value;

                    if (Visible && DrawIndex != -1)
                        renderBuffer.UpdateTextureOffset(DrawIndex, this);
                }
            }
        }

        private protected override void UpdateVisibility()
		{
			if (Visible && DrawIndex == -1)
				DrawIndex = renderBuffer.GetDrawIndex(this);
			else if (!Visible && DrawIndex != -1)
			{
				renderBuffer.FreeDrawIndex(DrawIndex);
				DrawIndex = -1;
			}
		}

        private protected virtual void UpdatePosition()
        {
            if (DrawIndex != -1)
                renderBuffer.UpdatePosition(DrawIndex, this);
        }

        private protected virtual void UpdateSize()
        {
            if (DrawIndex != -1)
                renderBuffer.UpdatePosition(DrawIndex, this);
        }

        private protected override bool CanBeVisible() => position.X != float.MaxValue && position.Y != float.MaxValue && position.Z != float.MaxValue;
    }

    internal class Surface3DFactory(Layer layer) : ISurface3DFactory
	{
		public ISurface3D Create() => new Surface3D(layer);
    }
}
