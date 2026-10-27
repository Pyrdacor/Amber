using Amber.Common;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy;

/// <summary>
/// The music modules (COSO) are played like the original players: the Atari ST with its sound chip
/// (<see cref="HippelCosoYmSong"/>), the Amiga with its sample voices (<see cref="HippelCosoPaulaSong"/>).
/// </summary>
internal class SongLoader(Amber.Assets.Common.IAssetProvider assetProvider, LegacyPlatform platform, List<int> songMusicIndices,
    List<string> songNames) : ISongLoader
{
    public int SongCount => songMusicIndices.Count;

    public IReadOnlyList<string> SongNames => songNames;

    public int GetMusicIndex(int song) => song >= 1 && song <= songMusicIndices.Count ? songMusicIndices[song - 1] : song;

    public ISong LoadSong(int musicIndex, int songIndex)
    {
        var asset = assetProvider.GetAsset(new(AssetType.Music, musicIndex))
            ?? throw new AmberException(ExceptionScope.Data, $"Music {musicIndex} not found.");
        var reader = asset.GetReader();
        reader.Position = 0;
        var module = reader.ReadBytes(reader.Size);

        return platform == LegacyPlatform.Amiga
            ? new HippelCosoPaulaSong(module, songIndex + 1)
            : new HippelCosoYmSong(module, songIndex + 1);
    }
}
