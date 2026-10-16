using Amber.Assets.Common;
using Amber.Common;
using Amber.Renderer.Common;

namespace Amber.Renderer.Headless;

internal class Texture(Size size) : ITexture
{
	public Size Size { get; } = size;

	public void Use()
	{
		// nothing to bind
	}
}

/// <summary>
/// All graphics are placed in one row. So every graphic
/// gets a unique offset which is all the game needs.
/// </summary>
internal sealed class TextureAtlas : Texture, ITextureAtlas
{
	readonly Dictionary<int, Position> offsets;

	private TextureAtlas(Size size, Dictionary<int, Position> offsets)
		: base(size)
	{
		this.offsets = offsets;
	}

	public static TextureAtlas Create(Dictionary<int, IGraphic> graphics)
	{
		var offsets = new Dictionary<int, Position>(graphics.Count);
		int x = 0;
		int height = 0;

		foreach (var (index, graphic) in graphics.OrderBy(g => g.Key))
		{
			offsets.Add(index, new(x, 0));
			x += graphic.Width;
			height = Math.Max(height, graphic.Height);
		}

		return new(new Size(x, height), offsets);
	}

	public Position GetOffset(int index) => offsets[index];
}

internal sealed class TextureFactory : ITextureFactory
{
	public ITexture Create(IGraphic graphic, int numMipMapLevels = 0) => new Texture(new Size(graphic.Width, graphic.Height));

	public ITextureAtlas CreateAtlas(Dictionary<int, IGraphic> graphics, int numMipMapLevels = 0) => TextureAtlas.Create(graphics);
}
