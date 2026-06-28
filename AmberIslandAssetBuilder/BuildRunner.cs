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
        var sorted = SortByDependency(operations);

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
                Visit(dep);

            sorted.Add(op);
        }

        foreach (var op in operations)
            Visit(op);

        return sorted;
    }
}
