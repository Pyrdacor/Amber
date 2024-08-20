using Amber.Common;

namespace Amber.Renderer;

public interface ISprite : ILayeredDrawable, ISizedDrawable
{
	Position TextureOffset { get; set; }
	Size? TextureSize { get; set; }
	byte PaletteIndex { get; set; }
	byte? MaskColorIndex { get; set; }
	bool MirrorX { get; set; }
	bool Opaque { get; set; }
}

public interface ISpriteFactory
{
	ISprite Create();
}
