using Amber.IO.Common.Serialization;
using Amber.IO.FileFormats.Compression;
using Amber.IO.FileFormats.Serialization;

namespace AmberIsland.GameData;

public enum MapMonsterSpawnType : byte
{
    Solo,
    Group, // Tries to spawn near monsters of same kind
    JoinOthers, // Tries to spawn near monsters regardless of kind
    SurroundBoss, // Each monster can specify a boss monster, only spawns when boss is on map
    Hide, // Tries to spawn far away from player
    Random // Picks any of the above (boss only if it has a boss set and this boss can spawn on the map)
}

public readonly record struct MapMonster
(
    MapMonsterSpawnType SpawnType,
    TravelType TravelType,
    ushort MonsterIndex,
    ushort MinAmount,
    ushort MaxAmount,
    ushort MinRespawnDelay, // in real time seconds
    ushort MaxRespawnDelay // in real time seconds (max ~18,2 hours)
)
{
    public void Write(IDataWriter writer)
    {
        writer.Write((byte)SpawnType);
        writer.Write((byte)TravelType);
        writer.Write(MonsterIndex);
        writer.Write(MinAmount);
        writer.Write(MaxAmount);
        writer.Write(MinRespawnDelay);
        writer.Write(MaxRespawnDelay);
    }

    public static MapMonster Read(IDataReader reader)
    {
        var spawnType = (MapMonsterSpawnType)reader.ReadByte();
        var travelType = (TravelType)reader.ReadByte();
        var monsterIndex = reader.ReadWord();
        var minAmount = reader.ReadWord();
        var maxAmount = reader.ReadWord();
        var minRespawnDelay = reader.ReadWord();
        var maxRespawnDelay = reader.ReadWord();

        return new(spawnType, travelType, monsterIndex, minAmount, maxAmount, minRespawnDelay, maxRespawnDelay);
    }
}

public readonly record struct MapNPC
(
    // TODO ...
)
{
    public void Write(IDataWriter writer)
    {
        // TODO
        throw new NotImplementedException();
    }

    public static MapNPC Read(IDataReader reader)
    {
        // TODO
        throw new NotImplementedException();
    }
}

public record Map
(
    byte Width,
    byte Height,
    ushort BackgroundTilesetIndex,
    ushort ObjectTilesetIndex,
    ushort ForegroundTilesetIndex,
    byte[] BackgroundLayer,
    byte[] ObjectLayer,
    byte[] ForegroundLayer,
    MapEventTrigger[] EventTriggers,
    MapEventCondition[] EventConditions,
    MapEventAction[] EventActions,
    MapEvent[] Events,
    MapMonster[] Monsters,
    MapNPC[] NPCs
)
{
    public void Write(IDataWriter writer)
    {
        var uncompressedWriter = new DataWriter();

        uncompressedWriter.Write(Width);
        uncompressedWriter.Write(Height);
        uncompressedWriter.Write(BackgroundTilesetIndex);
        uncompressedWriter.Write(ObjectTilesetIndex);
        uncompressedWriter.Write(ForegroundTilesetIndex);

        uncompressedWriter.Write(BackgroundLayer);
        uncompressedWriter.Write(ObjectLayer);
        uncompressedWriter.Write(ForegroundLayer);

        uncompressedWriter.Write((ushort)EventTriggers.Length);
        uncompressedWriter.Write((ushort)EventConditions.Length);
        uncompressedWriter.Write((ushort)EventActions.Length);
        uncompressedWriter.Write((ushort)Events.Length);
        uncompressedWriter.Write((byte)Monsters.Length);
        uncompressedWriter.Write((byte)NPCs.Length);

        foreach (var trigger in EventTriggers)
            trigger.Write(uncompressedWriter);

        foreach (var condition in EventConditions)
            condition.Write(uncompressedWriter);

        foreach (var action in EventActions)
            action.Write(uncompressedWriter);

        foreach (var evt in Events)
            evt.Write(uncompressedWriter);

        foreach (var monster in Monsters)
            monster.Write(uncompressedWriter);

        foreach (var npc in NPCs)
            npc.Write(uncompressedWriter);

        writer.Write(Deflate.Compress(uncompressedWriter));
    }

    public static Map Read(IDataReader reader)
    {
        reader = Deflate.Decompress(reader);

        byte width = reader.ReadByte();
        byte height = reader.ReadByte();
        ushort backgroundTilesetIndex = reader.ReadWord();
        ushort objectTilesetIndex = reader.ReadWord();
        ushort foregroundTilesetIndex = reader.ReadWord();

        int tileCount = width * height;
        var backgroundLayer = reader.ReadBytes(tileCount);
        var objectLayer = reader.ReadBytes(tileCount);
        var foregroundLayer = reader.ReadBytes(tileCount);

        int numEventTriggers = reader.ReadWord();
        int numEventConditions = reader.ReadWord();
        int numEventActions = reader.ReadWord();
        int numEvents = reader.ReadWord();
        int numMonsters = reader.ReadByte();
        int numNPCs = reader.ReadByte();

        var eventTriggers = new MapEventTrigger[numEventTriggers];
        var eventConditions = new MapEventCondition[numEventConditions];
        var eventActions = new MapEventAction[numEventActions];
        var events = new MapEvent[numEvents];
        var monsters = new MapMonster[numMonsters];
        var npcs = new MapNPC[numNPCs];

        for (int i = 0; i < eventTriggers.Length; i++)
            eventTriggers[i] = MapEventTrigger.Read(reader);

        for (int i = 0; i < eventConditions.Length; i++)
            eventConditions[i] = MapEventCondition.Read(reader);

        for (int i = 0; i < eventActions.Length; i++)
            eventActions[i] = MapEventAction.Read(reader);

        for (int i = 0; i < events.Length; i++)
            events[i] = MapEvent.Read(reader);

        for (int i = 0; i < monsters.Length; i++)
            monsters[i] = MapMonster.Read(reader);

        for (int i = 0; i < npcs.Length; i++)
            npcs[i] = MapNPC.Read(reader);

        return new(width, height, backgroundTilesetIndex, objectTilesetIndex, foregroundTilesetIndex,
            backgroundLayer, objectLayer, foregroundLayer, eventTriggers, eventConditions, eventActions,
            events, monsters, npcs);
    }
}
