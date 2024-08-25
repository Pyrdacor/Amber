using Ambermoon.Data.Legacy.Serialization;
using Amber.GameData.Pyrdacor.Compressions;
using Ambermoon.Data.Serialization;

namespace Amber.GameData.Pyrdacor.FileSpecs
{
    internal class ItemData : IFileSpec
    {
        public string Magic => "ITM";
        public byte SupportedVersion => 0;
        public ushort PreferredCompression => ICompression.GetIdentifier<Deflate>();
        Item? item = null;

        public Item Item => item!;

        public ItemData()
        {

        }

        public ItemData(Item item)
        {
            this.item = item;
        }

        public void Read(IDataReader dataReader, uint index, GameData _)
        {
            item = Item.Load(index, new ItemReader(), dataReader);
        }

        public void Write(IDataWriter dataWriter)
        {
            if (item == null)
                throw new AmberException(ExceptionScope.Application, "Chest data was null when trying to write it.");

            ItemWriter.WriteItem(item, dataWriter);
        }
    }
}
