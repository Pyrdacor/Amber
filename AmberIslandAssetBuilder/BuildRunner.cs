using System.Text.RegularExpressions;

namespace AmberIslandAssetBuilder;

sealed class BuildContext(string assetsDir)
{
    public string AssetsDir { get; } = Path.GetFullPath(assetsDir);
    public HashSet<string> TempFiles { get; } = new(StringComparer.OrdinalIgnoreCase);

    public string ResolvePath(string relativePath) =>
        Path.GetFullPath(Path.Combine(AssetsDir, relativePath));
}

sealed class BuildRunner
{
    private readonly Dictionary<string, Action<BuildContext, BuildOperation>> handlers = new(StringComparer.OrdinalIgnoreCase);

    public void RegisterHandler(string operationType, Action<BuildContext, BuildOperation> handler) =>
        handlers[operationType] = handler;

    public void Run(BuildContext context, List<BuildOperation> operations)
    {
        var expanded = ExpandAllPlaceholders(context, operations);
        var sorted = SortByDependency(expanded);

        foreach (var op in sorted)
        {
            if (!handlers.TryGetValue(op.Type, out var handler))
                throw new InvalidOperationException($"No handler registered for operation type '{op.Type}'.");

            handler(context, op);

            if (op.IsTemporary)
                context.TempFiles.Add(context.ResolvePath(op.OutputPath));
        }
    }

    public static void CleanupTempFiles(BuildContext context)
    {
        foreach (var path in context.TempFiles)
        {
            if (!File.Exists(path))
                continue;

            try
            {
                File.Delete(path);
                Console.WriteLine($"  Cleaned up: {Path.GetFileName(path)}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  Warning: could not delete temp file {path}: {ex.Message}");
            }
        }
    }

    static List<BuildOperation> ExpandAllPlaceholders(BuildContext context, List<BuildOperation> operations)
    {
        var result = new List<BuildOperation>();

        foreach (var op in operations)
        {
            if (!op.InputPath.Contains("{0}"))
            {
                result.Add(op);
                continue;
            }

            var concrete = ExpandPlaceholder(context, op);

            if (concrete.Count == 0)
                concrete = ExpandPlaceholderFromOutputs(op, result);

            if (concrete.Count == 0)
                Console.Error.WriteLine($"  Warning: no matches for placeholder pattern '{op.InputPath}'");

            result.AddRange(concrete);
        }

        return result;
    }

    static List<BuildOperation> ExpandPlaceholderFromOutputs(BuildOperation operation, List<BuildOperation> expandedOps)
    {
        string inputPath = operation.InputPath.Replace('\\', '/');
        string[] segments = inputPath.Split('/');
        int phSegIdx = Array.FindIndex(segments, s => s.Contains("{0}"));
        if (phSegIdx < 0)
            return [];

        string segTemplate = segments[phSegIdx];
        string prefix = phSegIdx > 0 ? string.Join('/', segments[..phSegIdx]) + "/" : "";
        string suffix = phSegIdx < segments.Length - 1
            ? "/" + string.Join('/', segments[(phSegIdx + 1)..])
            : "";

        var result = new List<BuildOperation>();
        var seenValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var prevOp in expandedOps)
        {
            string output = prevOp.OutputPath.Replace('\\', '/');

            if (prefix.Length > 0 && !output.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;
            if (suffix.Length > 0 && !output.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                continue;

            string candidate = output;
            if (prefix.Length > 0)
                candidate = candidate[prefix.Length..];
            if (suffix.Length > 0)
                candidate = candidate[..^suffix.Length];

            if (candidate.Contains('/'))
                continue;

            string? value = ExtractPlaceholderValue(candidate, segTemplate);
            if (value != null && seenValues.Add(value))
            {
                result.Add(operation with
                {
                    InputPath = operation.InputPath.Replace("{0}", value),
                    OutputPath = operation.OutputPath.Replace("{0}", value)
                });
            }
        }

        return result;
    }

    static List<BuildOperation> ExpandPlaceholder(BuildContext context, BuildOperation operation)
    {
        string inputPath = operation.InputPath.Replace('\\', '/');
        string[] segments = inputPath.Split('/');

        int phSegIdx = Array.FindIndex(segments, s => s.Contains("{0}"));
        if (phSegIdx < 0)
            return [operation];

        string baseDir = phSegIdx > 0
            ? string.Join('/', segments[..phSegIdx])
            : ".";
        string fullBaseDir = context.ResolvePath(baseDir);

        if (!Directory.Exists(fullBaseDir))
            return [];

        string segPattern = segments[phSegIdx].Replace("{0}", "*");
        bool matchDirs = phSegIdx < segments.Length - 1;

        string[] matches = matchDirs
            ? Directory.GetDirectories(fullBaseDir, segPattern)
            : Directory.GetFiles(fullBaseDir, segPattern);

        var result = new List<BuildOperation>();

        foreach (var match in matches.OrderBy(m => m, StringComparer.OrdinalIgnoreCase))
        {
            string matchName = Path.GetFileName(match);
            string? value = ExtractPlaceholderValue(matchName, segments[phSegIdx]);

            if (value is null)
                continue;

            result.Add(operation with
            {
                InputPath = operation.InputPath.Replace("{0}", value),
                OutputPath = operation.OutputPath.Replace("{0}", value)
            });
        }

        return result;
    }

    static string? ExtractPlaceholderValue(string actual, string template)
    {
        int phIdx = template.IndexOf("{0}");
        string prefix = template[..phIdx];
        string suffix = template[(phIdx + 3)..];

        if (prefix.Length > 0 && !actual.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return null;
        if (suffix.Length > 0 && !actual.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            return null;

        int endIdx = actual.Length - suffix.Length;
        return actual[prefix.Length..endIdx];
    }

    static List<BuildOperation> SortByDependency(List<BuildOperation> operations)
    {
        var byOutput = new Dictionary<string, BuildOperation>(StringComparer.OrdinalIgnoreCase);
        foreach (var op in operations)
            byOutput[op.OutputPath] = op;

        var sorted = new List<BuildOperation>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Visit(BuildOperation op)
        {
            if (!visited.Add(op.OutputPath))
                return;

            if (byOutput.TryGetValue(op.InputPath, out var dep))
            {
                Visit(dep);
            }
            else if (op.InputPath.Contains('*') || op.InputPath.Contains('?'))
            {
                foreach (var entry in byOutput)
                {
                    if (GlobMatches(op.InputPath, entry.Key))
                        Visit(entry.Value);
                }
            }

            sorted.Add(op);
        }

        foreach (var op in operations)
            Visit(op);

        return sorted;
    }

    static bool GlobMatches(string pattern, string path)
    {
        pattern = pattern.Replace('\\', '/');
        path = path.Replace('\\', '/');
        string regex = "^" + Regex.Escape(pattern).Replace("\\*", "[^/]*").Replace("\\?", "[^/]") + "$";
        return Regex.IsMatch(path, regex, RegexOptions.IgnoreCase);
    }
}
