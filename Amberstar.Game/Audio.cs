using Amber.Audio;
using Amberstar.GameData;

namespace Amberstar.Game;

class AudioStream(ISong song) : IAudioStream, IMusicPlayer
{
    public AudioStream(IAssetProvider assetProvider, int musicIndex, int songIndex)
        : this(assetProvider.SongLoader.LoadSong(musicIndex, songIndex))
    {
    }

    public event DataStreamEventHandler? DataStreamed;

    public void SampleData(short[] pcmData, bool endOfStream)
    {
        DataStreamed?.Invoke(pcmData, endOfStream);
    }

    public void Update(double elapsed)
    {
        song.Update(elapsed, this);
    }

    public void Play()
    {
        song.Play();
    }

    public void Stop()
    {
        song.Stop();
    }

    public void Reset()
    {
        song.Reset();
    }
}

partial class Game
{
    bool musicPlaying = false;
    int currentSongIndex = -1;
    // the song of the shown map which is played when the music is switched on
    int requestedSongIndex = -1;
    readonly Dictionary<int, AudioStream> music = [];

    /// <summary>
    /// The music of the outro is not one of the 19 songs of the game (see <see cref="IOutroData.Music"/>).
    /// </summary>
    internal const int OutroSong = 100;

    /// <summary>
    /// The music of the intro (see <see cref="IIntroData.Music"/>).
    /// </summary>
    internal const int IntroSong = 101;

    /// <summary>
    /// The songs of the logo of Thalion follow this one (see <see cref="IThalionLogo.GetSong"/>).
    /// </summary>
    internal const int ThalionLogoSong = 200;

    /// <summary>
    /// The song which is played (or would be played if the music is switched on), 0 if none.
    /// </summary>
    internal int CurrentSong => Math.Max(0, requestedSongIndex);

    private void UpdateMusic(double elapsed)
    {
        if (currentSongIndex != -1 && audioOutput.Available && audioOutput.Enabled)
        {
            var song = music[currentSongIndex];

            song.Update(elapsed * 1000.0);
        }
    }

    /// <summary>
    /// Switches the music on or off (stored in the savegame like in the original).
    /// </summary>
    internal void SetMusic(bool on)
    {
        State.MusicBlock = !on;
        StopSong();
        audioOutput.Enabled = on;

        if (on && requestedSongIndex != -1)
            PlaySong(requestedSongIndex);
    }

    internal void PlaySong(int songIndex)
    {
        requestedSongIndex = songIndex;

        // song 0 stops the music (Play_song in the original)
        if (songIndex == 0)
        {
            StopSong();
            return;
        }

        if (musicPlaying && songIndex == currentSongIndex)
            return;

        if (audioOutput == null || !audioOutput.Available || !audioOutput.Enabled)
            return;

        if (songIndex != currentSongIndex)
        {
            if (!music.TryGetValue(songIndex, out var audioStream))
            {
                audioStream = songIndex switch
                {
                    OutroSong => new AudioStream(AssetProvider.OutroData!.Music!),
                    IntroSong => new AudioStream(AssetProvider.IntroData!.Music!),
                    > ThalionLogoSong => new AudioStream(AssetProvider.ThalionLogo!.GetSong(songIndex - ThalionLogoSong)),
                    _ => new AudioStream(AssetProvider, AssetProvider.SongLoader.GetMusicIndex(songIndex), 0)
                };
                music.Add(songIndex, audioStream);
            }

            if (currentSongIndex != -1)
            {
                music[currentSongIndex].Reset();
                music[currentSongIndex].Stop();
            }

            currentSongIndex = songIndex;
            audioStream.Play();

            audioOutput.Stop();
            audioOutput.StreamData(audioStream, 1, 44100, ChannelDataFormat.Signed16Bit);
        }
        else
        {
            music[currentSongIndex].Play();
        }

        audioOutput.Start();

        musicPlaying = true;
    }

    internal void StopSong()
    {
        if (audioOutput == null || !audioOutput.Available)
            return;

        if (currentSongIndex != -1)
        {
            music[currentSongIndex].Reset();
            music[currentSongIndex].Stop();
        }

        audioOutput.Stop();
        musicPlaying = false;
        // The next song must be streamed again.
        currentSongIndex = -1;
    }
}
