/*
 * Texture.cs - OpenGL texture handling
 *
 * Copyright (C) 2024  Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of Amber.
 *
 * Amber is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Amber is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Amber. If not, see <http://www.gnu.org/licenses/>.
 */

using Amber.Assets.Common;
using Amber.Common;
using Amber.Renderer.Common;

namespace Amber.Renderer.OpenGL;

internal class Texture : ITexture, IDisposable
{
    public static Texture? ActiveTexture { get; private set; } = null;

    readonly State state;
    bool disposed = false;

	public virtual uint Index { get; private set; } = 0u;
	public Size Size { get; }

	protected Texture(State state, Size size)
    {
        this.state = state;
        Index = state.Gl.GenTexture();
        Size = size;
    }

    public Texture(State state, IGraphic graphic, int numMipMapLevels = 0)
    {
        this.state = state;
        Index = state.Gl.GenTexture();
        Size = new(graphic.Width, graphic.Height);

        Create(graphic.Format, graphic.GetData(), numMipMapLevels);
    }

    static InternalFormat ToOpenGLInternalFormat(GraphicFormat format)
    {
        return format switch
        {
            GraphicFormat.RGBA => InternalFormat.Rgba8,
            _ => InternalFormat.R8
        };
    }

    static GLEnum ToOpenGLPixelFormat(GraphicFormat graphicFormat)
    {
		return graphicFormat switch
		{
			GraphicFormat.RGBA => GLEnum.Rgba,
            _ => GLEnum.Red, // single component
		};
	}

    protected void Create(GraphicFormat format, byte[] pixelData, int numMipMapLevels)
    {
        Bind();

        var minMode = (numMipMapLevels > 0) ? TextureMinFilter.NearestMipmapNearest : TextureMinFilter.Nearest;

        state.Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minMode);
        state.Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        state.Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        state.Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        state.Gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

        unsafe
        {
            fixed (byte* ptr = &pixelData[0])
            {
                state.Gl.TexImage2D(GLEnum.Texture2D, 0, (int)ToOpenGLInternalFormat(format), (uint)Size.Width, (uint)Size.Height, 0, ToOpenGLPixelFormat(format), GLEnum.UnsignedByte, ptr);
            }
        }

        if (numMipMapLevels > 0)
            state.Gl.GenerateMipmap(GLEnum.Texture2D);
    }

    public virtual void Bind()
    {
        if (disposed)
            throw new Exception("Tried to bind a disposed texture.");

        if (ActiveTexture == this)
            return;

        state.Gl.BindTexture(TextureTarget.Texture2D, Index);
        ActiveTexture = this;
    }

    public void Unbind()
    {
        if (ActiveTexture == this)
        {
            state.Gl.BindTexture(TextureTarget.Texture2D, 0);
            ActiveTexture = null;
        }
    }

    public void Dispose()
    {
        if (!disposed)
        {
            if (ActiveTexture == this)
                Unbind();

            if (Index != 0)
            {
                state.Gl.DeleteTexture(Index);
                Index = 0;
            }

            disposed = true;
        }
    }

    public void Use() => Bind();
}

internal class TextureFactory(State state) : ITextureFactory
{
	public ITexture Create(IGraphic graphic)
	{
        return new Texture(state, graphic);
	}
}
