using Amber.Audio;
using Amberstar.GameData;

namespace Amberstar.Game;

class AudioStream(IAssetProvider assetProvider, int musicIndex, int songIndex) : IAudioStream, IMusicPlayer
{
    readonly ISong song = assetProvider.SongLoader.LoadSong(musicIndex, songIndex);

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
    int currentSongIndex = -1; // TODO: Use enum later?
    readonly Dictionary<int, AudioStream> music = [];

    private void UpdateMusic(double elapsed)
    {
        if (currentSongIndex != -1 && audioOutput.Available && audioOutput.Enabled)
        {
            var song = music[currentSongIndex];

            song.Update(elapsed * 1000.0);
        }
    }

    internal void PlaySong(int songIndex)
    {
        if (musicPlaying && songIndex == currentSongIndex)
            return;

        if (audioOutput == null || !audioOutput.Available)
            return;

        if (songIndex != currentSongIndex)
        {
            if (!music.TryGetValue(songIndex, out var audioStream))
            {
                audioStream = new AudioStream(AssetProvider, songIndex, 0);
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
    }
}
