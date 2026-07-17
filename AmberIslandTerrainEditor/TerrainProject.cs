using System.Text.Json;
using AmberIsland.GameData;

namespace AmberIslandTerrainEditor;

internal sealed record TerrainProjectTypeRange(
    TileType Type,
    bool Enabled,
    float MinHeight,
    float MaxHeight,
    uint? TextureSpriteIndex,
    int? TextureAtlasFrame);

/// <summary>
/// The editor's own project format (.aiterrain, JSON). Deliberately does NOT store the raw
/// heightmap grid: it is fully determined by Seed + Noise + Width/Height and is regenerated
/// deterministically via <see cref="HeightmapGenerator.Generate"/> on every load. This keeps
/// project files small and guarantees the seed+params combination stays the source of truth.
/// </summary>
internal sealed record TerrainProject(
    int Width,
    int Height,
    NoiseParameters Noise,
    string? SpriteContainerPath,
    List<TerrainProjectTypeRange> TypeRanges,
    string? LastAppliedPreset)
{
    public const string Extension = ".aiterrain";

    public static TerrainProject CreateDefault()
    {
        var ranges = TerrainTypeCatalog.CreateDefaultRanges()
            .Select(r => new TerrainProjectTypeRange(r.Type, r.Enabled, r.MinHeight, r.MaxHeight, null, null))
            .ToList();

        return new TerrainProject(128, 128, NoiseParameters.Default, null, ranges, null);
    }

    public List<TerrainTypeRange> ToTypeRanges() => TypeRanges
        .Select(r => new TerrainTypeRange
        {
            Type = r.Type,
            Enabled = r.Enabled,
            MinHeight = r.MinHeight,
            MaxHeight = r.MaxHeight,
            Texture = r.TextureSpriteIndex.HasValue && r.TextureAtlasFrame.HasValue
                ? new TerrainTextureRef(r.TextureSpriteIndex.Value, r.TextureAtlasFrame.Value)
                : null
        })
        .ToList();

    public static TerrainProject FromCurrent(
        int width,
        int height,
        NoiseParameters noise,
        string? spriteContainerPath,
        IReadOnlyList<TerrainTypeRange> typeRanges,
        string? lastAppliedPreset)
    {
        var ranges = typeRanges
            .Select(r => new TerrainProjectTypeRange(r.Type, r.Enabled, r.MinHeight, r.MaxHeight, r.Texture?.SpriteIndex, r.Texture?.AtlasFrame))
            .ToList();

        return new TerrainProject(width, height, noise, spriteContainerPath, ranges, lastAppliedPreset);
    }

    public static TerrainProject Load(string path)
    {
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<TerrainProject>(json, TerrainJson.Options)
            ?? throw new InvalidDataException($"Could not parse terrain project file: {path}");
    }

    public void Save(string path)
    {
        string json = JsonSerializer.Serialize(this, TerrainJson.Options);
        File.WriteAllText(path, json);
    }
}
