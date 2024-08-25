﻿using Amber.Serialization;

namespace Amber.GameData.Pyrdacor.Compressions
{
    internal class RLE0 : ICompression
    {
        public ushort Identifier => 0xC001;

        public IDataReader Decompress(IDataReader dataReader)
        {
            var decompressedData = new List<byte>();

            while (dataReader.Position < dataReader.Size)
            {
                var b = dataReader.ReadByte();

                if (b == 0)
                    decompressedData.AddRange(Enumerable.Repeat((byte)0, 1 + dataReader.ReadByte()));
                else
                    decompressedData.Add(b);
            }

            return new DataReader(decompressedData.ToArray());
        }

        public IDataWriter Compress(IDataWriter dataWriter)
        {
            int zeroCount = 0;
            var compressedData = new List<byte>();
            var data = dataWriter.ToArray();

            void WriteZeros()
            {
                if (zeroCount == 0)
                    return;

                int chunks = zeroCount / 256;
                int lastChunkSize = zeroCount % 256;

                if (lastChunkSize == 0)
                    ++chunks;

                for (int i = 0; i < chunks - 1; ++i)
                {
                    compressedData.Add(0);
                    compressedData.Add(255);
                }

                if (lastChunkSize != 0)
                {
                    compressedData.Add(0);
                    compressedData.Add((byte)(lastChunkSize - 1));
                }

                zeroCount = 0;
            }

            for (int i = 0; i < data.Length; ++i)
            {
                if (data[i] == 0)
                    ++zeroCount;
                else
                {
                    WriteZeros();
                    compressedData.Add(data[i]);
                }
            }

            WriteZeros();

            return new DataWriter(compressedData.ToArray());
        }
    }
}
