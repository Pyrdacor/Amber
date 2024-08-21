namespace Amberstar.GameData;

[Flags]
public enum MapFlags : byte
{
	Light = 0x01,
	LightChange = 0x02,
	Darkness = 0x04,
	CanUseMapViewSpell = 0x08,
	CanCamp = 0x10,
	Wilderness = 0x20, // Only used in 2D (if not set it's a 2D city)
	City = 0x40, // Only used in 3D (if not set it's a 3D dungeon)
}

public enum MapType : byte
{
	Map2D,
	Map3D
}

// If blind, all dark.
// Otherwise if Light bit is 1, just show full brightness.
// If Light bit is 0, check for LightChange bit.
// If 1, get the light radius by the hour. Use this table:
// 16,16,16,16,16,16,40,64, 9x 200, 64,64,40,16,16,16,16,16
// Any active light spell will add its effect (lsl by 3) to the radius.
// If change bit was 0 instead, check the dark bit. If this is not set,
// just do nothing (no change).
// Otherwise check travel type. Superchicken mode grants full light.
// Otherwise if no light spell is active, use full darkness.
// Otherwise use a radius of 16 + (lsl by 3 the spell effect).

public enum MapNPCType : byte
{
	Person,
	Monster,
	Popup
}

public enum MapNPCWalkType : byte
{
	Stationary,
	Random,
	Path,
	Chase
}

public struct MapNPC
{
	public word Index;	
	public byte Icon;
	public byte TravelType; // collision class
	public MapNPCType Type;	
	public MapNPCWalkType WalkType;
	public byte Day; // 0xff = always there
	public byte Month; // 0xff = always there	
}

public interface IMap : IEventProvider
{
	public int Width { get; }
	public int Height { get; }
	public MapType Type { get; }
	public MapFlags Flags { get; }
	public MapNPC[] NPCs { get; }
	public PositionList[] NPCPositions { get; }

	public const int EventCount = 254;
	public const int NPCCount = 24;
}