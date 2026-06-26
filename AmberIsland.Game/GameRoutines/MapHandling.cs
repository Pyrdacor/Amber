using AmberIsland.GameData;

namespace AmberIsland.Game;

partial class Game
{
    public Tile GetTileInfo(Map map, int x, int y)
    {
        int mapTileIndex = x + y * map.Width;
        Tile? tile;

        int backgroundTileIndex = map.BackgroundLayer[mapTileIndex];
        int objectTileIndex = map.ObjectLayer[mapTileIndex];
        int foregroundTileIndex = map.ForegroundLayer[mapTileIndex];

        if (foregroundTileIndex != 0)
        {
            tile = gameData.GetTileset(map.ForegroundTilesetIndex)?.Tiles[foregroundTileIndex - 1];

            if (tile != null)
            {
                if (!tile.Value.Flags.HasFlag(TileFlags.UseLowerLayerFlags))
                    return tile.Value;

                if (objectTileIndex == 0 && backgroundTileIndex == 0)
                    return tile.Value;
            }
        }

        if (objectTileIndex != 0)
        {
            tile = gameData.GetTileset(map.ObjectTilesetIndex)?.Tiles[objectTileIndex - 1];

            if (tile != null)
            {
                if (!tile.Value.Flags.HasFlag(TileFlags.UseLowerLayerFlags))
                    return tile.Value;

                if (backgroundTileIndex == 0)
                    return tile.Value;
            }
        }

        tile = gameData.GetTileset(map.BackgroundTilesetIndex)?.Tiles[backgroundTileIndex - 1];

        return tile ?? default;
    }

    public bool IsTileBlocking(Map map, int x, int y, ActorType actorType, TravelType travelType)
    {
        const byte allowAllTravel = 0x00;
        const byte blockAllTravel = 0xff;

        if (x < 0 || y < 0 || x >= map.Width || y >= map.Height)
            return true;

        var tileInfo = GetTileInfo(map, x, y);

        byte collisionMask = actorType switch
        {
            ActorType.Player => tileInfo.Flags.HasFlag(TileFlags.IgnorePlayerBlocking) ? allowAllTravel : tileInfo.BlockedTravel,
            // Items use same logic as player, as items have to be picked up by the player.
            ActorType.Item => tileInfo.Flags.HasFlag(TileFlags.IgnorePlayerBlocking) ? allowAllTravel : tileInfo.BlockedTravel,
            ActorType.Monster => tileInfo.Flags.HasFlag(TileFlags.IgnoreMonsterBlocking) ? allowAllTravel : tileInfo.BlockedTravel,
            ActorType.NPC => tileInfo.Flags.HasFlag(TileFlags.IgnoreNPCBlocking) ? allowAllTravel : tileInfo.BlockedTravel,
            ActorType.Object => tileInfo.Flags.HasFlag(TileFlags.IgnoreObjectBlocking) ? allowAllTravel : tileInfo.BlockedTravel,
            _ => blockAllTravel
        };

        return (collisionMask & (1 << (byte)travelType)) != 0;
    }
}
