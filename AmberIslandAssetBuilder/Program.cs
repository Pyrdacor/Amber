using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.Json;
using Amber.IO.FileFormats.Serialization;
using AmberIsland.GameData;
using AmberIslandAssetBuilder;
using Font = AmberIsland.GameData.Font;

#pragma warning disable CA1416 // Validate platform compatibility

if (args.Length < 2)
{
    Console.WriteLine("Usage: AmberIslandAssetBuilder <script-file> <assets-dir>");
    return 1;
}

string scriptPath = Path.GetFullPath(args[0]);
string assetsDir = Path.GetFullPath(args[1]);

if (!File.Exists(scriptPath))
{
    Console.Error.WriteLine($"Script not found: {scriptPath}");
    return 1;
}

if (!Directory.Exists(assetsDir))
{
    Console.Error.WriteLine($"Assets directory not found: {assetsDir}");
    return 1;
}

var scriptText = File.ReadAllText(scriptPath);
List<BuildOperation> operations;

try
{
    operations = BuildScriptParser.Parse(scriptText);
}
catch (FormatException ex)
{
    Console.Error.WriteLine($"Script parse error: {ex.Message}");
    return 1;
}

Console.WriteLine($"Script: {Path.GetFileName(scriptPath)}  ({operations.Count} operation(s))");
Console.WriteLine($"Assets: {assetsDir}");
Console.WriteLine();

var context = new BuildContext(assetsDir);
var runner = new BuildRunner();

// ---- Register handlers ----
runner.RegisterHandler("Container", HandleContainer);
runner.RegisterHandler("MapSpriteSheet", HandleMapSpriteSheet);
runner.RegisterHandler("Sprite", HandleSprite);
runner.RegisterHandler("FontSprite", HandleFontSprite);
runner.RegisterHandler("Font", HandleFont);

try
{
    runner.Run(context, operations);
    Console.WriteLine();
    Console.WriteLine("Build complete.");
}
finally
{
    if (context.TempFiles.Count > 0)
    {
        Console.WriteLine();
        BuildRunner.CleanupTempFiles(context);
    }
}

return 0;

// ========================================================================
// Built-in handlers
// ========================================================================

// ---- Container: pack matching files into a FileContainer ----

static void HandleContainer(BuildContext context, BuildOperation operation)
{
    string dir = Path.GetDirectoryName(operation.InputPath) ?? ".";
    string pattern = Path.GetFileName(operation.InputPath);
    string fullDir = context.ResolvePath(dir);

    if (!Directory.Exists(fullDir))
        throw new InvalidOperationException($"Directory not found: {fullDir}");

    var matchingFiles = Directory.GetFiles(fullDir, pattern)
        .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
        .ToList();

    if (matchingFiles.Count == 0)
        throw new InvalidOperationException($"No files match: {operation.InputPath}");

    var files = new Dictionary<uint, byte[]>();
    bool autoIndex = !uint.TryParse(
        Path.GetFileNameWithoutExtension(matchingFiles[0]).Split('_')[0], out _);

    uint nextIndex = 1;
    foreach (var filePath in matchingFiles)
    {
        uint index;
        if (autoIndex)
        {
            index = nextIndex++;
        }
        else
        {
            string indexStr = Path.GetFileNameWithoutExtension(filePath).Split('_')[0];
            if (!uint.TryParse(indexStr, out index))
                throw new FormatException($"Cannot parse file index from '{Path.GetFileName(filePath)}'. Expected format: 001_name.ext");
        }

        files[index] = File.ReadAllBytes(filePath);
    }

    string outputPath = context.ResolvePath(operation.OutputPath);
    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
    using var stream = File.Create(outputPath);
    FileContainer.Write(stream, files);

    Console.WriteLine($"  Container: {operation.OutputPath}  ({files.Count} files)");
}

// ---- MapSpriteSheet: select sprites + palettes from a container ----

