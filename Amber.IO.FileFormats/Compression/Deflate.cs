using System.IO.Compression;
using Amber.IO.Common.Serialization;
using Amber.IO.FileFormats.Serialization;

namespace Amber.IO.FileFormats.Compression;

public static class Deflate
{
    public static byte[] Decompress(byte[] data)
    {
        using Stream compressedFileStream = new MemoryStream(data);
        using var targetStream = new MemoryStream();
        using var decompressor = new DeflateStream(compressedFileStream, CompressionMode.Decompress);
        decompressor.CopyTo(targetStream);
        targetStream.Position = 0;
        return targetStream.ToArray();
    }

    public static IDataReader Decompress(IDataReader dataReader)
    {
        return new DataReader(Decompress(dataReader.ReadToEnd()));
    }

    public static byte[] Compress(byte[] data)
    {
        using Stream sourceStream = new MemoryStream(data);
        using var compressedStream = new MemoryStream();

        using (var compressor = new DeflateStream(compressedStream, CompressionLevel.Optimal))
        {
            sourceStream.CopyTo(compressor);
        }

        return compressedStream.ToArray();
    }

    public static byte[] Compress(IDataWriter dataWriter) => Compress(dataWriter.ToArray());
}
