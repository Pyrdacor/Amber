using Amber.IO.Common.Serialization;
using Amber.IO.FileFormats.Serialization;

namespace AmberIsland.GameData;

public sealed class GameData
{
    // Tileset-based (multiple tilesets per map possible)
    const int TileGraphicCacheSize = 10;
    const int TileDataCacheSize = 10;
    // Map-based
    const int MapDataCacheSize = 4;
    const int MonsterGraphicCacheSize = 4; // Those are per map!
    const int ProjectileGraphicCacheSize = 4; // And those
    // Actor-based
    const int MonsterDataCacheSize = 32;
    const int MonsterAnimationCacheSize = 32;
    const int ProjectileDataCacheSize = 32;
    const int ProjectileAnimationCacheSize = 32;

    // Non-cached assets
    private readonly PlayerSpriteSheet playerGraphics;
    private readonly PlayerSpriteSheet outfitGraphics;
    private readonly Dictionary<uint, Font> fonts;
    private readonly PaletteRgb textPalette;

    // Cached assets
    private readonly AssetCache<Sprite> tilesetGraphicCache;
    private readonly AssetCache<Tileset> tilesetDataCache;
    private readonly AssetCache<Map> mapDataCache;
    private readonly AssetCache<MapSpriteAtlas> monsterGraphicCache;
    private readonly AssetCache<Monster> monsterDataCache;
    private readonly AssetCache<FileContainer> monsterAnimationCache;
    private readonly AssetCache<MapSpriteAtlas> projectileGraphicCache;
    private readonly AssetCache<Projectile> projectileDataCache;
    private readonly AssetCache<FileContainer> projectileAnimationCache;

    public GameData(string path)
    {
        string Full(string filename) => Path.Combine(path, filename);

        // Non-cached
        var playerSheets = ReadAllContainerFiles(Full("player.aic"), PlayerSpriteSheet.Read);
        playerGraphics = playerSheets[1];
        outfitGraphics = playerSheets[2];
        fonts = ReadAllContainerFiles(Full("fonts.aic"), Font.Read);
        textPalette = PaletteRgb.Read(new DataReader(File.ReadAllBytes(Full("text_palette.aipal"))));

        // Cached
        tilesetGraphicCache = new(Full("tileatlas.aic"), TileGraphicCacheSize, Sprite.Read);
        tilesetDataCache = new(Full("tileset.aic"), TileDataCacheSize, Tileset.Read);
        mapDataCache = new(Full("map.aic"), MapDataCacheSize, Map.Read);
        monsterGraphicCache = new(Full("mon_mss.aic"), MonsterGraphicCacheSize, reader => ReadMapSpriteAtlas(reader, Full("mon_sprites.aic")));
        monsterDataCache = new(Full("mon_data.aic"), MonsterDataCacheSize, Monster.Read);
        monsterAnimationCache = new(Full("mon_anim.aic"), MonsterAnimationCacheSize, FileContainer.Read);
        projectileGraphicCache = new(Full("proj_atlas.aic"), ProjectileGraphicCacheSize, MapSpriteAtlas.Read);
        projectileDataCache = new(Full("proj_data.aic"), ProjectileDataCacheSize, Projectile.Read);
        projectileAnimationCache = new(Full("proj_anim.aic"), ProjectileAnimationCacheSize, FileContainer.Read);
        // TODO ...
    }

    private static MapSpriteAtlas ReadMapSpriteAtlas(IDataReader spriteSheetReader, string spriteContainerPath)
    {
        var spriteSheet = SpriteSheet.Read(spriteSheetReader);
        using var stream = File.OpenRead(spriteContainerPath);

        var spriteFiles = FileContainer.ReadFiles(stream, spriteSheet.Entries.Select(entry => entry.SpriteIndex).ToArray());
        var sprites = spriteFiles.ToDictionary(file => file.Key, file => Sprite.Read(new DataReader(file.Value)));
        
        return MapSpriteAtlas.FromSpriteSheet(spriteSheet, sprites);
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
    public PlayerSpriteSheet GetPlayerSpriteSheet() => playerGraphics;

    public PlayerSpriteSheet GetOutfitSpriteSheet() => outfitGraphics;

    public Dictionary<uint, Font> GetFonts() => fonts;

    public PaletteRgb GetTextPalette() => textPalette;


    // Cached assets
    public Sprite GetTilesetAtlasSprite(uint index) => tilesetGraphicCache.LoadAsset(index);

    public Tileset? GetTileset(uint index) => tilesetDataCache.LoadAsset(index);

    public Map? GetMap(uint index) => mapDataCache.LoadAsset(index);

    public MapSpriteAtlas GetMonsterAtlasSprites(uint mapIndex) => monsterGraphicCache.LoadAsset(mapIndex);

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

    public MapSpriteAtlas GetProjectileAtlasSprites(uint mapIndex) => projectileGraphicCache.LoadAsset(mapIndex);

    public Projectile GetProjectile(uint index) => projectileDataCache.LoadAsset(index);

    public Dictionary<ProjectileState, Animation> GetProjectileAnimations(uint index)
    {
        return projectileAnimationCache.LoadAsset(index)?
            .GetAllFileReaders()
            .ToDictionary(
                file => (ProjectileState)(file.Key - 1),
                file =>
                {
                    file.Value.Position = 0;
                    return Animation.Read(file.Value);
                }) ?? [];
    }
}
