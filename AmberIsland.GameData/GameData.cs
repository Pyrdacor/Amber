using Amber.IO.Common.Serialization;
using Amber.IO.FileFormats.Serialization;

namespace AmberIsland.GameData;

public sealed class GameData
{
    const int TileGraphicCacheSize = 10;
    const int TileDataCacheSize = 10;
    const int MapDataCacheSize = 4;
    const int MonsterGraphicCacheSize = 20; // We might have several monsters on the same map (adjust if needed)
    const int MonsterDataCacheSize = 20;
    const int MonsterAnimationCacheSize = 20;

    // Non-cached assets
    private readonly SpriteWithPalettes playerGraphic;
    private readonly Dictionary<uint, SpriteWithPalettes> outfitGraphics;

    // Cached assets
    private readonly AssetCache<Sprite> tilesetGraphicCache;
    private readonly AssetCache<Tileset> tilesetDataCache;
    private readonly AssetCache<Map> mapDataCache;
    private readonly AssetCache<Sprite> monsterGraphicCache;
    private readonly AssetCache<Monster> monsterDataCache;
    private readonly AssetCache<FileContainer> monsterAnimationCache;

    public GameData(string path)
    {
        string Full(string filename) => Path.Combine(path, filename);

        // Non-cached
        playerGraphic = ReadSingleContainerFile(Full("player.aic"), 1, SpriteWithPalettes.Read);
        outfitGraphics = ReadAllContainerFiles(Full("outfit.aic"), SpriteWithPalettes.Read);

        // Cached
        tilesetGraphicCache = new(Full("tileatlas.aic"), TileGraphicCacheSize, Sprite.Read);
        tilesetDataCache = new(Full("tileset.aic"), TileDataCacheSize, Tileset.Read);
        mapDataCache = new(Full("map.aic"), MapDataCacheSize, Map.Read);
        monsterGraphicCache = new(Full("mon_atlas.aic"), MonsterGraphicCacheSize, Sprite.Read);
        monsterDataCache = new(Full("mon_data.aic"), MonsterDataCacheSize, Monster.Read);
        monsterAnimationCache = new(Full("mon_anim.aic"), MonsterAnimationCacheSize, FileContainer.Read);

        // TODO ...
    }

    private static T? ReadSingleContainerFile<T>(string containerPath, uint index, Func<IDataReader, T> assetLoader)
    {
        var file = FileContainer.ReadFiles(containerPath, index).FirstOrDefault().Value;

        if (file == null)
            return default;

        return assetLoader(new DataReader(file));
    }

    private static Dictionary<uint, T> ReadAllContainerFiles<T>(string containerPath, Func<IDataReader, T> assetLoader)
    {
        var files = FileContainer.ReadAllFiles(containerPath);

        if (files.Count == 0)
            return [];

        return files.ToDictionary(file => file.Key, file => assetLoader(new DataReader(file.Value)));
    }

    // Non-cached assets
    public SpriteWithPalettes GetPlayerSprite() => playerGraphic;

    public SpriteWithPalettes GetOutfitSprite(uint index) => outfitGraphics.GetValueOrDefault(index);


    // Cached assets
    public Sprite GetTilesetAtlasSprite(uint index) => tilesetGraphicCache.LoadAsset(index);

    public Tileset? GetTileset(uint index) => tilesetDataCache.LoadAsset(index);

    public Map? GetMap(uint index) => mapDataCache.LoadAsset(index);

    public Dictionary<uint, Sprite> GetMonsterAtlasSprites() => monsterGraphicCache.LoadAllAssets();

    public Monster GetMonster(uint index) => monsterDataCache.LoadAsset(index);

    public Dictionary<MonsterState, Animation> GetMonsterAnimations(uint index)
    {
        return monsterAnimationCache.LoadAsset(index)?
            .GetAllFileReaders()
            .ToDictionary(
                file => (MonsterState)(file.Key - 1),
                file =>
                {
                    file.Value.Position = 0;
                    return Animation.Read(file.Value);
                }) ?? [];
    }
}