static void HandleMapSpriteSheet(BuildContext context, BuildOperation operation)
{
    string sourcePath = context.ResolvePath(operation.InputPath);
    Dictionary<uint, byte[]> files;
    using (var stream = File.OpenRead(sourcePath))
        files = FileContainer.ReadAllFiles(stream);

    int[] indices = operation.InputIndices
        ?? files.Keys.Select(k => (int)k).OrderBy(k => k).ToArray();

    Dictionary<int, int[]>? paletteMap = null;
    foreach (var arg in operation.Arguments)
    {
        if (arg is IntMapArgument map)
        {
            paletteMap = map.Map;
            break;
        }
    }

    var uniquePalettes = new List<PaletteRgb>();
    var entries = new List<SpriteSheetEntry>();

    foreach (int idx in indices)
    {
        uint key = (uint)idx;
        if (!files.TryGetValue(key, out var data))
        {
            Console.Error.WriteLine($"    Warning: sprite index {idx} not found in container, skipping.");
            continue;
        }

        PaletteRgb[] palettes = ReadPalettes(data);

        int[] selectedPalettes = paletteMap != null && paletteMap.TryGetValue(idx, out var mapped)
            ? mapped
            : Enumerable.Range(0, palettes.Length).ToArray();

        var mappedIndices = new List<byte>();

        foreach (int palIdx in selectedPalettes)
        {
            if (palIdx < 0 || palIdx >= palettes.Length)
            {
                Console.Error.WriteLine($"    Warning: palette {palIdx} out of range for sprite {idx} (has {palettes.Length}), skipping.");
                continue;
            }
            mappedIndices.Add(GetOrAddPalette(uniquePalettes, palettes[palIdx]));
        }

        if (mappedIndices.Count > 0)
            entries.Add(new SpriteSheetEntry(key, [.. mappedIndices]));
    }

    var sheet = new SpriteSheet([.. uniquePalettes], [.. entries]);
    var writer = new DataWriter();
    sheet.Write(writer);

    string outputPath = context.ResolvePath(operation.OutputPath);
    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
    File.WriteAllBytes(outputPath, writer.ToArray());

    Console.WriteLine($"  SpriteSheet: {operation.OutputPath}  ({entries.Count} sprites, {uniquePalettes.Count} palettes)");
}

// ---- Sprite: select PNG ----

static void HandleSprite(BuildContext context, BuildOperation operation)
{
    string sourcePath = context.ResolvePath(operation.InputPath);
    using var image = (Bitmap)Image.FromFile(sourcePath);
    var sprite = BuildSprite(image);

    var writer = new DataWriter();
    sprite.Write(writer);

    string outputPath = context.ResolvePath(operation.OutputPath);
    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
    File.WriteAllBytes(outputPath, writer.ToArray());

    Console.WriteLine($"  Sprite: {operation.OutputPath}  ({sprite.Width}x{sprite.Height}, {sprite.ColorIndices.Length} color indices, {sprite.Colors.Length} colors)");
}

// ---- FontSprite: select PNG ----

static void HandleFontSprite(BuildContext context, BuildOperation operation)
{
    string sourcePath = context.ResolvePath(operation.InputPath);
    using var image = (Bitmap)Image.FromFile(sourcePath);
    var (alphaValues, width, height) = BuildFontSprite(image);

    var writer = new DataWriter();
    writer.Write((uint)width);
    writer.Write((uint)height);
    writer.Write(alphaValues);

    string outputPath = context.ResolvePath(operation.OutputPath);
    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
    File.WriteAllBytes(outputPath, writer.ToArray());

    Console.WriteLine($"  FontSprite: {operation.OutputPath}  ({width}x{height})");
}

// ---- Font: select font atlas + metrics ----

static void HandleFont(BuildContext context, BuildOperation operation)
{
    string sourcePath = context.ResolvePath(operation.InputPath);
    string metricPath = Path.Combine(Path.GetDirectoryName(sourcePath) ?? "", Path.GetFileNameWithoutExtension(sourcePath) + ".json");

    if (!File.Exists(metricPath))
    {
        Console.Error.WriteLine($"    Error: font metrics file \"{metricPath}\" not found, skipping.");
        return;
    }

    var reader = new DataReader(File.ReadAllBytes(sourcePath));
    uint width = reader.ReadDword();
    uint height = reader.ReadDword();
    byte[] alphaValues = reader.ReadBytes((int)(width * height));
    var metrics = JsonSerializer.Deserialize<FontMetrics>(File.ReadAllText(metricPath), jsonSerializerOptions)!;
    var glyphs = new FontGlyph[metrics.Characters.Length];

    for (int i = 0; i < metrics.Characters.Length; i++)
    {
        var character = metrics.Characters[i];
        var glyph = new FontGlyph(new(character.Character[0]), (byte)character.AdvanceWidth);

        glyphs[i] = glyph;
    }

    var font = new Font(width, height, alphaValues, (byte)metrics.CellWidth, (byte)metrics.CellHeight, glyphs);
    var writer = new DataWriter();
    font.Write(writer);

    string outputPath = context.ResolvePath(operation.OutputPath);
    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
    File.WriteAllBytes(outputPath, writer.ToArray());

    Console.WriteLine($"  Font: {operation.OutputPath}  ({glyphs.Length} glyphs, {width}x{height} atlas)");
}

