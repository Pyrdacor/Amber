using System.Numerics;
using Amber.Common;

namespace Amber.Renderer.Common;

public enum SurfaceFace
{
	/// <summary>
	/// Walls facing away
	/// </summary>
	Back,
	/// <summary>
	/// Walls facing right
	/// </summary>
	Right,
	/// <summary>
	/// Walls facing towards screen
	/// </summary>
	Front,
	/// <summary>
	/// Walls facing left
	/// </summary>
	Left,
	/// <summary>
	/// Ceiling
	/// </summary>
	Down,
	/// <summary>
	/// Floor
	/// </summary>
	Up
}

public interface ISurface3D : IDrawable3D
{
    SurfaceFace Face { get; set; }
    FloatSize Size { get; set; }
    Position TextureOffset { get; set; }
	Size? TextureSize { get; set; }
	/// <summary>
	/// If 1, the whole surface is covered by the texture.
	/// If 2, the surface displays the texture twice.
	/// If 0.5, the surface only shows half the texture.
	/// </summary>
	Vector2 TextureSizeFactor { get; set; }
	byte PaletteIndex { get; set; }
	bool MirrorX { get; set; }
    byte Alpha { get; set; }
	int CurrentFrameIndex { get; set; }
	int FrameCount { get; set; }
}

public interface ISurface3DFactory
{
    ISurface3D Create();
}
