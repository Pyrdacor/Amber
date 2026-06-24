namespace AmberIsland.GameData;

public sealed class GameData : IDisposable
{
    private enum FileContainerType
    {
        PlayerGraphic,
        OutfitGraphic,
        TilesetGraphic,
        TilesetData,
        MapData,
        MonsterGraphic,
        MonsterData,
    }

    private readonly Dictionary<FileContainerType, Lazy<FileContainer>> containers = [];

    public GameData(string path)
    {
        void AddContainer(FileContainerType type, string filename)
        {
            containers.Add(type, new(() => FileContainer.Read(File.OpenRead(Path.Combine(path, filename)))));
        }

        AddContainer(FileContainerType.PlayerGraphic, "player.aifc");
        AddContainer(FileContainerType.OutfitGraphic, "outfit.aifc");
        AddContainer(FileContainerType.TilesetGraphic, "tileatlas.aifc");
        AddContainer(FileContainerType.TilesetData, "tileset.aifc");
        AddContainer(FileContainerType.MapData, "map.aifc");
        AddContainer(FileContainerType.MonsterGraphic, "monsteratlas.aifc");
        AddContainer(FileContainerType.MonsterData, "monster.aifc");
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
        return ProcessOneTimeFileContainer(FileContainerType.PlayerGraphic, container => SpriteWithPalettes.Read(container.GetFileReader(1)));
    }

    public SpriteWithPalettes GetOutfitSprite()
    {
        return ProcessOneTimeFileContainer(FileContainerType.OutfitGraphic, container => SpriteWithPalettes.Read(container.GetFileReader(1)));
    }

    public Sprite GetTilesetAtlasSprite()
    {
        return ProcessOneTimeFileContainer(FileContainerType.TilesetGraphic, container => Sprite.Read(container.GetFileReader(1)));
    }

    public Tileset GetTileset()
    {
        return ProcessOneTimeFileContainer(FileContainerType.TilesetData, container => Tileset.Read(container.GetFileReader(1)));
    }

    public Map GetMap()
    {
        return ProcessOneTimeFileContainer(FileContainerType.MapData, container => Map.Read(container.GetFileReader(1)));
    }

    public Sprite GetMonsterAtlasSprite()
    {
        return ProcessOneTimeFileContainer(FileContainerType.MonsterGraphic, container => Sprite.Read(container.GetFileReader(1)));
    }

    public Monster GetMonster()
    {
        return ProcessOneTimeFileContainer(FileContainerType.MonsterData, container => Monster.Read(container.GetFileReader(1)));
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
