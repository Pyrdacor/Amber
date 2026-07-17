using AmberIsland.GameData;

namespace AmberIslandTerrainEditor;

/// <summary>References a texture frame picked from a loaded sprite container.</summary>
internal sealed record TerrainTextureRef(uint SpriteIndex, int AtlasFrame);

/// <summary>A configurable height band mapped to a ground <see cref="TileType"/>.</summary>
internal sealed class TerrainTypeRange
{
    public required TileType Type { get; set; }
    public bool Enabled { get; set; } = true;
    public float MinHeight { get; set; }
    public float MaxHeight { get; set; }
    public TerrainTextureRef? Texture { get; set; }
}

/// <summary>The curated set of <see cref="TileType"/> values relevant to heightmap-based terrain.</summary>
internal static class TerrainTypeCatalog
{
    public static readonly TileType[] Defaults =
    [
        TileType.DeepWater,
        TileType.ShallowWater,
        TileType.Sand,
        TileType.HotSand,
        TileType.Mud,
        TileType.Swamp,
        TileType.Grass,
        TileType.HighGrass,
        TileType.Earth,
        TileType.Stone,
        TileType.Ice,
        TileType.Snow
    ];

    public static List<TerrainTypeRange> CreateDefaultRanges()
    {
        var ranges = new List<TerrainTypeRange>(Defaults.Length);
        float step = 1f / Defaults.Length;

        for (int i = 0; i < Defaults.Length; i++)
        {
            ranges.Add(new TerrainTypeRange
            {
                Type = Defaults[i],
                Enabled = true,
                MinHeight = i * step,
                MaxHeight = (i + 1) * step
            });
        }

        return ranges;
    }
}

internal static class TerrainTypeResolver
{
    /// <summary>
    /// Resolves the ground <see cref="TileType"/> for a given normalized height.
    /// Ordered list = priority order. The first enabled range covering the height wins.
    /// If no enabled range covers it (a gap, or the owning range is disabled), falls back
    /// to the nearest enabled range so disabling a type never leaves holes. If no range is
    /// enabled at all, falls back to <see cref="TileType.Grass"/>.
    /// </summary>
    public static TileType Resolve(float height, IReadOnlyList<TerrainTypeRange> ranges)
    {
        TerrainTypeRange? nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (var range in ranges)
        {
            if (!range.Enabled)
                continue;

            if (height >= range.MinHeight && height <= range.MaxHeight)
                return range.Type;

            float distance = height < range.MinHeight
                ? range.MinHeight - height
                : height - range.MaxHeight;

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = range;
            }
        }

        return nearest?.Type ?? TileType.Grass;
    }
}
