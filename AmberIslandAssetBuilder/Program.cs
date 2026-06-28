using Amber.IO.FileFormats.Serialization;
using AmberIsland.GameData;
using AmberIslandAssetBuilder;

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
// Add new handlers here by calling runner.RegisterHandler("OperationType", MyHandler).
runner.RegisterHandler("Container", HandleContainer);
runner.RegisterHandler("MapSpriteSheet", HandleMapSpriteSheet);

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

    foreach (var filePath in matchingFiles)
    {
        string name = Path.GetFileNameWithoutExtension(filePath);
        string indexStr = name.Split('_')[0];

        if (!uint.TryParse(indexStr, out uint index))
            throw new FormatException($"Cannot parse file index from '{Path.GetFileName(filePath)}'. Expected format: 001_name.ext");

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

// ---- Helpers ----

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
