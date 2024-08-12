using Amber.IO.Common.FileSystem;
using Amber.IO.FileSystem.Virtual;

namespace Ambermoon.Data.FileSystems.Virtual;

internal class ReadOnlyFileStream : IReadOnlyFileStream
{
    protected readonly DataStream baseStream;
    protected int streamOffset;
    protected int streamLength;

    public ReadOnlyFileStream(DataStream baseStream, int streamOffset, int streamLength)
    {
        this.baseStream = baseStream;
        this.streamOffset = streamOffset;
        this.streamLength = streamLength;
    }

    public int Offset => streamOffset;

    public int Size => streamLength;

    public IDisposableDataReader GetReader()
    {
        return baseStream.GetReader();
    }

    public void AdjustOffset(int newOffset)
    {
        streamOffset = newOffset;
    }
}
