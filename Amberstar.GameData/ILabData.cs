using Amber.Assets.Common;

namespace Amberstar.GameData;

// TODO
[Flags]
public enum Tile3DFlags : uint
{
	/// <summary>
	/// If set animation goes back and forth, otherwise only in one direction.
	/// </summary>
	WaveAnimation =		0x_0000_0001,
	BlockSight =		0x_0000_0002,
	BlockAllMovement =	0x_0000_0080,
	AllowWalk1 =        0x_0000_0100,
	AllowWalk2 =		0x_0000_0200,
	AllowWalk3 =	    0x_0000_0400,
	AllowWalk4 =        0x_0000_0800,
	AllowWalk5 =		0x_0000_1000,
	AllowWalk6 =		0x_0000_2000,
	AllowWalk7 =		0x_0000_4000,
	// TODO ...

}

public interface ILabBlock
{
	/// <summary>
	/// If underlay, <see cref="SecondaryIndex"/> is not used.
	/// If overlay, <see cref="SecondaryIndex"/> gives the underlay.
	/// </summary>
	int PrimaryIndex { get; }
	int SecondaryIndex { get; }
	int MinimapColorIndex { get; }
	Tile3DFlags Flags { get; }
}

public interface ILabData
{
	// TODO
}