using Amber.IO.Common.FileSystem;

namespace Amber.IO.FileSystem.OperatingSystem;

internal class FileStream : ReadOnlyFileStream, IFileStream
{
    public FileStream(FileInfo fileInfo)
        : base(fileInfo)
    {

    }

    public IDisposableDataWriter GetWriter()
    {
        var stream = file.OpenWrite();
        return new StreamedDataWriter(stream, false);
    }
}
