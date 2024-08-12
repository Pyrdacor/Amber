namespace Amber.IO.Common.FileSystem;

public interface IReadOnlyFileSystem : IFileReaderProvider
{
    bool MemoryFileSystem { get; }
    IReadOnlyNode? GetNode(string path);
    IReadOnlyFolder? GetFolder(string path);
    IReadOnlyFile? GetFile(string path);
    IEnumerable<IReadOnlyFile> GetAllFiles();
}
