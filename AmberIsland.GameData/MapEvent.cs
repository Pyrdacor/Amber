using Amber.Common;
using Amber.IO.Common.Serialization;

namespace AmberIsland.GameData;

public enum MapEventTriggerType
{
    EnterTile,
    Interact,
    UseItem,
    EnterMap,    
    RealTime,
    GameTime,
    DayTime,
    TimeCycle,
    KillMonsters,
}

public enum MapEventConditionType
{
    // TODO ...
}

public enum MapEventActionType
{
    // TODO ...
}

public readonly record struct MapEventTrigger(MapEventTriggerType TriggerType, params ushort[] Parameters)
{
    public void Write(IDataWriter dataWriter)
    {
        // TODO ...
        throw new NotImplementedException();
    }

    public static MapEventTrigger Read(IDataReader reader)
    {
        var type = (MapEventTriggerType)reader.ReadByte();

        return type switch
        {
            MapEventTriggerType.EnterTile => new MapEventTrigger(type, reader.ReadByte(), reader.ReadByte()), // x, y
            MapEventTriggerType.UseItem => new MapEventTrigger(type, reader.ReadWord()), // itemId
            MapEventTriggerType.RealTime => new MapEventTrigger(type, reader.ReadWord(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte()), // year, month, day, hour, minute
            MapEventTriggerType.GameTime => new MapEventTrigger(type, reader.ReadWord(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte()), // year, month, day, hour, minute
            MapEventTriggerType.DayTime => new MapEventTrigger(type, reader.ReadByte(), reader.ReadByte()), // game hour, game minute
            MapEventTriggerType.TimeCycle => new MapEventTrigger(type, reader.ReadWord(), reader.ReadByte()), // game minutes (every x minutes), game offset minute (0 to 59)
            MapEventTriggerType.KillMonsters => new MapEventTrigger(type, reader.ReadByte(), reader.ReadWord()), // monsterId, count
            _ => new MapEventTrigger(type),
        };
    }
}

public readonly record struct MapEventCondition(MapEventConditionType ConditionType, params ushort[] Parameters)
{
    public void Write(IDataWriter dataWriter)
    {
        // TODO ...
        throw new NotImplementedException();
    }

    public static MapEventCondition Read(IDataReader reader)
    {
        var type = (MapEventConditionType)reader.ReadByte();

        return type switch
        {
            // TODO ...
            _ => new MapEventCondition(type),
        };
    }
}

public readonly record struct MapEventAction(MapEventActionType ActionType, params ushort[] Parameters)
{
    public void Write(IDataWriter dataWriter)
    {
        // TODO ...
        throw new NotImplementedException();
    }

    public static MapEventAction Read(IDataReader reader)
    {
        var type = (MapEventActionType)reader.ReadByte();

        return type switch
        {
            // TODO ...
            _ => new MapEventAction(type),
        };
    }
}

public record MapEvent
(
    ushort TriggerIndex,
    ushort[] ConditionIndices,
    ushort[] ActionIndices,
    Position[] UsingTiles
)
{
    public void Write(IDataWriter dataWriter)
    {
        dataWriter.Write((byte)ConditionIndices.Length);
        dataWriter.Write((byte)ActionIndices.Length);
        dataWriter.Write(TriggerIndex);

        foreach (var conditionIndex in ConditionIndices)
            dataWriter.Write(conditionIndex);

        foreach (var actionIndex in ActionIndices)
            dataWriter.Write(actionIndex);

        dataWriter.Write((ushort)UsingTiles.Length);

        foreach (var usingTile in UsingTiles)
        {
            dataWriter.Write((byte)usingTile.X);
            dataWriter.Write((byte)usingTile.Y);
        }
    }

    public static MapEvent Read(IDataReader reader)
    {
        ushort[] conditionIndices = new ushort[reader.ReadByte()];
        ushort[] actionIndices = new ushort[reader.ReadByte()];
        ushort triggerIndex = reader.ReadWord();

        for (int i = 0; i < conditionIndices.Length; i++)
            conditionIndices[i] = reader.ReadWord();

        for (int i = 0; i < actionIndices.Length; i++)
            actionIndices[i] = reader.ReadWord();

        Position[] usingTiles = new Position[reader.ReadWord()];

        for (int i = 0; i < conditionIndices.Length; i++)
        {
            usingTiles[i] = new(reader.ReadByte(), reader.ReadByte());
        }

        return new(triggerIndex, conditionIndices, actionIndices, usingTiles);
    }
}
