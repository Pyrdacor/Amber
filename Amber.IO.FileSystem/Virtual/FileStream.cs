using Amber.IO.Common.FileSystem;
using Ambermoon.Data.FileSystems.Virtual;

namespace Amber.IO.FileSystem.Virtual;

internal class FileStream : ReadOnlyFileStream, IFileStream
{
    MemoryStream? writeStream;

    public FileStream(DataStream baseStream, int streamOffset, int streamLength)
        : base(baseStream, streamOffset, streamLength)
    {

    }

    void WriteStream()
    {
        var newSize = writeStream!.Length;

        if (newSize == streamLength)
        {
            baseStream.Position = streamOffset;
            baseStream.WriteBytes(writeStream.ToArray());
        }
        else
        {
            int change = (int)(newSize - streamLength);
            baseStream.Replace(streamOffset, streamLength, writeStream.ToArray());
            streamLength = (int)newSize;
            FileSizeChanged?.Invoke(change);
        }
    }

    public IDisposableDataWriter GetWriter()
    {
        writeStream = new MemoryStream(baseStream.ToArray(streamOffset, streamLength));
        return new StreamedDataWriter(writeStream, false, WriteStream);
    }

    public event Action<int>? FileSizeChanged;
}
