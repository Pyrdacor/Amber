using Amber.Common;

namespace Amber.Renderer;

public interface ISprite : IDrawable
{
	Position TextureOffset { get; set; }
	Size Size { get; set; }
	Size? TextureSize { get; set; }
}
