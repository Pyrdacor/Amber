namespace Amber.IO.Common.FileSystem;

public interface IFileReaderProvider
{
    IReadOnlyFileStream? GetFileReader(string path);
}
