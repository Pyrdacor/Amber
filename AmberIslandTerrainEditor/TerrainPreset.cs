using System.Text.Json;
using AmberIsland.GameData;

namespace AmberIslandTerrainEditor;

/// <summary>A preset's height band, deliberately without a texture — textures are per-project.</summary>
internal sealed record TerrainPresetTypeRange(TileType Type, bool Enabled, float MinHeight, float MaxHeight);

internal sealed record TerrainPreset(string Name, NoiseParameters Noise, List<TerrainPresetTypeRange> TypeRanges)
{
    public List<TerrainTypeRange> ToTypeRanges(IReadOnlyList<TerrainTypeRange>? existing = null)
    {
        var result = new List<TerrainTypeRange>(TypeRanges.Count);

        foreach (var presetRange in TypeRanges)
        {
            var texture = existing?.FirstOrDefault(r => r.Type == presetRange.Type)?.Texture;

            result.Add(new TerrainTypeRange
            {
                Type = presetRange.Type,
                Enabled = presetRange.Enabled,
                MinHeight = presetRange.MinHeight,
                MaxHeight = presetRange.MaxHeight,
                Texture = texture
            });
        }

        return result;
    }

    public static TerrainPreset FromCurrent(string name, NoiseParameters noise, IReadOnlyList<TerrainTypeRange> typeRanges)
    {
        var ranges = typeRanges
            .Select(r => new TerrainPresetTypeRange(r.Type, r.Enabled, r.MinHeight, r.MaxHeight))
            .ToList();

        return new TerrainPreset(name, noise, ranges);
    }

    public static TerrainPreset Load(string path)
    {
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<TerrainPreset>(json, TerrainJson.Options)
            ?? throw new InvalidDataException($"Could not parse preset file: {path}");
    }

    public void Save(string path)
    {
        string json = JsonSerializer.Serialize(this, TerrainJson.Options);
        File.WriteAllText(path, json);
    }

    public static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
        return sanitized.Length == 0 ? "preset" : sanitized;
    }
}
