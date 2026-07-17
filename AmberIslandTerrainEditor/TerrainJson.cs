using System.Text.Json;
using System.Text.Json.Serialization;

namespace AmberIslandTerrainEditor;

/// <summary>Shared JSON options for the editor's own project/preset file formats.</summary>
internal static class TerrainJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };
}
