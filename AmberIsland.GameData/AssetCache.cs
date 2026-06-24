using Amber.IO.Common.Serialization;
using Amber.IO.FileFormats.Serialization;

namespace AmberIsland.GameData;

public interface IAssetCache<T> where T : notnull
{
    T? LoadAsset(uint fileIndex);
    Dictionary<uint, T> LoadAssets(params uint[] fileIndices);
}

internal class AssetCache<T> : IAssetCache<T> where T : notnull
{
    private readonly string? containerPath = null;
    private readonly Func<uint[], Dictionary<uint, IDataReader>> fileLoader;
    private readonly int maxCacheEntries;
    private readonly Func<IDataReader, T> assetLoader;
    private readonly Dictionary<uint, LinkedListNode<(uint Index, T Asset)>> cacheMap;
    private readonly LinkedList<(uint Index, T Asset)> lruList = new();

    public AssetCache(string containerPath, int maxCacheEntries, Func<IDataReader, T> assetLoader)
    {
        this.containerPath = containerPath;
        this.maxCacheEntries = maxCacheEntries;
        this.assetLoader = assetLoader;
        cacheMap = new(maxCacheEntries);

        fileLoader = LoadFilesFromContainer;
    }

    private protected AssetCache(Func<uint[], Dictionary<uint, IDataReader>> fileLoader, int maxCacheEntries, Func<IDataReader, T> assetLoader)
    {
        this.fileLoader = fileLoader;
        this.maxCacheEntries = maxCacheEntries;
        this.assetLoader = assetLoader;
        cacheMap = new(maxCacheEntries);
    }

    private Dictionary<uint, IDataReader> LoadFilesFromContainer(params uint[] fileIndices)
    {
        if (fileIndices == null || fileIndices.Length == 0)
            return [];

        using var stream = File.OpenRead(containerPath!);
        var result = new Dictionary<uint, IDataReader>(fileIndices.Length);
        var files = FileContainer.ReadFiles(stream, fileIndices);

        foreach (var fileIndex in fileIndices)
        {
            if (files.TryGetValue(fileIndex, out var data))
                result.Add(fileIndex, new DataReader(data));
        }

        return result;
    }

    public T? LoadAsset(uint fileIndex)
    {
        if (cacheMap.TryGetValue(fileIndex, out var node))
        {
            lruList.Remove(node);
            lruList.AddFirst(node);

            return node.Value.Asset;
        }

        var files = fileLoader([fileIndex]);

        if (files == null || files.Count == 0)
            return default;

        if (cacheMap.Count >= maxCacheEntries)
        {
            var lastNode = lruList.Last;

            if (lastNode != null)
            {
                cacheMap.Remove(lastNode.Value.Index);
                lruList.RemoveLast();
            }
        }

        var asset = assetLoader(files.First().Value);
        var newNode = new LinkedListNode<(uint Index, T Asset)>((fileIndex, asset));
        lruList.AddFirst(newNode);
        cacheMap[fileIndex] = newNode;

        return asset;
    }

    public Dictionary<uint, T> LoadAssets(params uint[] fileIndices)
    {
        if (fileIndices == null || fileIndices.Length == 0)
            return [];

        var result = new Dictionary<uint, T>(fileIndices.Length);
        var indices = new List<uint>(fileIndices);

        foreach (var fileIndex in fileIndices)
        {
            if (cacheMap.TryGetValue(fileIndex, out var node))
            {
                lruList.Remove(node);
                lruList.AddFirst(node);

                result.Add(fileIndex, node.Value.Asset);
                indices.Remove(fileIndex);
            }
        }

        if (indices.Count == 0)
            return result;

        var files = fileLoader([.. indices]);

        foreach (var file in files)
        {
            if (cacheMap.Count >= maxCacheEntries)
            {
                var lastNode = lruList.Last;

                if (lastNode != null)
                {
                    cacheMap.Remove(lastNode.Value.Index);
                    lruList.RemoveLast();
                }
            }

            var asset = assetLoader(file.Value);
            var newNode = new LinkedListNode<(uint Index, T Asset)>((file.Key, asset));
            lruList.AddFirst(newNode);
            cacheMap[file.Key] = newNode;

            result.Add(file.Key, asset);
        }

        return result;
    }
}
