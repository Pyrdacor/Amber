using Amber.Common;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy;

internal class SongLoader(Amber.Assets.Common.IAssetProvider assetProvider, LegacyPlatform platform, List<int> songMusicIndices) : ISongLoader
{
    public int SongCount => songMusicIndices.Count;

    public int GetMusicIndex(int song) => song >= 1 && song <= songMusicIndices.Count ? songMusicIndices[song - 1] : song;

    readonly Dictionary<int, List<HippelCosoSong>> music = [];

    public ISong LoadSong(int musicIndex, int songIndex)
    {
        if (!music.TryGetValue(musicIndex, out var songs))
        {
            var asset = assetProvider.GetAsset(new(AssetType.Music, musicIndex));

            if (asset == null)
                throw new AmberException(ExceptionScope.Data, $"Music {musicIndex} not found.");

            songs = HippelCosoLoader.Load(asset.GetReader(), platform);
            music.Add(musicIndex, songs);
        }
            
        if (songIndex < 0 || songIndex >= songs.Count)                
            throw new AmberException(ExceptionScope.Data, $"Song {songIndex} is not available in music {musicIndex}.");

        return songs[songIndex];
    }
}
