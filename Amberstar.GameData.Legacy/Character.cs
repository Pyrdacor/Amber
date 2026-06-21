using Amber.Common;
using Amber.IO.Common.Serialization;

namespace Amberstar.GameData.Legacy;

internal class Character : ICharacter
{
    private CharacterType type;
    private Gender gender;
    private Race race;
    private Class @class;
    private string name = string.Empty;

    private readonly Dictionary<EquipmentSlot, ItemSlot> equipment = [];
    private readonly ItemSlot[] inventory = new ItemSlot[ICharacter.InventorySlotCount];

    public Character()
    {
        foreach (var slot in Enum.GetValues<EquipmentSlot>())
            equipment.Add(slot, new ItemSlot(0, null));

        for (int i = 0; i < inventory.Length; i++)
            inventory[i] = new ItemSlot(0, null);
    }

    public static void Load(Character character, IDataReader reader)
    {
        reader.Position = 0x00;

        if (reader.ReadWord() != 0xff)
            throw new AmberException(ExceptionScope.Data, "Invalid character data.");

        character.type = (CharacterType)reader.ReadByte();
        character.gender = (Gender)reader.ReadByte();
        character.race = (Race)reader.ReadByte();
        character.@class = (Class)reader.ReadByte();

        reader.Position = 0x1b;
        character.Level = reader.ReadByte();

        reader.Position = 0x90;
        character.Gold = reader.ReadWord();
        character.Food = reader.ReadWord();

        reader.Position = 0x22;
        var itemCounts = reader.ReadBytes(9 + ICharacter.InventorySlotCount);

        reader.Position = 0xf0;
        character.name = reader.ReadString(16).TrimEnd(' ', '\0');

        reader.Position = 0x132;
        var itemLoader = new ItemLoader();

        IItem? LoadOrSkipItem(int count)
        {
            if (count == 0)
            {
                reader.Position += 40; // TODO: Move to constant
                return null;
            }

            return itemLoader.ReadItem(reader);
        }
            
        var items = Enumerable.Range(0, itemCounts.Length)
            .Select(i => LoadOrSkipItem(itemCounts[i])).ToArray();

        for (int i = 0; i < 9; i++)
            character.equipment[(EquipmentSlot)i] = new ItemSlot(itemCounts[i], items[i]);

        for (int i = 0; i < ICharacter.InventorySlotCount; i++)
            character.inventory[i] = new ItemSlot(itemCounts[9 + i], items[9 + i]);
    }

    protected void CloneInto(Character character)
    {
        character.type = type;
        character.gender = gender;
        character.race = race;
        character.@class = @class;
        character.name = name;

        for (int i = 0; i < 9; i++)
            character.equipment[(EquipmentSlot)i] = new ItemSlot(equipment[(EquipmentSlot)i].Count, equipment[(EquipmentSlot)i].Item?.Clone());

        for (int i = 0; i < ICharacter.InventorySlotCount; i++)
            character.inventory[i] = new ItemSlot(inventory[i].Count, inventory[i].Item?.Clone());
    }

    CharacterType ICharacter.Type { get => type; init => type = value; }
    Gender ICharacter.Gender { get => gender; init => gender = value; }
    Race ICharacter.Race { get => race; init => race = value; }
    Class ICharacter.Class { get => @class; init => @class = value; }
    public byte Level { get; set; }
    public word Gold { get; set; }
    public word Food { get; set; }
    public Dictionary<EquipmentSlot, ItemSlot> Equipment => equipment;
    public ItemSlot[] Inventory => inventory;
    string ICharacter.Name { get => name; init => name = value; }
}
