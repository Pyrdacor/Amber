namespace Amberstar.GameData;

public record ItemSlot
{
    public ItemSlot(byte count, IItem? item)
    {
        Count = count;
        Item = item;
    }

    public byte Count { get; set; }
    public IItem? Item { get; private set; }

    public (IItem? OldItem, byte OldCount) SetItem(IItem item, byte count = 1)
    {
        var oldItem = Item;
        var oldCount = Count;

        Item = item;
        Count = count;

        return (oldItem, oldCount);
    }

    public (IItem? OldItem, byte OldCount) ClearItem()
    {
        var oldItem = Item;
        var oldCount = Count;

        Item = null;
        Count = 0;

        return (oldItem, oldCount);
    }
}
