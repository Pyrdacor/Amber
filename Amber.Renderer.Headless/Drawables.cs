using System.Numerics;
using Amber.Common;
using Amber.Renderer.Common;

namespace Amber.Renderer.Headless;

internal class SizedDrawable(ILayer layer) : ILayeredDrawable, ISizedDrawable
{
	public bool Visible { get; set; } = false;
	public ILayer Layer { get; } = layer;
	public Position Position { get; set; } = new();
	public Size Size { get; set; } = new();
	public int BaseLineOffset { get; set; } = 0;
	public Rect? ClipRect { get; set; } = null;
	public byte DisplayLayer { get; set; } = 0;
}

internal sealed class ColoredRect(ILayer layer) : SizedDrawable(layer), IColoredRect
{
	public Color Color { get; set; } = Color.Black;
}

internal class Sprite(ILayer layer) : SizedDrawable(layer), ISprite
{
	public Position TextureOffset { get; set; } = new();
	public Size? TextureSize { get; set; } = null;
	public byte PaletteIndex { get; set; } = 0;
	public byte? MaskColorIndex { get; set; } = null;
	public byte? TransparentColorIndex { get; set; } = null;
	public bool MirrorX { get; set; } = false;
	public bool Opaque { get; set; } = false;
}

internal sealed class AnimatedSprite(ILayer layer) : Sprite(layer), IAnimatedSprite
{
	int frameCount = 1;
	int currentFrameIndex = 0;

	public int FrameCount
	{
		get => frameCount;
		set
		{
			frameCount = Math.Max(1, value);
			currentFrameIndex %= frameCount;
		}
	}

	public int CurrentFrameIndex
	{
		get => currentFrameIndex;
		set => currentFrameIndex = value % frameCount;
	}
}

internal sealed class AlphaSprite(ILayer layer) : Sprite(layer), IAlphaSprite
{
	public byte Alpha { get; set; } = 0xff;
}

internal sealed class Surface3D(ILayer layer) : ISurface3D
{
	public bool Visible { get; set; } = false;
	public ILayer Layer { get; } = layer;
	public Vector3 Position { get; set; } = Vector3.Zero;
	public SurfaceFace Face { get; set; } = SurfaceFace.Front;
	public FloatSize Size { get; set; } = new();
	public Position TextureOffset { get; set; } = new();
	public Size? TextureSize { get; set; } = null;
	public Vector2 TextureSizeFactor { get; set; } = Vector2.One;
	public byte PaletteIndex { get; set; } = 0;
	public bool MirrorX { get; set; } = false;
	public byte Alpha { get; set; } = 0xff;
	public int CurrentFrameIndex { get; set; } = 0;
	public int FrameCount { get; set; } = 1;
}

internal sealed class SpriteFactory(ILayer layer) : ISpriteFactory
{
	public ISprite Create() => new Sprite(layer);
	public IAnimatedSprite CreateAnimated() => new AnimatedSprite(layer);
	public IAlphaSprite CreateWithAlpha() => new AlphaSprite(layer);
}

internal sealed class ColoredRectFactory(ILayer layer) : IColoredRectFactory
{
	public IColoredRect Create() => new ColoredRect(layer);
}

internal sealed class Surface3DFactory(ILayer layer) : ISurface3DFactory
{
	public ISurface3D Create() => new Surface3D(layer);
}
