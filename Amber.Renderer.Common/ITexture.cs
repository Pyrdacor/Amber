using Amber.Common;
using Amber.Assets.Common;

namespace Amber.Renderer.Common;

public interface ITexture
{
    Size Size { get; }

    void Use();
}

public interface ITextureAtlas : ITexture
{
	Position GetOffset(int index);
    Size GetSize(int index);
    Rect GetArea(int index);
}

public interface ITextureFactory
{
	ITexture Create(IGraphic graphic, int numMipMapLevels = 0);
	ITextureAtlas CreateAtlas(Dictionary<int, IGraphic> graphics, int numMipMapLevels = 0);
    ITextureAtlas CreateAtlas(Dictionary<int, Rect> areas, IGraphic atlasGraphic, int numMipMapLevels = 0);
}
