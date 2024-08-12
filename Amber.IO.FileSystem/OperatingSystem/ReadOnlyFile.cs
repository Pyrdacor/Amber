using Amber.IO.Common.FileSystem;

namespace Amber.IO.FileSystem.OperatingSystem;

internal class ReadOnlyFile : IReadOnlyFile
{
    readonly FileInfo file;
    IReadOnlyFileStream? stream;

    public ReadOnlyFile(FileInfo fileInfo, IReadOnlyFolder parent)
    {
        file = fileInfo;
        Parent = parent;
    }

    public IReadOnlyFileStream Stream
    {
        get
        {
            if (stream != null)
                return stream;

            return stream = new ReadOnlyFileStream(file);
        }
    }

    public string Name => file.Name;

    public string Path => file.FullName;

    public IReadOnlyFolder Parent { get; }
}
