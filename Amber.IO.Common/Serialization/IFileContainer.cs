namespace Amber.IO.Common.Serialization;

public interface IFileContainer
{
    string Name { get; }
    /// <summary>
    /// Identifies the container's file/format type.
    /// </summary>
    uint Header { get; }
    /// <summary>
    /// The contained files, keyed by their index within the container.
    /// </summary>
    Dictionary<int, IDataReader> Files { get; }
}
