namespace Amber.IO.FileFormats.Compression;

public static class RLE0
{
    public static byte[] Decompress(byte[] data)
    {
        var decompressedData = new List<byte>();

        for (int i = 0; i < data.Length; i++)
        {
            var b = data[i];

            if (b == 0)
            {
                int count = data[++i];

                if ((count & 0x80) == 0)
                    count += 1;
                else
                    count = 129 + (((count & 0x7f) << 8) | data[++i]);

                while (--count >= 0)
                    decompressedData.Add(0);
            }
            else
                decompressedData.Add(b);
        }

        return [.. decompressedData];
    }

    public static byte[] Compress(byte[] data)
    {
        int zeroCount = 0;
        var compressedData = new List<byte>();

        void WriteZeros()
        {
            if (zeroCount == 0)
                return;

            int chunks = zeroCount / 32_896;
            int lastChunkSize = zeroCount % 32_896;

            for (int i = 0; i < chunks; ++i)
            {
                compressedData.Add(0);
                compressedData.Add(0x7f);
                compressedData.Add(0xff);
            }

            if (lastChunkSize != 0)
            {
                compressedData.Add(0);

                if (lastChunkSize <= 128)
                    compressedData.Add((byte)(lastChunkSize - 1));
                else
                {
                    lastChunkSize -= 129;
                    compressedData.Add((byte)(0x80 | (lastChunkSize >> 8)));
                    compressedData.Add((byte)(lastChunkSize & 0xff));
                }
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

        return [..compressedData];
    }
}
