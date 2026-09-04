namespace Amber.IO.Common.FileSystem;

public interface IFileSystem : IFileReaderProvider
{
    /// <summary>
    /// True if this file system lives entirely in memory rather than being backed by disk.
    /// </summary>
    bool MemoryFileSystem { get; }
    /// <summary>
    /// Looks up a node (file or folder) by path, or null if it doesn't exist.
    /// </summary>
    INode? GetNode(string path);
    /// <summary>
    /// Looks up a folder by path, or null if it doesn't exist.
    /// </summary>
    IFolder? GetFolder(string path);
    /// <summary>
    /// Looks up a file by path, or null if it doesn't exist.
    /// </summary>
    IFile? GetFile(string path);
    /// <summary>
    /// Creates the folder at the given path, including any missing parent folders.
    /// </summary>
    IFolder CreateFolder(string path);
    /// <summary>
    /// Creates a file at the given path with the given contents.
    /// </summary>
    IFile CreateFile(string path, byte[] data);
    /// <summary>
    /// Creates an empty file at the given path.
    /// </summary>
    IFile CreateEmptyFile(string path);
    /// <summary>
    /// Returns a read-only view of this file system.
    /// </summary>
    IReadOnlyFileSystem AsReadOnly();
    /// <summary>
    /// Returns all files in this file system, recursively.
    /// </summary>
    IEnumerable<IFile> GetAllFiles();
}
