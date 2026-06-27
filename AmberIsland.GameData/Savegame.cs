using Amber.IO.Common.Serialization;

namespace AmberIsland.GameData;

public readonly record struct PlayerData
(
    // Byte-sized
    byte Level,
    Race Race,
    Class Class,
    // Dword-sized
    uint Experience,
    // Collections
    ItemSlot[] InventoryItems,
    ItemSlot[] EquipmentItems
    // TODO: spells
)
{
    public const int InventorySlotCount = 32;
    public const int EquipmentSlotCount = 9;

    public void Write(IDataWriter writer)
    {
        // Byte-sized
        writer.Write(Level);
        writer.Write((byte)Race);
        writer.Write((byte)Class);

        // Dword-sized
        writer.Write(Experience);

        // Collections
        for (int i = 0; i < InventorySlotCount; i++)
            InventoryItems[i].Write(writer);
        for (int i = 0; i < EquipmentSlotCount; i++)
            EquipmentItems[i].Write(writer);
    }

    public static PlayerData Read(IDataReader reader)
    {
        // Byte-sized
        var level = reader.ReadByte();
        var race = (Race)reader.ReadByte();
        var @class = (Class)reader.ReadByte();

        // Dword-sized
        var exp = reader.ReadDword();

        // Collections
        var inventoryItems = new ItemSlot[InventorySlotCount];
        var equipmentItems = new ItemSlot[EquipmentSlotCount];

        for (int i = 0; i < InventorySlotCount; i++)
            inventoryItems[i] = ItemSlot.Read(reader);
        for (int i = 0; i < EquipmentSlotCount; i++)
            equipmentItems[i] = ItemSlot.Read(reader);

        return new
        (
            // Byte-sized
            level,
            race,
            @class,
            // Dword-sized
            exp,
            // Collections
            inventoryItems,
            equipmentItems
        );
    }
}

public readonly record struct Savegame
(
    // Byte-sized
    byte Hour,
    byte Minute,
    byte X,
    byte Y,
    Direction Direction,
    // Word-sized
    ushort MapIndex,
    // Player
    PlayerData Player
)
{
    public void Write(IDataWriter writer)
    {
        // Byte-sized
        writer.Write(Hour);
        writer.Write(Minute);
        writer.Write(X);
        writer.Write(Y);
        writer.Write((byte)Direction);

        // Word-sized
        writer.Write(MapIndex);

        // Player
        Player.Write(writer);
        
    }

    public static Savegame Read(IDataReader reader)
    {
        // Byte-sized
        var hour = reader.ReadByte();
        var minute = reader.ReadByte();
        var x = reader.ReadByte();
        var y = reader.ReadByte();
        var direction = (Direction)reader.ReadByte();        

        // Word-sized
        var mapIndex = reader.ReadWord();

        // Player
        var player = PlayerData.Read(reader);

        return new
        (            
            // Byte-sized
            hour,
            minute,
            x,
            y,
            direction,
            // Word-sized
            mapIndex,
            // Player
            player
        );
    }
}