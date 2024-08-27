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
using Amber.Renderer.OpenGL.Drawables;
using Amber.Renderer.OpenGL.Shaders;

namespace Amber.Renderer.OpenGL;

internal class Layer : ILayer, IDisposable
{
    static int NextIndex = 1;
	static readonly Dictionary<string, BaseShader> cachedShaders = [];
	readonly SpriteFactory? spriteFactory;
	readonly ColoredRectFactory? coloredRectFactory;
	readonly List<BaseShader> shaders = [];
	readonly Dictionary<string, RenderBuffer> renderBuffers = [];
	readonly State state;
	bool disposed = false;

	public bool Visible
    {
        get;
        set;
    }

    public LayerType Type {  get; }

    public LayerConfig Config { get; }

	public int Index { get; }

	public ISpriteFactory? SpriteFactory => spriteFactory;

	public IColoredRectFactory? ColoredRectFactory => coloredRectFactory;

	// TODO
	public PositionTransformation? PositionTransformation { get; }

	// TODO
	public SizeTransformation? SizeTransformation { get; }

	public Layer(State state, LayerType type, LayerConfig config)
    {
        Index = NextIndex++;
        Type = type;
		Config = config;
        Visible = true;
        this.state = state;

        if (type.UsesTextures() && Config.Texture == null)
            throw new AmberException(ExceptionScope.Application, "Layer supports textures but has no texture.");

		if (type.UsesPalette() && Config.Palette == null)
			throw new AmberException(ExceptionScope.Application, "Layer supports palettes but has no palette.");

		void AddColor2DBuffer()
		{
			var colorShader = EnsureShader(() => new ColorShader(state));
			shaders.Add(colorShader);
			renderBuffers.Add(typeof(ColoredRect).Name, new RenderBuffer(state, colorShader, config.LayerFeatures));
		}

		void AddTexture2DBuffer()
		{
			var textureShader = EnsureShader(() => new Texture2DShader(state));
			shaders.Add(textureShader);
			renderBuffers.Add(typeof(Sprite).Name, new RenderBuffer(state, textureShader, config.LayerFeatures));
		}

        switch (type)
        {
			case LayerType.Color2D:
				AddColor2DBuffer();
				coloredRectFactory = new(this);
				break;
			case LayerType.ColorAndTexture2D:
            {
				AddColor2DBuffer();
				AddTexture2DBuffer();
				coloredRectFactory = new(this);
				spriteFactory = new(this);
				break;
			}			
			case LayerType.Texture2D:
			{
				AddTexture2DBuffer();
				spriteFactory = new(this);
				break;
			}
			default:
			{
				throw new NotSupportedException($"Layer type {type} is not supported.");
			}
		}
	}

	public RenderBuffer? GetBufferForDrawable<TDrawable>()
		where TDrawable : Drawable
	{
		return renderBuffers.GetValueOrDefault(typeof(TDrawable).Name);
	}

    private static TShader EnsureShader<TShader>(Func<TShader> factory)
        where TShader : BaseShader
    {
        var typeName = typeof(TShader).Name;
        return (TShader)cachedShaders.GetOrAdd(typeName, factory);
    }

    public void Render(IRenderer renderer)
    {
        if (!Visible)
            return;

		void SetupTextureShader(ITextureShader textureShader, bool usePalette)
		{
			textureShader.SetTexture(0);
			state.Gl.ActiveTexture(GLEnum.Texture0);
			Config.Texture!.Use();
			textureShader.UsePalette(usePalette);
			textureShader.SetAtlasSize((uint)Config.Texture.Size.Width, (uint)Config.Texture.Size.Height);
			textureShader.AllowTransparency(Config.LayerFeatures.HasFlag(LayerFeatures.Transparency));
		}

		foreach (var shader in shaders)
		{
			shader.SetZ(Config.BaseZ);
            shader.UpdateMatrices(state);

			if (shader is IPaletteShader paletteShader)
			{
				if (shader is ITextureShader textureShader)
					SetupTextureShader(textureShader, true);

				paletteShader.SetPalette(1);
				state.Gl.ActiveTexture(GLEnum.Texture1);
				Config.Palette!.Use();
				paletteShader.SetPaletteSize(Config.Palette.Size.Width);
				paletteShader.SetPaletteCount(Config.Palette.Size.Height);
			}
			else if (shader is ITextureShader textureShader)
			{
				SetupTextureShader(textureShader, false);
			}

			// TODO ...
		}

		if (Config.Texture != null)
		{
			state.Gl.ActiveTexture(GLEnum.Texture0);
			Config.Texture.Use();
		}

		if (Config.Palette != null)
		{
			state.Gl.ActiveTexture(GLEnum.Texture1);
			Config.Palette.Use();
		}

		foreach (var buffer in renderBuffers)
		{
			state.EnableBlending(buffer.Value.NeedsBlending);
			buffer.Value.Render();
		}
	}

    public void Dispose()
    {
        if (!disposed)
        {
			foreach (var buffer in renderBuffers)
				buffer.Value.Dispose();
			renderBuffers.Clear();

            Visible = false;

            disposed = true;
        }
    }
}

internal class LayerFactory(State state) : ILayerFactory
{
	public ILayer Create(LayerType type, LayerConfig config)
	{
		return new Layer(state, type, config);
	}
}
