namespace Amber.IO.Common.Serialization;

public interface IFileReader
{
    IFileContainer ReadRawFile(string name, Stream stream);
}