// ---- Helpers ----

static Sprite BuildSprite(Bitmap bitmap)
{
    int width = bitmap.Width;
    int height = bitmap.Height;

    var data = bitmap.LockBits(new Rectangle(0, 0, width, height),
        ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
    var bgra = new byte[width * height * 4];
    Marshal.Copy(data.Scan0, bgra, 0, bgra.Length);
    bitmap.UnlockBits(data);

    var colors = new List<ColorRgb>();
    var colorIndices = new byte[width * height];
    int offset = 0;

    for (int i = 0; i < colorIndices.Length; i++)
    {
        byte b = bgra[offset];
        byte g = bgra[offset + 1];
        byte r = bgra[offset + 2];
        byte a = bgra[offset + 3];
        offset += 4;

        if (a == 0)
        {
            colorIndices[i] = 0;
            continue;
        }

        var color = new ColorRgb(r, g, b);
        int index = colors.IndexOf(color);

        if (index == -1)
        {
            if (colors.Count >= 255)
                throw new InvalidOperationException(
                    "The image uses more than 255 distinct colors and cannot be stored as a palette sprite.");

            colors.Add(color);
            colorIndices[i] = (byte)colors.Count; // 1-based
        }
        else
        {
            colorIndices[i] = (byte)(1 + index);
        }
    }

    return new Sprite((ushort)width, (ushort)height, colors.ToArray(), colorIndices);
}

static (byte[] AlphaValues, int Width, int Height) BuildFontSprite(Bitmap bitmap)
{
    int width = bitmap.Width;
    int height = bitmap.Height;

    var data = bitmap.LockBits(new Rectangle(0, 0, width, height),
        ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
    byte[] alpha = new byte[width * height];

    try
    {
        unsafe
        {
            for (int y = 0; y < height; y++)
            {
                byte* row = (byte*)data.Scan0 + y * data.Stride;

                for (int x = 0; x < width; x++)
                {
                    alpha[y * width + x] = row[x * 4 + 3];
                }
            }
        }
    }
    finally
    {
        bitmap.UnlockBits(data);
    }

    return (alpha, width, height);
}

static PaletteRgb[] ReadPalettes(byte[] data)
{
    var reader = new DataReader(data);
    reader.ReadWord(); // width
    reader.ReadWord(); // height

    byte firstByte = reader.PeekByte();

    if (firstByte > 0)
    {
        reader.Position = 4;
        reader.ReadByte(); // paletteCount
        byte colorCount = reader.ReadByte();

        if (colorCount > 0)
        {
            reader.Position = 0;
            var swp = SpriteWithPalettes.Read(reader);
            return swp.Palettes;
        }
    }

    reader.Position = 0;
    var sprite = Sprite.Read(reader);
    return [new PaletteRgb(sprite.Colors)];
}

static byte GetOrAddPalette(List<PaletteRgb> unique, PaletteRgb palette)
{
    for (int i = 0; i < unique.Count; i++)
    {
        if (PalettesEqual(unique[i], palette))
            return (byte)i;
    }
    byte idx = (byte)unique.Count;
    unique.Add(palette);
    return idx;
}

static bool PalettesEqual(PaletteRgb a, PaletteRgb b)
{
    if (a.Colors.Length != b.Colors.Length)
        return false;
    for (int i = 0; i < a.Colors.Length; i++)
    {
        if (a.Colors[i] != b.Colors[i])
            return false;
    }
    return true;
}

file record FontMetrics
{
    public record FontCharacter
    {
        public string Character { get; set; } = "";
        public int AdvanceWidth { get; set; }
    }

    public int CharCount { get; set; }
    public int CellWidth { get; set; }
    public int CellHeight { get; set; }
    public FontCharacter[] Characters { get; set; } = [];
}

partial class Program
{
    static readonly JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
}
