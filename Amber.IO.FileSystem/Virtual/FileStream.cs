using Amber.IO.Common.FileSystem;

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
        // like a file opened for writing: it starts at the beginning and can grow (it is not truncated)
        writeStream = new MemoryStream();
        writeStream.Write(baseStream.ToArray(streamOffset, streamLength));
        writeStream.Position = 0;
        return new StreamedDataWriter(writeStream, false, WriteStream);
    }

    public event Action<int>? FileSizeChanged;
}
