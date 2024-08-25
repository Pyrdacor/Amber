using Amber.GameData.Pyrdacor.Compressions;
using Amber.GameData.Pyrdacor.Objects;
using Ambermoon.Data.Serialization;

namespace Amber.GameData.Pyrdacor.FileSpecs
{
    internal class Texts : IFileSpec
    {
        public string Magic => "TXT";
        public byte SupportedVersion => 0;
        public ushort PreferredCompression => ICompression.GetIdentifier<Deflate>();
        TextList? textList = null;

        public TextList TextList => textList!;

        public Texts()
        {

        }

        public Texts(TextList textList)
        {
            this.textList = textList;
        }

        public void Read(IDataReader dataReader, uint _, GameData __)
        {
            textList = new TextList(dataReader);
        }

        public void Write(IDataWriter dataWriter)
        {
            if (textList == null)
                throw new AmberException(ExceptionScope.Application, "Text data was null when trying to write it.");

            textList.Write(dataWriter);
        }
    }
}
