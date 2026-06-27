using Amber.IO.Common.Serialization;

namespace AmberIsland.GameData;

[Flags]
public enum ItemFlags : byte
{
    Important   = 1 << 0,
}

public enum ItemType : byte
{
    Equipment,
    Loot,
    Potion,
    TextItem,
    Key,
    Essence
}

public readonly record struct Item
(
    // Byte-sized
    ItemType Type,
    ItemFlags Flags,
    // Dword-sized
    uint SellPrice
)
{
    public void Write(IDataWriter writer)
    {
        // Byte-sized
        writer.Write((byte)Type);
        writer.Write((byte)Flags);

        // Dword-sized
        writer.Write(SellPrice);        
        
    }

    public static Item Read(IDataReader reader)
    {
        // Byte-sized
        var type = (ItemType)reader.ReadByte();
        var flags = (ItemFlags)reader.ReadByte();

        // Dword-sized
        var sellPrice = reader.ReadDword();

        return new
        (
            // Byte-sized
            type,
            flags,
            // Dword-sized
            sellPrice
        );
    }
}

public enum EquipmentType
{
    Amulet,
    Headgear,
    Weapon,
    Armor,
    Shield,
    Ring,
    Footgear,
    Ammunition,
}

public readonly record struct Equipment
(
    Item Item,
    // Byte-sized
    EquipmentType EquipmentType,
    byte MinLevel,
    Classes Classes,
    Element Element,
    // Dword-sized
    uint Damage,
    uint Defense,
    uint HitPoints,
    uint SpellPoints
)
{
    public void Write(IDataWriter writer)
    {
        Item.Write(writer);

        // Byte-sized
        writer.Write((byte)EquipmentType);
        writer.Write(MinLevel);
        writer.Write((byte)Classes);
        writer.Write((byte)Element);

        // Dword-sized
        writer.Write(Damage);
        writer.Write(Defense);
        writer.Write(HitPoints);
        writer.Write(SpellPoints);

    }

    public static Equipment Read(IDataReader reader)
    {
        var item = Item.Read(reader);

        // Byte-sized
        var equipmentType = (EquipmentType)reader.ReadByte();
        var minLevel = reader.ReadByte();
        var classes = (Classes)reader.ReadByte();
        var element = (Element)reader.ReadByte();

        // Dword-sized
        var damage = reader.ReadDword();
        var defense = reader.ReadDword();
        var hitPoints = reader.ReadDword();
        var spellPoints = reader.ReadDword();

        return new
        (
            item,
            // Byte-sized
            equipmentType,
            minLevel,
            classes,
            element,
            // Dword-sized
            damage,
            defense,
            hitPoints,
            spellPoints
        );
    }
}