namespace Amber.Serialization;

public interface IFileContainer
{
    string Name { get; }
    uint Header { get; }
    Dictionary<int, IDataReader> Files { get; }
}
