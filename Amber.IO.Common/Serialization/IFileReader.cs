namespace Amber.IO.Common.Serialization;

public interface IFileReader
{
    /// <summary>
    /// Reads and unpacks a container file from the given stream.
    /// </summary>
    IFileContainer ReadRawFile(string name, Stream stream);
}
