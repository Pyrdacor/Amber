using Amber.Common;
using Amber.Serialization;
namespace Amberstar.GameData.Legacy;

internal class Map2D : Map, IMap2D
{
	private Map2D(MapHeader header, MapNPC[] npcs, PositionList[] npcPositions, Tile2D[] tiles)
		: base(header, npcs, npcPositions)
	{
		Tiles = tiles;
	}

	public Tile2D[] Tiles { get; }

	public static unsafe Map2D Load(int id, MapHeader header, IDataReader reader)
	{
		if (header.MapType != MapType.Map2D)
			throw new AmberException(ExceptionScope.Data, $"Map {id} is not a 2D map.");

		int mapSize = header.Width * header.Height;
		var underlay = reader.ReadBytes(mapSize);
		var overlay = reader.ReadBytes(mapSize);
		var events = reader.ReadBytes(mapSize);

		var tiles = Enumerable.Range(0, mapSize).Select(index => new Tile2D
		{
			Underlay = underlay[index],
			Overlay = overlay[index],
			Event = events[index]
		}).ToArray();

		word* index = header.NPCData;
		byte* icon = header.NPCIcon;
		byte* move = header.NPCMove;
		byte* flags = header.NPCFlags;
		byte* day = header.NPCDay;
		byte* month = header.NPCMonth;

		var npcs = new MapNPC[IMap.NPCCount];
		var positionCountPerNpcs = new int[IMap.NPCCount];

		for (int i = 0; i < npcs.Length; i++)
		{
			var npc = new MapNPC();

			npc.Index = *index++;
			npc.Icon = *icon++;
			var npcFlags = *flags++;
			npc.TravelType = *move++;
			npc.Type = (npcFlags & 0x1) != 0 ? MapNPCType.Monster
				: (npcFlags & 0x10) != 0 ? MapNPCType.Popup
				: MapNPCType.Person;
			npc.WalkType = npc.Type == MapNPCType.Monster
				? ((npcFlags & 0x04) != 0 ? MapNPCWalkType.Chase : MapNPCWalkType.Stationary)
				: ((npcFlags & 0x02) != 0 ? MapNPCWalkType.Random : MapNPCWalkType.Path);
			bool hasSpawnDate = (npcFlags & 0x08) != 0;
			var spawnDay = *day++;
			var spawnMonth = *month++;
			npc.Day = hasSpawnDate ? spawnDay : (byte)0xff;
			npc.Month = hasSpawnDate ? spawnMonth : (byte)0xff;		

			npcs[i] = npc;

			positionCountPerNpcs[i] = npc.Index == 0 ? 0 : npc.WalkType == MapNPCWalkType.Path ? 288 : 1;
		}

		int totalNPCPositions = positionCountPerNpcs.Sum();
		var x = reader.ReadBytes(totalNPCPositions);
		var y = reader.ReadBytes(totalNPCPositions);
		var npcPositions = new PositionList[IMap.NPCCount];
		int offset = 0;

		for (int i = 0; i < npcs.Length; i++)
		{
			var positions = new Position[positionCountPerNpcs[i]];

			for (int p = 0; p < positions.Length; offset++, p++)
			{
				positions[p] = new Position(x[offset], y[offset]);
			}

			npcPositions[i] = positions;
		}

		return new Map2D(header, npcs, npcPositions, tiles);
	}
}
