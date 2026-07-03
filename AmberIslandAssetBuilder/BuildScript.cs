namespace AmberIslandAssetBuilder;

// ---- Parsed types ----

abstract record BuildArgument;
record StringArgument(string Value) : BuildArgument;
record IntListArgument(int[] Values) : BuildArgument;
record IntMapArgument(Dictionary<int, int[]> Map) : BuildArgument;

record BuildOperation(
    string Type,
    bool IsTemporary,
    string OutputPath,
    string InputPath,
    int[]? InputIndices,
    BuildArgument[] Arguments);

// ---- Parser ----

static class BuildScriptParser
{
    public static List<BuildOperation> Parse(string text)
    {
        var operations = new List<BuildOperation>();
        string? currentType = null;
        int lineNumber = 0;

        foreach (var rawLine in text.Split('\n'))
        {
            lineNumber++;
            var line = rawLine.Trim();

            if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
                continue;

            if (line.StartsWith('#'))
            {
                currentType = line[1..].Trim();
                continue;
            }

            if (currentType is null)
                throw new FormatException($"Line {lineNumber}: operation before any # section.");

            try
            {
                operations.Add(ParseOperation(currentType, line));
            }
            catch (Exception ex)
            {
                throw new FormatException($"Line {lineNumber}: {ex.Message}", ex);
            }
        }

        return operations;
    }

    static BuildOperation ParseOperation(string type, string line)
    {
        int pos = 0;

        bool isTemp = Eat(line, ref pos, '@');
        string outputPath = ReadQuotedString(line, ref pos);

        SkipWhitespace(line, ref pos);
        Expect(line, ref pos, "<-");
        SkipWhitespace(line, ref pos);

        string inputPath = ReadQuotedString(line, ref pos);

        int[]? inputIndices = null;
        if (pos < line.Length && line[pos] == '[')
        {
            int close = FindClosing(line, pos, '[', ']');
            inputIndices = ParseIntRanges(line[(pos + 1)..close]);
            pos = close + 1;
        }

        var arguments = new List<BuildArgument>();
        SkipWhitespace(line, ref pos);

        while (pos < line.Length && line[pos] == ',')
        {
            pos++;
            SkipWhitespace(line, ref pos);

            if (pos >= line.Length)
                break;

            if (line[pos] == '{')
            {
                int close = FindClosing(line, pos, '{', '}');
                arguments.Add(new IntMapArgument(ParseIntMap(line[(pos + 1)..close])));
                pos = close + 1;
            }
            else if (line[pos] == '[')
            {
                int close = FindClosing(line, pos, '[', ']');
                arguments.Add(new IntListArgument(ParseIntRanges(line[(pos + 1)..close])));
                pos = close + 1;
            }
            else if (line[pos] == '"')
            {
                int close = line.IndexOf('"', pos + 1);
                arguments.Add(new StringArgument(line[(pos + 1)..close]));
                pos = close + 1;
            }

            SkipWhitespace(line, ref pos);
        }

        return new BuildOperation(type, isTemp, outputPath, inputPath, inputIndices, [.. arguments]);
    }

    // ---- Int ranges: "2,5-7,9" → [2,5,6,7,9] ----

    public static int[] ParseIntRanges(string text)
    {
        var result = new List<int>();

        foreach (var part in text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            int dash = part.IndexOf('-');
            if (dash > 0 && dash < part.Length - 1)
            {
                int from = int.Parse(part[..dash].Trim());
                int to = int.Parse(part[(dash + 1)..].Trim());
                for (int i = from; i <= to; i++)
                    result.Add(i);
            }
            else
            {
                result.Add(int.Parse(part));
            }
        }

        return [.. result];
    }

    // ---- Int map: "2:[0-3], 5:0, 6-9: 1" ----

    static Dictionary<int, int[]> ParseIntMap(string text)
    {
        var result = new Dictionary<int, int[]>();

        foreach (var entry in SplitTopLevel(text, ','))
        {
            int colon = entry.IndexOf(':');
            if (colon < 0)
                throw new FormatException($"Expected ':' in map entry: {entry}");

            string keyPart = entry[..colon].Trim();
            string valuePart = entry[(colon + 1)..].Trim();

            if (valuePart.StartsWith('[') && valuePart.EndsWith(']'))
                valuePart = valuePart[1..^1];

            int[] keys = ParseIntRanges(keyPart);
            int[] values = ParseIntRanges(valuePart);

            foreach (int key in keys)
                result[key] = values;
        }

        return result;
    }

    // ---- Low-level helpers ----

    static List<string> SplitTopLevel(string text, char separator)
    {
        var parts = new List<string>();
        int depth = 0;
        int start = 0;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c is '[' or '{') depth++;
            else if (c is ']' or '}') depth--;
            else if (c == separator && depth == 0)
            {
                parts.Add(text[start..i].Trim());
                start = i + 1;
            }
        }

        if (start < text.Length)
            parts.Add(text[start..].Trim());

        return parts;
    }

    static int FindClosing(string text, int openPos, char open, char close)
    {
        int depth = 1;
        for (int i = openPos + 1; i < text.Length; i++)
        {
            if (text[i] == open) depth++;
            else if (text[i] == close && --depth == 0) return i;
        }
        throw new FormatException($"Unmatched '{open}' at position {openPos}.");
    }

    static string ReadQuotedString(string text, ref int pos)
    {
        SkipWhitespace(text, ref pos);
        if (pos >= text.Length || text[pos] != '"')
            throw new FormatException($"Expected '\"' at position {pos}.");
        int start = ++pos;
        while (pos < text.Length && text[pos] != '"')
            pos++;
        if (pos >= text.Length)
            throw new FormatException("Unterminated string.");
        string value = text[start..pos];
        pos++;
        return value;
    }

    static bool Eat(string text, ref int pos, char c)
    {
        SkipWhitespace(text, ref pos);
        if (pos < text.Length && text[pos] == c) { pos++; return true; }
        return false;
    }

    static void Expect(string text, ref int pos, string expected)
    {
        if (!text.AsSpan(pos).StartsWith(expected))
            throw new FormatException($"Expected '{expected}' at position {pos}.");
        pos += expected.Length;
    }

    static void SkipWhitespace(string text, ref int pos)
    {
        while (pos < text.Length && char.IsWhiteSpace(text[pos]))
            pos++;
    }
}
