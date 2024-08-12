namespace Amber.IO.Common.FileSystem;

public interface IFolder : INode
{
    public IReadOnlyDictionary<string, IFolder> Folders { get; }
    public IReadOnlyDictionary<string, IFile> Files { get; }
}
