using Amber.IO.FileFormats.Serialization;
using AmberIsland.GameData;

if (args.Length < 2)
{
    Console.WriteLine("Usage: AmberIslandActorAtlasMerger <source.aic> <output>");
    Console.WriteLine();
    Console.WriteLine("  source.aic  Sprite container (Sprite or SpriteWithPalettes files)");
    Console.WriteLine("  output      Output file path for the sprite sheet");
    return 1;
}

string sourcePath = args[0];
string outputPath = args[1];

if (!File.Exists(sourcePath))
{
    Console.Error.WriteLine($"File not found: {sourcePath}");
    return 1;
}

// ---- Load container ----

Dictionary<uint, byte[]> files;
using (var stream = File.OpenRead(sourcePath))
    files = FileContainer.ReadAllFiles(stream);

if (files.Count == 0)
{
    Console.Error.WriteLine("Container is empty.");
    return 1;
}

Console.WriteLine($"Loaded: {sourcePath}  ({files.Count} files)");
Console.WriteLine();

// ---- Choose format ----

Console.Write("Format: [1] Sprite (embedded palette), [2] SpriteWithPalettes: ");
string? formatInput = Console.ReadLine()?.Trim();
bool useSpriteWithPalettes = formatInput == "2";

// ---- Parse all sprites ----

var spriteInfos = new SortedDictionary<uint, SpriteInfo>();

foreach (var (index, data) in files)
{
    try
    {
        var reader = new DataReader(data);

        if (useSpriteWithPalettes)
        {
            var swp = SpriteWithPalettes.Read(reader);
            spriteInfos[index] = new SpriteInfo(swp.Width, swp.Height, swp.Palettes);
        }
        else
        {
            var sprite = Sprite.Read(reader);
            var palette = new PaletteRgb(sprite.Colors);
            spriteInfos[index] = new SpriteInfo(sprite.Width, sprite.Height, [palette]);
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"  Warning: could not read index {index}: {ex.Message}");
    }
}

if (spriteInfos.Count == 0)
{
    Console.Error.WriteLine("No readable sprites found.");
    return 1;
}

// ---- Display sprites ----

Console.WriteLine();
Console.WriteLine($"{"Index",6}  {"Size",10}  Palettes");
Console.WriteLine(new string('-', 36));

foreach (var (index, info) in spriteInfos)
{
    string size = $"{info.Width}x{info.Height}";
    Console.WriteLine($"{index,6}  {size,10}  {info.Palettes.Length}");
}

Console.WriteLine();

// ---- Select sprites ----

Console.Write($"Include which indices? (e.g., 1,3-5,8 or 'all') [all]: ");
string? selectionInput = Console.ReadLine()?.Trim();

HashSet<uint> selectedIndices;

if (string.IsNullOrEmpty(selectionInput) || selectionInput.Equals("all", StringComparison.OrdinalIgnoreCase))
{
    selectedIndices = [.. spriteInfos.Keys];
}
else
{
    selectedIndices = ParseIndices(selectionInput, spriteInfos.Keys);
}

if (selectedIndices.Count == 0)
{
    Console.Error.WriteLine("No sprites selected.");
    return 1;
}

Console.WriteLine($"{selectedIndices.Count} sprite(s) selected.");
Console.WriteLine();

// ---- Per-sprite palette selection ----

var selectedPalettes = new Dictionary<uint, List<int>>();

foreach (var index in selectedIndices.Order())
{
    var info = spriteInfos[index];

    if (info.Palettes.Length <= 1)
    {
        selectedPalettes[index] = [0];
        continue;
    }

    Console.Write($"Index {index}: {info.Palettes.Length} palettes (0-{info.Palettes.Length - 1}) — exclude any? [none]: ");
    string? excludeInput = Console.ReadLine()?.Trim();

    var all = Enumerable.Range(0, info.Palettes.Length).ToList();

    if (!string.IsNullOrEmpty(excludeInput))
    {
        var excluded = ParseIntSet(excludeInput);
        all.RemoveAll(p => excluded.Contains(p));
    }

    if (all.Count == 0)
    {
        Console.WriteLine($"  All palettes excluded — removing sprite {index}.");
        continue;
    }

    selectedPalettes[index] = all;
}

if (selectedPalettes.Count == 0)
{
    Console.Error.WriteLine("No sprites remain after palette selection.");
    return 1;
}

// ---- Deduplicate palettes ----

var uniquePalettes = new List<PaletteRgb>();
var paletteMap = new Dictionary<int, byte>(); // hash → index into uniquePalettes

byte GetOrAddPalette(PaletteRgb palette)
{
    int hash = PaletteHash(palette);

    if (paletteMap.TryGetValue(hash, out byte existing))
    {
        if (PalettesEqual(uniquePalettes[existing], palette))
            return existing;
    }

    // Hash collision or new palette — linear scan for exact match
    for (int i = 0; i < uniquePalettes.Count; i++)
    {
        if (PalettesEqual(uniquePalettes[i], palette))
            return (byte)i;
    }

    byte idx = (byte)uniquePalettes.Count;
    uniquePalettes.Add(palette);
    paletteMap[hash] = idx;
    return idx;
}

// ---- Build entries ----

var entries = new List<SpriteSheetEntry>();

foreach (var (index, palIndices) in selectedPalettes.OrderBy(kv => kv.Key))
{
    var info = spriteInfos[index];
    var mappedIndices = palIndices.Select(p => GetOrAddPalette(info.Palettes[p])).ToArray();
    entries.Add(new SpriteSheetEntry(index, mappedIndices));
}

// ---- Save ----

var sheet = new SpriteSheet([.. uniquePalettes], [.. entries]);
var writer = new DataWriter();
sheet.Write(writer);
File.WriteAllBytes(outputPath, writer.ToArray());

Console.WriteLine();
Console.WriteLine($"Sprite sheet saved: {outputPath}");
Console.WriteLine($"  {entries.Count} sprite(s), {uniquePalettes.Count} unique palette(s)");

return 0;

// ---- Helpers ----

static HashSet<uint> ParseIndices(string input, IEnumerable<uint> validIndices)
{
    var valid = new HashSet<uint>(validIndices);
    var result = new HashSet<uint>();

    foreach (var part in input.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
    {
        if (part.Contains('-'))
        {
            var range = part.Split('-', 2);
            if (uint.TryParse(range[0], out uint from) && uint.TryParse(range[1], out uint to))
            {
                for (uint i = from; i <= to; i++)
                {
                    if (valid.Contains(i))
                        result.Add(i);
                }
            }
        }
        else if (uint.TryParse(part, out uint idx))
        {
            if (valid.Contains(idx))
                result.Add(idx);
        }
    }

    return result;
}

static HashSet<int> ParseIntSet(string input)
{
    var result = new HashSet<int>();

    foreach (var part in input.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
    {
        if (int.TryParse(part, out int val))
            result.Add(val);
    }

    return result;
}

static int PaletteHash(PaletteRgb palette)
{
    var hash = new HashCode();
    hash.Add(palette.Colors.Length);
    foreach (var c in palette.Colors)
    {
        hash.Add(c.R);
        hash.Add(c.G);
        hash.Add(c.B);
    }
    return hash.ToHashCode();
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

record SpriteInfo(ushort Width, ushort Height, PaletteRgb[] Palettes);
