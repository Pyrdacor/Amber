namespace Amber.IO.Common.FileSystem;

public interface IReadOnlyFileStream
{
    IDisposableDataReader GetReader();
    int Size { get; }
}
