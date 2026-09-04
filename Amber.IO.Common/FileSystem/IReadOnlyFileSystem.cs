namespace Amber.IO.Common.FileSystem;

public interface IReadOnlyFileSystem : IFileReaderProvider
{
    /// <summary>
    /// True if this file system lives entirely in memory rather than being backed by disk.
    /// </summary>
    bool MemoryFileSystem { get; }
    /// <summary>
    /// Looks up a node (file or folder) by path, or null if it doesn't exist.
    /// </summary>
    IReadOnlyNode? GetNode(string path);
    /// <summary>
    /// Looks up a folder by path, or null if it doesn't exist.
    /// </summary>
    IReadOnlyFolder? GetFolder(string path);
    /// <summary>
    /// Looks up a file by path, or null if it doesn't exist.
    /// </summary>
    IReadOnlyFile? GetFile(string path);
    /// <summary>
    /// Returns all files in this file system, recursively.
    /// </summary>
    IEnumerable<IReadOnlyFile> GetAllFiles();
}
