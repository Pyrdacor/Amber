using Amber.Assets.Common;

namespace Amberstar.GameData.Serialization;

public interface ISongLoader
{
	ISong LoadSong(int musicIndex, int songIndex);

	/// <summary>
	/// Number of songs which the game can play (19 in the original).
	/// </summary>
	int SongCount { get; }

	/// <summary>
	/// Gets the music (module) of a song number of the game (1-based). Some songs share a module.
	/// </summary>
	int GetMusicIndex(int song);
}
