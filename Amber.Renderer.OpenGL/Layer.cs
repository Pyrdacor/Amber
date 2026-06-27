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
using Amber.Renderer.Common;
using Amber.Renderer.OpenGL.Drawables;
using Amber.Renderer.OpenGL.Shaders;

namespace Amber.Renderer.OpenGL;

internal class Layer : ILayer, IDisposable
{
    static int NextIndex = 1;
	static readonly Dictionary<string, BaseShader> cachedShaders = [];
	readonly SpriteFactory? spriteFactory;
    private protected readonly Surface3DFactory? surface3DFactory;
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

    public LayerConfig Config { get; set; }

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

        ColorShader AddColor2DBuffer()
		{
			var colorShader = EnsureShader(() => new ColorShader(state));
			shaders.Add(colorShader);
			renderBuffers.Add(typeof(ColoredRect).Name, new RenderBuffer(state, colorShader, config.LayerFeatures));
			return colorShader;
		}

        Texture2DShader AddTexture2DBuffer()
		{
			var textureShader = EnsureShader(() => new Texture2DShader(state));
			shaders.Add(textureShader);
			renderBuffers.Add(typeof(Sprite).Name, new RenderBuffer(state, textureShader, config.LayerFeatures));
			return textureShader;
        }

        Texture3DShader AddTexture3DBuffer()
        {
            var textureShader = EnsureShader(() => new Texture3DShader(state));
            shaders.Add(textureShader);
            renderBuffers.Add(typeof(Surface3D).Name, new RenderBuffer(state, textureShader, config.LayerFeatures));
            return textureShader;
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
			case LayerType.Images:
			{
                AddTexture2DBuffer();
                spriteFactory = new(this);
                break;
			}
			case LayerType.Texture3D:
			{
				AddTexture3DBuffer();
				surface3DFactory = new(this);
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
            textureShader.AllowAlpha(Config.LayerFeatures.HasFlag(LayerFeatures.Alpha));
        }

		if (renderer.Camera is Camera3D camera && (Type == LayerType.Texture3D || Type == LayerType.Billboard3D))
			state.PushModelViewMatrix(camera.ViewMatrix);

		foreach (var shader in shaders)
		{
			shader.SetZ(Config.BaseZ);
            shader.UpdateMatrices(state);

			if (shader is IPaletteShader paletteShader)
			{
				if (shader is ITextureShader textureShader)
					SetupTextureShader(textureShader, Config.Palette != null);

				if (Config.Palette != null)
				{
					paletteShader.SetPalette(1);
					state.Gl.ActiveTexture(GLEnum.Texture1);
					Config.Palette!.Use();
					paletteShader.SetPaletteSize(Config.Palette.Size.Width);
					paletteShader.SetPaletteCount(Config.Palette.Size.Height);
				}
			}
			else if (shader is ITextureShader textureShader)
			{
				SetupTextureShader(textureShader, false);
			}

			if (shader is Texture3DShader texture3DShader)
			{
				var layer3D = this as ILayer3D;
				bool allowColorReplacement = Config.LayerFeatures.HasFlag(LayerFeatures.ColorReplacement);
                bool allowSkyColorReplacement = Config.LayerFeatures.HasFlag(LayerFeatures.SkyColorReplacemnet);
                bool allowFog = Config.LayerFeatures.HasFlag(LayerFeatures.Fog);
                bool allowFading = Config.LayerFeatures.HasFlag(LayerFeatures.Fading);
                bool allowTinting = Config.LayerFeatures.HasFlag(LayerFeatures.Tinting);

				if (allowColorReplacement && layer3D?.ReplacementColors != null)
				{
					texture3DShader.SetReplacementColors(layer3D.ReplacementColors);
					texture3DShader.AllowColorReplacement(true);
				}
				else
				{
                    texture3DShader.AllowColorReplacement(false);
                }

                texture3DShader.SetLightIntensity(layer3D?.LightIntensity ?? 1.0f);
                
				if (allowSkyColorReplacement && layer3D != null)
				{
                    texture3DShader.SetSkyColorIndex(layer3D.SkyColorIndex ?? -1);
                    texture3DShader.SetSkyReplacementColor(layer3D.SkyReplacementColor ?? Color.White);
                }
				else
				{
                    texture3DShader.SetSkyColorIndex(-1);
                    texture3DShader.SetSkyReplacementColor(Color.White);
                }

				if (allowFog && layer3D != null)
				{
					texture3DShader.SetFogColor(layer3D?.FogColor ?? Color.Black);
					texture3DShader.SetFogStartDistance(layer3D?.FogStartDistance ?? 10.0f);
					texture3DShader.SetFogEndDistance(layer3D?.FogEndDistance ?? 15.0f);
					texture3DShader.EnableFog(true);
				}
				else
				{
                    texture3DShader.EnableFog(false);
                }

				if (allowFading && layer3D != null)
					texture3DShader.SetFadeFactor(layer3D.FadeFactor);
				else
					texture3DShader.SetFadeFactor(1.0f);

				if (allowTinting && layer3D?.TintColor != null)
					texture3DShader.SetTintColor(layer3D.TintColor.Value);
				else
                    texture3DShader.SetTintColor(Color.White);
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

        if (renderer.Camera is Camera3D && (Type == LayerType.Texture3D || Type == LayerType.Billboard3D))
            state.PopModelViewMatrix();
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

internal class Layer3D(State state, LayerType type, LayerConfig config) : Layer(state, type, config), ILayer3D, IDisposable
{
    public Color? FogColor { get; set; }
	public float FogStartDistance { get; set; } = 10.0f;
	public float FogEndDistance { get; set; } = 15.0f;
    public Color[]? ReplacementColors { get; set; }
    public Color? TintColor { get; set; }
    public int? SkyColorIndex { get; set; }
    public Color? SkyReplacementColor { get; set; }
	public float LightIntensity { get; set; } = 1.0f;
	public float FadeFactor { get; set; } = 1.0f;
	public ISurface3DFactory? Surface3DFactory => surface3DFactory;
}

internal class LayerFactory(State state) : ILayerFactory
{
	public ILayer Create(LayerType type, LayerConfig config)
	{
        return type switch
        {
            LayerType.Texture3D or LayerType.Billboard3D => new Layer3D(state, type, config),
            _ => new Layer(state, type, config),
        };
    }
}
