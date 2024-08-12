using Amber.IO.Common.FileSystem;
using static System.IO.Path;

namespace Amber.IO.FileSystem.OperatingSystem;

internal class ReadOnlyFileSystem : IReadOnlyFileSystem
{
    readonly string rootPath;
    readonly IReadOnlyFolder rootFolder;

    public ReadOnlyFileSystem(string rootPath)
    {
        this.rootPath = rootPath;

        if (!this.rootPath.EndsWith(DirectorySeparatorChar) && !this.rootPath.EndsWith(AltDirectorySeparatorChar))
            this.rootPath += DirectorySeparatorChar;

        rootFolder = new ReadOnlyFolder(new DirectoryInfo(this.rootPath), null);
    }

    public bool MemoryFileSystem => false;

    string ToRelativePath(string path)
    {
        if (path.StartsWith(rootPath, StringComparison.InvariantCultureIgnoreCase))
            return path[rootPath.Length..];

        return path;
    }

    static string[] GetPathParts(string path)
    {
        return path.Split(DirectorySeparatorChar, AltDirectorySeparatorChar);
    }

    public IReadOnlyNode? GetNode(string path)
    {
        path = ToRelativePath(path);
        var parts = GetPathParts(path);
        return GetNode(parts, 0, rootFolder);
    }

    static IReadOnlyNode? GetNode(string[] pathParts, int currentIndex, IReadOnlyFolder parent)
    {
        string name = pathParts[currentIndex];

        if (currentIndex == pathParts.Length - 1)
        {
            if (parent.Files.TryGetValue(name, out var file))
                return file;

            if (parent.Folders.TryGetValue(name, out var folder))
                return folder;                

            return null;
        }
        else
        {
            if (!parent.Folders.TryGetValue(name, out var folder))
                return null;

            return GetNode(pathParts, currentIndex + 1, folder);
        }
    }

    public IReadOnlyFile? GetFile(string path)
    {
        return GetNode(path) as IReadOnlyFile;
    }

    public IReadOnlyFolder? GetFolder(string path)
    {
        return GetNode(path) as IReadOnlyFolder;
    }

    public IReadOnlyFileStream? GetFileReader(string path)
    {
        return GetFile(path)?.Stream;
    }

    public IEnumerable<IReadOnlyFile> GetAllFiles()
    {
        var foundFiles = new List<IReadOnlyFile>();
        GetAllFilesInFolder(foundFiles, rootFolder);
        return foundFiles;
    }

    static void GetAllFilesInFolder(List<IReadOnlyFile> foundFiles, IReadOnlyFolder folder)
    {
        foreach (var subFolder in folder.Folders)
            GetAllFilesInFolder(foundFiles, subFolder.Value);

        foreach (var file in folder.Files)
            foundFiles.Add(file.Value);
    }
}
