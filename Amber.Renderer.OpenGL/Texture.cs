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

	public uint Index { get; private set; }
	public Size Size { get; private protected init; }

	protected Texture(State state)
    {
        this.state = state;
        Index = state.Gl.GenTexture();
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

internal class TextureAtlas : Texture, ITextureAtlas
{
	// key = max height of category
	class TextureCategorySorter : IComparer<KeyValuePair<uint, List<int>>>
	{
		public int Compare(KeyValuePair<uint, List<int>> x, KeyValuePair<uint, List<int>> y)
		{
			return x.Key.CompareTo(y.Key);
		}
	}

	readonly Dictionary<int, Position> textureOffsets = [];

	public TextureAtlas(State state, Dictionary<int, IGraphic> graphics, int numMipMapLevels = 0)
		: base(state)
	{
		if (graphics.Count == 0)
			return;

		var formats = graphics.Select(g => g.Value.Format);
		var graphicFormat = formats.First();

		if (formats.Skip(1).Any(f => f != graphicFormat))
			throw new AmberException(ExceptionScope.Application, "All graphics for a texture atlas must use the same format.");		

		// sort textures by similar heights (16-pixel bands)
		// heights of items are < key * 16
		// value = list of texture indices
		Dictionary<uint, List<int>> textureCategories = [];
		Dictionary<uint, uint> textureCategoryMinValues = [];
		Dictionary<uint, uint> textureCategoryMaxValues = [];
		Dictionary<uint, uint> textureCategoryTotalWidth = [];

		foreach (var texture in graphics)
		{
			uint category = (uint)texture.Value.Height / 16u;

			if (!textureCategories.ContainsKey(category))
			{
				textureCategories.Add(category, []);
				textureCategoryMinValues.Add(category, (uint)texture.Value.Height);
				textureCategoryMaxValues.Add(category, (uint)texture.Value.Height);
				textureCategoryTotalWidth.Add(category, (uint)texture.Value.Width);
			}
			else
			{
				if (texture.Value.Height < textureCategoryMinValues[category])
					textureCategoryMinValues[category] = (uint)texture.Value.Height;
				if (texture.Value.Height > textureCategoryMaxValues[category])
					textureCategoryMaxValues[category] = (uint)texture.Value.Height;
				textureCategoryTotalWidth[category] += (uint)texture.Value.Width;
			}

			textureCategories[category].Add(texture.Key);
		}

		var filteredTextureCategories = new List<KeyValuePair<uint, List<int>>>();

		foreach (var category in textureCategories)
		{
			if (textureCategories[category.Key].Count == 0)
				continue; // was merged with lower category

			// merge categories with minimal differences
			if (textureCategoryMinValues[category.Key] >= category.Key * 16 + 8 &&
				textureCategories.ContainsKey(category.Key + 1) &&
				textureCategoryMaxValues[category.Key + 1] <= (category.Key + 1) * 16 + 8)
			{
				textureCategories[category.Key].AddRange(textureCategories[category.Key + 1]);
				textureCategoryMaxValues[category.Key] = Math.Max(textureCategoryMaxValues[category.Key], textureCategoryMaxValues[category.Key + 1]);
				textureCategories[category.Key + 1].Clear();
			}

			filteredTextureCategories.Add(new KeyValuePair<uint, List<int>>(textureCategoryMaxValues[category.Key], textureCategories[category.Key]));
		}

		filteredTextureCategories.Sort(new TextureCategorySorter());

		// now we have a sorted category list with all texture indices

		uint maxWidth = Math.Max(512u, textureCategoryMaxValues.Max(m => m.Value));
		uint width = 0u;
		uint height = 0u;
		uint xOffset = 0u;
		uint yOffset = 0u;

		// create texture offsets
		foreach (var category in filteredTextureCategories)
		{
			foreach (var textureIndex in category.Value)
			{
				var texture = graphics[textureIndex];

				if (xOffset + texture.Width <= maxWidth)
				{
					if (yOffset + texture.Height > height)
						height = yOffset + (uint)texture.Height;

					textureOffsets.Add(textureIndex, new Position((int)xOffset, (int)yOffset));

					xOffset += (uint)texture.Width;

					if (xOffset > width)
						width = xOffset;
				}
				else
				{
					xOffset = 0;
					yOffset = height;

					height = yOffset + (uint)texture.Height;

					textureOffsets.Add(textureIndex, new Position((int)xOffset, (int)yOffset));

					xOffset += (uint)texture.Width;

					if (xOffset > width)
						width = xOffset;
				}
			}

			if (xOffset > maxWidth - 320) // we do not expect textures with a width greater than 320
			{
				xOffset = 0;
				yOffset = height;
			}
		}

		// create texture
		Size = new((int)width, (int)height);
		var atlasGraphic = new Graphic(Size.Width, Size.Height, graphicFormat);

		foreach (var offset in textureOffsets)
		{
			var subGraphic = graphics[offset.Key];

			atlasGraphic.AddOverlay(offset.Value.X, offset.Value.Y, subGraphic);
		}

		Create(graphicFormat, atlasGraphic.GetData(), numMipMapLevels);
	}

	public Position GetOffset(int index) => textureOffsets[index];
}

internal class TextureFactory(State state) : ITextureFactory
{
	public ITexture Create(IGraphic graphic, int numMipMapLevels = 0)
	{
        return new Texture(state, graphic, numMipMapLevels);
	}

	public ITextureAtlas CreateAtlas(Dictionary<int, IGraphic> graphics, int numMipMapLevels = 0)
    {
        return new TextureAtlas(state, graphics, numMipMapLevels);
    }
}
