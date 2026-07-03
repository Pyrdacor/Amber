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
runner.RegisterHandler("PlayerSpriteSheetDefinition", HandlePlayerSpriteSheetDefinition);
runner.RegisterHandler("MergePlayerSpriteSheetDefinitions", HandleMergePlayerSpriteSheetDefinitions);
runner.RegisterHandler("PlayerSpriteSheet", HandlePlayerSpriteSheet);

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

// ---- PlayerSpriteSheetDefinition: create player sprite sheet definition ----

static void HandlePlayerSpriteSheetDefinition(BuildContext context, BuildOperation operation)
{
    string outputPath = context.ResolvePath(operation.OutputPath);
    var playerState = Enum.Parse<PlayerState>(operation.InputPath);

    var definition = new PlayerSpriteSheetDefinition();

    if (operation.Arguments.Length >= 1 && operation.Arguments[0] is IntListArgument intList)
    {
        var stateSprites = new PlayerStateSprites(
            playerState,
            (ushort)intList.Values[1], // OffsetXInFrames
            (ushort)intList.Values[2], // OffsetYInFrames
            intList.Values.Skip(3).Select(i => (byte)i).ToArray(), // FrameIndices
            [] // PossiblePaletteIndices (not used here, added later)
        );
        definition.StateSprites = [stateSprites];
        definition.FilePrefixes = [intList.Values[0]]; // FilePrefix
    }
    else
    {
        Console.Error.WriteLine($"    Error: missing or invalid sprite sheet definition values.");
        Console.Error.WriteLine($"           Provide: [FilePrefix, OffsetXInFrames, OffsetYInFrames, FrameIndices...].");
        return;
    }

    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
    File.WriteAllText(outputPath, JsonSerializer.Serialize(definition, jsonSerializerOptions));
    Console.WriteLine($"  PlayerSpriteSheetDefinition: {operation.OutputPath}  ({definition.StateSprites.Length} state sprites)");
}

// ---- MergePlayerSpriteSheetDefinitions: merge player sprite sheet definitions into one ----

static void HandleMergePlayerSpriteSheetDefinitions(BuildContext context, BuildOperation operation)
{
    string sourcePath = context.ResolvePath(operation.InputPath);
    string sourceDirectory = Path.GetDirectoryName(sourcePath) ?? ".";
    string pattern = Path.GetFileName(sourcePath);
    var files = Directory.GetFiles(sourceDirectory, pattern)
        .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
        .ToList();

    var mergedDefinition = new PlayerSpriteSheetDefinition();

    foreach (var file in files)
    {
        var definition = JsonSerializer.Deserialize<PlayerSpriteSheetDefinition>(File.ReadAllText(file), jsonSerializerOptions)!;
        mergedDefinition.StateSprites = mergedDefinition.StateSprites
            .Concat(definition.StateSprites)
            .ToArray();
        mergedDefinition.FilePrefixes = mergedDefinition.FilePrefixes
            .Concat(definition.FilePrefixes)
            .ToArray();
    }

    string outputPath = context.ResolvePath(operation.OutputPath);
    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
    File.WriteAllText(outputPath, JsonSerializer.Serialize(mergedDefinition, jsonSerializerOptions));
    Console.WriteLine($"  MergePlayerSpriteSheetDefinitions: {operation.OutputPath}  ({mergedDefinition.StateSprites.Length} state sprites)");
}

// ---- PlayerSpriteSheet: select sprite sheet ----

