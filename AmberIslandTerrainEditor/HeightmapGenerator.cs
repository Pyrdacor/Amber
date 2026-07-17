namespace AmberIslandTerrainEditor;

internal sealed record NoiseParameters(
    int Seed,
    float Scale,
    int Octaves,
    float Persistence,
    float Lacunarity,
    bool IslandFalloff,
    float IslandFalloffStrength)
{
    public static NoiseParameters Default { get; } = new(
        Seed: Environment.TickCount,
        Scale: 0.05f,
        Octaves: 4,
        Persistence: 0.5f,
        Lacunarity: 2.0f,
        IslandFalloff: false,
        IslandFalloffStrength: 0.5f);
}

internal static class HeightmapGenerator
{
    /// <summary>
    /// Generates a width*height heightmap (row-major, y*width+x), values normalized to [0, 1].
    /// Deterministic for a given (width, height, parameters) triple.
    /// </summary>
    public static float[] Generate(int width, int height, NoiseParameters parameters)
    {
        var noise = new PerlinNoise(parameters.Seed);
        var result = new float[width * height];

        int octaves = Math.Max(1, parameters.Octaves);
        float maxAmplitude = 0f;
        float amplitude = 1f;
        for (int o = 0; o < octaves; o++)
        {
            maxAmplitude += amplitude;
            amplitude *= parameters.Persistence;
        }

        float scale = parameters.Scale <= 0f ? 0.0001f : parameters.Scale;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float frequency = 1f;
                amplitude = 1f;
                float sum = 0f;

                for (int o = 0; o < octaves; o++)
                {
                    double sampleX = x * scale * frequency;
                    double sampleY = y * scale * frequency;
                    sum += (float)noise.Sample(sampleX, sampleY) * amplitude;

                    amplitude *= parameters.Persistence;
                    frequency *= parameters.Lacunarity;
                }

                float normalized = maxAmplitude > 0f ? sum / maxAmplitude : 0f;
                float value01 = (normalized + 1f) * 0.5f;

                if (parameters.IslandFalloff)
                    value01 = ApplyIslandFalloff(value01, x, y, width, height, parameters.IslandFalloffStrength);

                result[y * width + x] = Math.Clamp(value01, 0f, 1f);
            }
        }

        return result;
    }

    private static float ApplyIslandFalloff(float value, int x, int y, int width, int height, float strength)
    {
        float nx = width <= 1 ? 0f : (x / (float)(width - 1)) * 2f - 1f;
        float ny = height <= 1 ? 0f : (y / (float)(height - 1)) * 2f - 1f;

        float distanceSquared = nx * nx + ny * ny;
        float falloff = Math.Clamp(1f - distanceSquared, 0f, 1f);

        float falloffValue = value * falloff;
        return value + (falloffValue - value) * Math.Clamp(strength, 0f, 1f);
    }
}
