using Amber.Serialization;
using System.Runtime.InteropServices;

namespace Amberstar.GameData.Legacy;

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

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct MapHeader
{
	public byte Magic; // 0xff
	public byte Fill; // 0x00
	public word Tileset;
	public MapType MapType; // 0: 2D, 1: 3D
	public MapFlags Flags;
	public byte Music;
	public byte Width;
	public byte Height;
	public fixed byte Name[31]; // null-terminated (max 30 chars)
	public fixed byte EventData[IMap.EventCount * IEvent.DataSize];
	public fixed word NPCData[IMap.NPCCount]; // NPC index, monster group index, map text index
	public fixed byte NPCIcon[IMap.NPCCount];
	public fixed byte NPCMove[IMap.NPCCount];
	public fixed byte NPCFlags[IMap.NPCCount];
	public fixed byte NPCDay[IMap.NPCCount];
	public fixed byte NPCMonth[IMap.NPCCount];
	public word StepsPerDay; // 288
	public byte MonthsPerYear; // 12
	public byte DaysPerMonth; // 30
	public byte HoursPerDay; // 24
	public byte MinutesPerHour; // 60
	public byte MinutesPerStep; // 5
	public byte HoursPerDaytime; // 12
	public byte HoursPerNighttime; // 12
	public readonly word LabdataIndex => Tileset;
}

public abstract class Map : IMap
{
	protected readonly MapHeader header;

	public unsafe Map(MapHeader header, MapNPC[] npcs, PositionList[] npcPositions)
	{
		this.header = header;
		NPCs = npcs;
		NPCPositions = npcPositions;

		var eventReader = new FixedDataReader(header.EventData, IMap.EventCount * IEvent.DataSize);

		for (int i = 0; i < IMap.EventCount; i++)
		{
			if (eventReader.PeekByte() == 0)
			{
				// No event here
				eventReader.Position += IEvent.DataSize;
				continue;
			}

			Events.Add(Event.ReadEvent(eventReader));
		}
	}

	public MapType Type => header.MapType;

	public MapFlags Flags => header.Flags;

	public MapNPC[] NPCs { get; }

	public PositionList[] NPCPositions { get; }

	public List<IEvent> Events { get; } = [];
}