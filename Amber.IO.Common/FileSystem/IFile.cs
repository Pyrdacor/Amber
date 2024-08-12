namespace Amber.IO.Common.FileSystem;

public interface IFile : INode
{
    public IFileStream Stream { get; }
}
