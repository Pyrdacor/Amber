namespace AmberIsland.GameData;

public sealed class GameData : IDisposable
{
    private enum FileContainerType
    {
        Player,
    }

    private readonly Dictionary<FileContainerType, Lazy<FileContainer>> containers = [];

    public GameData(string path)
    {
        void AddContainer(FileContainerType type, string filename)
        {
            containers.Add(type, new(() => FileContainer.Read(File.OpenRead(Path.Combine(path, filename)))));
        }

        AddContainer(FileContainerType.Player, "player.aifc");
        // TODO ...
    }

    private T ProcessOneTimeFileContainer<T>(FileContainerType type, Func<FileContainer, T> processAction)
    {
        var container = containers[type].Value;
        var result = processAction(container);
        DisposeFileContainer(type);
        return result;
    }

    public SpriteWithPalettes GetPlayerSprite()
    {
        return ProcessOneTimeFileContainer(FileContainerType.Player, container => SpriteWithPalettes.Read(container.GetFileReader(1)));
    }

    private void DisposeFileContainer(FileContainerType type)
    {
        if (containers.TryGetValue(type, out var container))
        {
            if (container.IsValueCreated)
                container.Value.Dispose();

            containers.Remove(type);
        }
    }

    private void DisposeAllFileContainers()
    {
        foreach (var container in containers.Values)
        {
            if (container.IsValueCreated)
                container.Value.Dispose();
        }

        containers.Clear();
    }

    public void Dispose()
    {
        DisposeAllFileContainers();
    }
}
