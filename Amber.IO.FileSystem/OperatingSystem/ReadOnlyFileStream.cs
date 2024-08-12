using Amber.IO.Common.FileSystem;

namespace Amber.IO.FileSystem.OperatingSystem;

internal class ReadOnlyFileStream : IReadOnlyFileStream
{
    protected readonly FileInfo file;

    public ReadOnlyFileStream(FileInfo fileInfo)
    {
        file = fileInfo;
    }

    public int Size => (int)file.Length;

    public IDisposableDataReader GetReader()
    {
        var stream = file.OpenRead();
        return new StreamedDataReader(stream, false);
    }
}
