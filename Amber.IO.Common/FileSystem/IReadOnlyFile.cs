namespace Amber.IO.Common.FileSystem;

public interface IReadOnlyFile : IReadOnlyNode
{
    public IReadOnlyFileStream Stream { get; }
}
