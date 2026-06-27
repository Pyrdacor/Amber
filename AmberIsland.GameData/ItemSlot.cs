using Amber.IO.Common.Serialization;

namespace AmberIsland.GameData;

public readonly record struct ItemSlot
(
    // Word-sized
    ushort Amount,
    ushort ItemIndex
)
{
    public void Write(IDataWriter writer)
    {
        // Word-sized
        writer.Write(Amount);
        writer.Write(ItemIndex);
    }

    public static ItemSlot Read(IDataReader reader)
    {
        // Word-sized
        var amount = reader.ReadWord();
        var itemIndex = reader.ReadWord();

        return new
        (
            // Word-sized
            amount,
            itemIndex
        );
    }
}
