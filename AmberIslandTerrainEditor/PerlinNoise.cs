namespace AmberIslandTerrainEditor;

/// <summary>
/// Classic seeded 2D gradient (Perlin) noise. Values are in the range [-1, 1].
/// </summary>
internal sealed class PerlinNoise
{
    private static readonly (double X, double Y)[] Gradients =
    [
        (1, 0), (-1, 0), (0, 1), (0, -1),
        (1, 1), (-1, 1), (1, -1), (-1, -1)
    ];

    private readonly int[] permutation = new int[512];

    public PerlinNoise(int seed)
    {
        var source = new int[256];
        for (int i = 0; i < 256; i++)
            source[i] = i;

        var random = new Random(seed);
        for (int i = 255; i > 0; i--)
        {
            int swapIndex = random.Next(i + 1);
            (source[i], source[swapIndex]) = (source[swapIndex], source[i]);
        }

        for (int i = 0; i < 512; i++)
            permutation[i] = source[i & 255];
    }

    public double Sample(double x, double y)
    {
        int cellX = (int)Math.Floor(x) & 255;
        int cellY = (int)Math.Floor(y) & 255;

        double localX = x - Math.Floor(x);
        double localY = y - Math.Floor(y);

        double fadeX = Fade(localX);
        double fadeY = Fade(localY);

        int a = permutation[cellX] + cellY;
        int b = permutation[cellX + 1] + cellY;

        double dotTopLeft = DotGradient(permutation[a], localX, localY);
        double dotTopRight = DotGradient(permutation[b], localX - 1, localY);
        double dotBottomLeft = DotGradient(permutation[a + 1], localX, localY - 1);
        double dotBottomRight = DotGradient(permutation[b + 1], localX - 1, localY - 1);

        double top = Lerp(dotTopLeft, dotTopRight, fadeX);
        double bottom = Lerp(dotBottomLeft, dotBottomRight, fadeX);

        return Lerp(top, bottom, fadeY);
    }

    private double DotGradient(int hash, double x, double y)
    {
        var gradient = Gradients[hash & 7];
        return gradient.X * x + gradient.Y * y;
    }

    private static double Fade(double t) => t * t * t * (t * (t * 6 - 15) + 10);

    private static double Lerp(double a, double b, double t) => a + t * (b - a);
}
