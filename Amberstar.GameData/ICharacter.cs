namespace Amberstar.GameData;

public interface ICharacter
{
    public const int InventorySlotCount = 12;

    CharacterType Type { get; init; }
    Gender Gender { get; init; }
    Race Race { get; init; }
    Class Class { get; init; }
    byte Level { get; set; }
    word Gold { get; set; }
    word Food { get; set; }
    Dictionary<EquipmentSlot, ItemSlot> Equipment { get; }
    ItemSlot[] Inventory { get; } // 12
    string Name { get; init; }
}    
