namespace Amber.IO.Common.FileSystem;

public interface IFileReaderProvider
{
    /// <summary>
    /// Returns a stream for reading the file at the given path, or null if it doesn't exist.
    /// </summary>
    IReadOnlyFileStream? GetFileReader(string path);
}
