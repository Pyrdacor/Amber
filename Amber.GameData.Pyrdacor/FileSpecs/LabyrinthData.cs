﻿using Amber.GameData.Pyrdacor.Compressions;
using Ambermoon.Data.Serialization;

namespace Amber.GameData.Pyrdacor.FileSpecs
{
    internal class LabyrinthData : IFileSpec
    {
        public string Magic => "LAB";
        public byte SupportedVersion => 0;
        public ushort PreferredCompression => ICompression.GetIdentifier<Deflate>();

        public void Read(IDataReader dataReader, uint _, GameData __)
        {
            throw new NotImplementedException();
        }

        public void Write(IDataWriter dataWriter)
        {
            throw new NotImplementedException();
        }
    }
}
