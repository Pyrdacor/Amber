using Amberstar.GameData;

namespace Amberstar.Tests;

/// <summary>
/// Plays the songs of the original game data without an audio device.
/// </summary>
public sealed class MusicTests
{
	const int SongCount = 16;

	sealed class SampleCounter : IMusicPlayer
	{
		public int SampleCount { get; private set; }

		public int MaxAmplitude { get; private set; }

		public void SampleData(short[] pcmData, bool endOfStream)
		{
			SampleCount += pcmData.Length;

			foreach (var sample in pcmData)
				MaxAmplitude = Math.Max(MaxAmplitude, Math.Abs((int)sample));
		}
	}

	[Theory]
	[MemberData(nameof(TestData.DataFolders), MemberType = typeof(TestData))]
	public void AllSongsPlay(string folder)
	{
		var assetProvider = TestData.CreateAssetProvider(folder);

		for (int songIndex = 1; songIndex <= SongCount; songIndex++)
		{
			var song = assetProvider.SongLoader.LoadSong(songIndex, 0);
			var counter = new SampleCounter();

			song.Play();

			// 30 seconds in steps of 1/60 seconds (milliseconds like the game)
			for (int i = 0; i < 30 * 60; i++)
				song.Update(1000.0 / 60.0, counter);

			Assert.True(counter.SampleCount > 0, $"Song {songIndex} produced no samples.");
			Assert.True(counter.MaxAmplitude > 1000, $"Song {songIndex} is silent.");
		}
	}
}
