using Ambermoon.Data.Legacy.Characters;
using Amber.GameData.Pyrdacor.Compressions;
using Ambermoon.Data.Serialization;

namespace Amber.GameData.Pyrdacor.FileSpecs
{
    internal class CharacterData : IFileSpec
    {
        public string Magic => "CHR";
        public byte SupportedVersion => 0;
        public ushort PreferredCompression => ICompression.GetIdentifier<Deflate>();
        Character? character = null;

        public Character Character => character!;

        public CharacterData()
        {

        }

        public CharacterData(Character character)
        {
            this.character = character;
        }

        public void Read(IDataReader dataReader, uint index, GameData gameData)
        {
            switch (dataReader.PeekByte())
            {
                case 0: // party member
                    // TODO
                    throw new NotImplementedException();
                case 1: // NPC
                    // TODO
                    throw new NotImplementedException();
                case 2: // monster
                    // TODO
                    throw new NotImplementedException();
                default:
                    throw new AmberException(ExceptionScope.Data, "Invalid character data.");
            }
        }

        public void Write(IDataWriter dataWriter)
        {
            if (character == null)
                throw new AmberException(ExceptionScope.Application, "Character data was null when trying to write it.");

            // TODO
            throw new NotImplementedException();
        }
    }
}
