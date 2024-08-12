using Amber.IO.Common.FileSystem;

namespace Amber.IO.FileSystem.Virtual;

internal class ReadOnlyFolder : IReadOnlyFolder
{
    readonly Dictionary<string, IReadOnlyFolder> folders = [];
    readonly Dictionary<string, IReadOnlyFile> files = [];

    public ReadOnlyFolder(string name, IReadOnlyFolder? parent)
    {
        Name = name;
        Parent = parent;
    }

    public IReadOnlyDictionary<string, IReadOnlyFolder> Folders => folders;

    public IReadOnlyDictionary<string, IReadOnlyFile> Files => files;

    public string Name { get; }

    public string Path => Parent == null ? Name : Parent.Path + "/" + Name;

    public IReadOnlyFolder? Parent { get; }
}
