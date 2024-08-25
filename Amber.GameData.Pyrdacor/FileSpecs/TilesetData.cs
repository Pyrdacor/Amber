using Ambermoon.Data.Legacy.Serialization;
using Amber.GameData.Pyrdacor.Compressions;
using Ambermoon.Data.Serialization;

namespace Amber.GameData.Pyrdacor.FileSpecs
{
    internal class TilesetData : IFileSpec
    {
        public string Magic => "TIL";
        public byte SupportedVersion => 0;
        public ushort PreferredCompression => ICompression.GetIdentifier<Deflate>();
        Tileset? tileset = null;

        public Tileset Tileset => tileset!;

        public TilesetData()
        {

        }

        public TilesetData(Tileset tileset)
        {
            this.tileset = tileset;
        }

        public void Read(IDataReader dataReader, uint _, GameData __)
        {
            tileset = new Tileset();
            new TilesetReader().ReadTileset(tileset, dataReader);
        }

        public void Write(IDataWriter dataWriter)
        {
            if (tileset == null)
                throw new AmberException(ExceptionScope.Application, "Tileset data was null when trying to write it.");

            TilesetWriter.WriteTileset(tileset, dataWriter);
        }
    }
}
