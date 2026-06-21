using Amber.IO.Common.Serialization;
using Amberstar.GameData;

namespace Amberstar.Game;

public interface ISavedItemSlot : IItemSlot
{
    int Amount { get; set; }

    uint ItemIndex { get; set; }

    void Write(IDataWriter dataWriter);
}

public interface ISavedChest
{
    int Index { get; set; }

    ISavedItemSlot[] ItemsSlots { get; }

    void Write(IDataWriter dataWriter);
}

public interface IExtendedSavegame : ISavegame
{
    Dictionary<uint, IStaticItemData> Items { get; }

    Dictionary<int, ISavedChest> Chests { get; }
}

file record Chest(int Index, IItem?[] Items) : IChest;

public static class ExtendedSavegameExtensions
{
    public static IChest? GetChest(this IExtendedSavegame savegame, IItemFactory itemFactory, int chestIndex)
    {
        if (!savegame.Chests.TryGetValue(chestIndex, out var chest))
            return null;

        var items = new IItem?[IChest.SlotCount];

        for (int i = 0; i < items.Length; i++)
        {
            var itemSlot = chest.ItemsSlots[i];
            var itemData = savegame.Items[itemSlot.ItemIndex];

            items[i] = itemFactory.Create(itemData, itemSlot);
        }

        return new Chest(chestIndex, items);
    }
}