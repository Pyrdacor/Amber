using Ambermoon.Data.Legacy.Serialization;
using Amber.GameData.Pyrdacor.Compressions;
using Ambermoon.Data.Serialization;

namespace Amber.GameData.Pyrdacor.FileSpecs
{
    internal class ChestData : IFileSpec
    {
        public string Magic => "CHE";
        public byte SupportedVersion => 0;
        public ushort PreferredCompression => ICompression.GetIdentifier<RLE0>();
        Chest? chest = null;

        public ChestData()
        {

        }

        public ChestData(Chest chest)
        {
            this.chest = chest;
        }

        public void Read(IDataReader dataReader, uint _, GameData __)
        {
            chest = Chest.Load(new ChestReader(), dataReader);
        }

        public void Write(IDataWriter dataWriter)
        {
            if (chest == null)
                throw new AmberException(ExceptionScope.Application, "Chest data was null when trying to write it.");

            new ChestWriter().WriteChest(chest, dataWriter);
        }
    }
}
