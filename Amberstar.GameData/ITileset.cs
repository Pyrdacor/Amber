using Amber.Assets.Common;

namespace Amberstar.GameData;

// TODO
[Flags]
public enum TileFlags : uint
{
	/// <summary>
	/// If set animation goes back and forth, otherwise only in one direction.
	/// </summary>
	WaveAnimation =		0x_0000_0001,
	BlockSight =		0x_0000_0002,
	/// <summary>
	/// If set and the player enters the tile, the next tile graphic is
	/// displayed instead which usually is the tile occupied by a sitting
	/// or sleeping player.
	/// </summary>
	ChairOrBed =		0x_0000_0008,
	/// <summary>
	/// Animation starts at a random frame, pauses for a random duration
	/// and starts again from a random frame.
	/// </summary>
	RandomAnimation =	0x_0000_0010,
	/// <summary>
	/// If set this tile should be drawn over the player.
	/// </summary>
	Foreground =		0x_0000_0040,
	BlockAllMovement =	0x_0000_0080,
	AllowWalk =			0x_0000_0100,
	AllowHorse =		0x_0000_0200,
	AllowRaft =			0x_0000_0400,
	AllowShip =			0x_0000_0800,
	AllowDisk =			0x_0000_1000,
	AllowEagle =		0x_0000_2000,
	AllowFly =			0x_0000_4000, // cheat mode
	PartyInvisible =	0x_0000_8000,
	// TODO ...

}

public interface ITile
{
	int FrameCount { get; }
	int ImageIndex { get; }
	int MinimapColorIndex { get; }
	TileFlags Flags { get; }
}

public interface ITileset
{
	int PlayerSpriteIndex { get; }
	IReadOnlyList<ITile> Tiles { get; }
	IGraphic Palette { get; }
	IReadOnlyList<IGraphic> Graphics { get; }

	public const int TileCount = 250;
}