namespace Amber.IO.Common.FileSystem;

public interface IFileStream : IReadOnlyFileStream
{
    IDisposableDataWriter GetWriter();
}