static void HandlePlayerSpriteSheet(BuildContext context, BuildOperation operation)
{
    string sourcePath = context.ResolvePath(operation.InputPath);
    string sourceDirectory = Path.GetDirectoryName(sourcePath) ?? ".";
    string pattern = Path.GetFileName(sourcePath);
    var files = Directory.GetFiles(sourceDirectory, pattern)
        .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
        .ToList();

    if (operation.Arguments.Length >= 2 && operation.Arguments[0] is StringArgument defFilePath && operation.Arguments[1] is IntListArgument frameSize)
    {
        var defFileFullPath = context.ResolvePath(defFilePath.Value);

        if (!File.Exists(defFileFullPath))
        {
            Console.Error.WriteLine($"    Error: definition file \"{defFilePath.Value}\" not found.");
            return;
        }

        var sprites = new Dictionary<string, (Sprite Sprite, int Y)>();
        var palettes = new HashSet<PaletteRgb>();
        var usedPalettes = new Dictionary<string, HashSet<PaletteRgb>>();
        var stateSprites = new List<PlayerStateSprites>();
        int y = 0;

        foreach (var file in files)
        {
            // Example: char_a_p1_1out_boxr_v01.png
            // Becomes: 0_char_a_p1_1out_boxr_v01_outfit.aispr
            // The prefix is "0", the variant identifier is "boxr".
            // See PlayerStateSpriteVariant
            var fileParts = Path.GetFileNameWithoutExtension(file).Split('_');
            string prefixStr = fileParts[0];
            string variantIdentifier = fileParts[^3];
            string key = prefixStr + "_" + variantIdentifier;

            if (!PlayerStateSprites.VariantFileIdentifiers.TryGetValue(variantIdentifier, out var variant))
            {
                Console.Error.WriteLine($"    Error: Variant '{variantIdentifier}' is not known.");
                return;
            }
            
            var sprite = Sprite.Read(new DataReader(File.ReadAllBytes(file)));

            if (!sprites.ContainsKey(key))
            {
                sprites[key] = (sprite, y);
                y += sprite.Height;
            }

            var palette = new PaletteRgb(sprite.Colors);

            palettes.Add(palette);

            if (!usedPalettes.TryGetValue(key, out var usedPaletteSet))
            {
                usedPalettes[key] = [palette];
            }
            else
            {
                usedPaletteSet.Add(palette);
            }
        }

        var paletteList = palettes.ToList();
        var variants = new Dictionary<string, PlayerStateSpriteVariant>(usedPalettes.Count);

        foreach (var kvp in usedPalettes)
        {
            var variant = kvp.Key;
            var offset = (ushort)sprites[kvp.Key].Y; ;
            var paletteIndices = kvp.Value.Select(palette => (byte)paletteList.IndexOf(palette)).ToArray();
            variants[kvp.Key] = new PlayerStateSpriteVariant(0, offset, paletteIndices);
        }
        
        var definition = JsonSerializer.Deserialize<PlayerSpriteSheetDefinition>(File.ReadAllText(defFileFullPath), jsonSerializerOptions)!;
        int frameHeight = frameSize.Values[1];
        var groupedKeys = sprites.Keys.GroupBy(key => key.Split('_')[0]);

        foreach (var keyGroup in groupedKeys)
        {
            var key = keyGroup.Key;
            int prefixNumber = int.Parse(key);
            var sprite = sprites[keyGroup.Order().First()];

            var variantKeys = variants.Keys.Where(k => k.StartsWith(prefixNumber + "_")).ToList();
            var matchingVariants = keyGroup.ToDictionary(k => PlayerStateSprites.VariantFileIdentifiers[k.Split('_')[1]], k => variants[k]);

            for (int i = 0; i < definition.StateSprites.Length; i++)
            {
                var stateSprite = definition.StateSprites[i];

                if (definition.FilePrefixes[i] == prefixNumber)
                {
                    stateSprites.Add(stateSprite with { OffsetY = (ushort)(sprite.Y / frameHeight + stateSprite.OffsetY), Variants = matchingVariants });
                }
            }
        }

        string outputPath = context.ResolvePath(operation.OutputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        int atlasWidth = sprites.Values.Max(s => s.Sprite.Width);
        int atlasHeight = sprites.Values.Sum(s => s.Sprite.Height);
        var atlasColorIndices = new byte[atlasWidth * atlasHeight];

        // Build atlas data
        foreach (var sprite in sprites)
        {
            int offsetY = sprite.Value.Y;

            for (y = 0; y < sprite.Value.Sprite.Height; y++)
            {
                Buffer.BlockCopy
                (
                    src: sprite.Value.Sprite.ColorIndices,
                    srcOffset: y * sprite.Value.Sprite.Width,
                    dst: atlasColorIndices,
                    dstOffset: (offsetY + y) * atlasWidth,
                    count: sprite.Value.Sprite.Width
                );
            }
        }

        var atlas = new SpriteWithPalettes((ushort)atlasWidth, (ushort)atlasHeight, paletteList.ToArray(), atlasColorIndices);
        var playerSpriteSheet = new PlayerSpriteSheet(stateSprites.ToArray(), atlas);
        var writer = new DataWriter();
        playerSpriteSheet.Write(writer);
        File.WriteAllBytes(outputPath, writer.ToArray());

        Console.WriteLine($"  PlayerSpriteSheet: {operation.OutputPath}  ({atlas.Width}x{atlas.Height}, {atlas.ColorIndices.Length} color indices, {atlas.Palettes.Length} palettes)");
    }
    else
    {
        Console.Error.WriteLine($"    Error: missing or invalid definition file path or frame size.");
        Console.Error.WriteLine($"           Provide: \"DefinitionFilePath\", [FrameWidth, FrameHeight].");
        return;
    }
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

file record PlayerSpriteSheetDefinition
{
    public PlayerStateSprites[] StateSprites { get; set; } = [];
    public int[] FilePrefixes { get; set; } = [];
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
