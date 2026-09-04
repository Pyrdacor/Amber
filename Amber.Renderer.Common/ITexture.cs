using Amber.Common;
using Amber.Assets.Common;

namespace Amber.Renderer.Common;

public interface ITexture
{
    /// <summary>
    /// Size of the texture, in pixels.
    /// </summary>
    Size Size { get; }

    /// <summary>
    /// Binds the texture for use in subsequent draw calls.
    /// </summary>
    void Use();
}

public interface ITextureAtlas : ITexture
{
	/// <summary>
	/// Returns the pixel offset of the sub-texture with the given index within the atlas.
	/// </summary>
	Position GetOffset(int index);
}

public interface ITextureFactory
{
	/// <summary>
	/// Creates a texture from a single graphic.
	/// </summary>
	ITexture Create(IGraphic graphic, int numMipMapLevels = 0);
	/// <summary>
	/// Creates a texture atlas by packing all given graphics into one texture, keyed by the given index.
	/// </summary>
	ITextureAtlas CreateAtlas(Dictionary<int, IGraphic> graphics, int numMipMapLevels = 0);
}
