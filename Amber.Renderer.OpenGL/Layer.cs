/*
 * Layer.cs - Render layer implementation
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

using Amber.Common;
using Amber.Renderer.OpenGL.Shaders;

namespace Amber.Renderer.OpenGL;

internal class Layer : ILayer, IDisposable
{
    static int NextIndex = 1;
    readonly List<BaseShader> shaders = [];
    readonly State state;
	bool disposed = false;

	public bool Visible
    {
        get;
        set;
    }

    public LayerConfig Config { get; }

	public int Index { get; }

	public ISpriteFactory SpriteFactory => throw new NotImplementedException();

	public Layer(State state, LayerConfig config)
    {
        // TODO: create render buffer
        Index = NextIndex++;
		Config = config;
        Visible = true;
        this.state = state;

        if (config.LayerFeatures.HasFlag(LayerFeatures.Sprites) && Config.Texture == null)
            throw new AmberException(ExceptionScope.Application, "Layer supports sprites but has no texture.");

		if (config.LayerFeatures.HasFlag(LayerFeatures.Palette) && Config.Palette == null)
			throw new AmberException(ExceptionScope.Application, "Layer supports palettes but has no palette.");

        if (config.LayerFeatures.HasFlag(LayerFeatures.ColoredRects))
        {
            shaders.Add(ColorShader.Create(state));
		}

        if (config.LayerFeatures.HasFlag(LayerFeatures.Sprites))
        {
            if (config.LayerFeatures.HasFlag(LayerFeatures.Palette))
                shaders.Add(TextureShader.Create(state));
			// else
			//  shaders.Add(ImageShader.Create(state));
		}

		// TODO ...

		foreach (var shader in shaders)
        {
			if (shader is ITextureShader textureShader)
			{
				textureShader.SetTexture(0);
				state.Gl.ActiveTexture(GLEnum.Texture0);
				Config.Texture!.Use();

				if (Config.Palette != null && shader is IPaletteShader paletteShader)
				{
					paletteShader.SetPalette(1);
					state.Gl.ActiveTexture(GLEnum.Texture1);
					Config.Palette.Use();

                    paletteShader.SetPaletteCount(Config.Palette.Size.Height);
				}

				textureShader.SetAtlasSize((uint)Config.Texture.Size.Width, (uint)Config.Texture.Size.Height);
			}
		}
	}

    public void Render(IRenderer renderer)
    {
        if (!Visible)
            return;

		foreach (var shader in shaders)
		{
			shader.SetZ(Config.BaseZ);
            shader.UpdateMatrices(state);

            if (shader is  IPaletteShader paletteShader)
                paletteShader.SetColorKey(0); // TODO

			// TODO ...
		}

        // RenderBuffer?.Render();
    }

    public void Dispose()
    {
        if (!disposed)
        {
            //RenderBuffer?.Dispose();
            //texture?.Dispose();
            Visible = false;

            disposed = true;
        }
    }
}

internal class LayerFactory : ILayerFactory
{
    readonly State state;

    public LayerFactory(State state)
    {
        this.state = state;
    }

	public ILayer Create(LayerConfig config)
	{
		return new Layer(state, config);
	}
}
