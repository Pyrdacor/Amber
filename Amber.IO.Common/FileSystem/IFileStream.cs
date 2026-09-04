namespace Amber.IO.Common.FileSystem;

public interface IFileStream : IReadOnlyFileStream
{
    /// <summary>
    /// Opens a writer over the file's data. Dispose it when done.
    /// </summary>
    IDisposableDataWriter GetWriter();
}
