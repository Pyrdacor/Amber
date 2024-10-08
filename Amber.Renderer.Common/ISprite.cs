﻿using Amber.Common;

namespace Amber.Renderer;

public interface ISprite : ILayeredDrawable, ISizedDrawable
{
	Position TextureOffset { get; set; }
	Size? TextureSize { get; set; }
	byte PaletteIndex { get; set; }
	byte? MaskColorIndex { get; set; }
	byte? TransparentColorIndex { get; set; }
	bool MirrorX { get; set; }
	bool Opaque { get; set; }
}

public interface IAnimatedSprite : ISprite
{
	int CurrentFrameIndex { get; set; }
	int FrameCount { get; set; }
}

public interface ISpriteFactory
{
	ISprite Create();
	IAnimatedSprite CreateAnimated();
}
