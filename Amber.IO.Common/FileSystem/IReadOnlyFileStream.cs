namespace Amber.IO.Common.FileSystem;

public interface IReadOnlyFileStream
{
    /// <summary>
    /// Opens a reader over the file's data. Dispose it when done.
    /// </summary>
    IDisposableDataReader GetReader();
    int Size { get; }
}
