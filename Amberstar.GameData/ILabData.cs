using Amber.Assets.Common;
using Amber.Common;

namespace Amberstar.GameData;

// TODO
[Flags]
public enum LabTileFlags : uint
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

public struct LabTile
{
	/// <summary>
	/// If underlay, <see cref="SecondaryLabBlockIndex"/> is not used.
	/// If overlay, <see cref="SecondaryLabBlockIndex"/> gives the underlay.
	/// </summary>
	public int PrimaryLabBlockIndex;
	public int SecondaryLabBlockIndex;
	public int MinimapColorIndex;
	public LabTileFlags Flags;
}

public enum LabBlockType : byte
{
	None,
	Wall,
	Overlay,
	Object
}

public enum BlockFacing
{
	FacingPlayer,
	LeftOfPlayer,
	RightOfPlayer
}


public enum PerspectiveLocation
{
	Forward3Left1, // Walls also use it for Forward3Left2
	Forward3Right1, // Walls also use it for Forward3Right2
	Forward3,
	Forward2Left1,
	Forward2Right1,
	Forward2,
	Forward1Left1,
	Forward1Right1,
	Forward1,
	Left1,	
	Right1,	
	PlayerLocation,
	Forward3Left2, // TODO: Test this! Walls contain 13 perspectives.
	Forward3Right2, // TODO: This as well
}


public struct PerspectiveInfo
{
	public PerspectiveLocation Location;
	public IGraphic[] Frames;
	public BlockFacing Facing;
	public Position RenderPosition;
}

public interface ILabBlock
{
	int Index { get; }
	LabBlockType Type { get; }	
	PerspectiveInfo[] Perspectives { get; }
}

public interface ILabData
{
	int CeilingIndex { get; }
	int FloorIndex { get; }
	int PaletteIndex { get; }
	ILabBlock[] LabBlocks { get; }
}
