using Amber.Common;

namespace Amber.Renderer.Common;

public interface ISprite : ILayeredDrawable, ISizedDrawable
{
	/// <summary>
	/// Pixel offset of the sprite's graphic within its texture (atlas).
	/// </summary>
	Position TextureOffset { get; set; }
	/// <summary>
	/// Size of the sprite's graphic within its texture, if different from <see cref="ISizedDrawable.Size"/>.
	/// </summary>
	Size? TextureSize { get; set; }
	byte PaletteIndex { get; set; }
	/// <summary>
	/// Palette color index that masks out (skips drawing) matching pixels, if any.
	/// </summary>
	byte? MaskColorIndex { get; set; }
	/// <summary>
	/// Palette color index that is treated as fully transparent, if any.
	/// </summary>
	byte? TransparentColorIndex { get; set; }
	/// <summary>
	/// Mirrors the sprite horizontally.
	/// </summary>
	bool MirrorX { get; set; }
	bool Opaque { get; set; }
}

public interface IAlphaSprite : ISprite
{
    byte Alpha { get; set; }
}

public interface IAnimatedSprite : ISprite
{
	int CurrentFrameIndex { get; set; }
	int FrameCount { get; set; }
}

public interface ISpriteFactory
{
	/// <summary>
	/// Creates a plain sprite.
	/// </summary>
	ISprite Create();
	/// <summary>
	/// Creates a sprite that cycles through multiple frames.
	/// </summary>
	IAnimatedSprite CreateAnimated();
	/// <summary>
	/// Creates a sprite with an adjustable alpha value.
	/// </summary>
	IAlphaSprite CreateWithAlpha();
}
