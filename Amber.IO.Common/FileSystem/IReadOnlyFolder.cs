namespace Amber.IO.Common.FileSystem;

public interface IReadOnlyFolder : IReadOnlyNode
{
    public IReadOnlyDictionary<string, IReadOnlyFolder> Folders { get; }
    public IReadOnlyDictionary<string, IReadOnlyFile> Files { get; }
}
