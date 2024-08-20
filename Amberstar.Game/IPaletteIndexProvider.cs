using Amberstar.GameData.Serialization;

namespace Amberstar.Game
{
	public interface IPaletteIndexProvider
	{
		int UIPaletteIndex { get; }

		int GetTilesetPaletteIndex(int tileset);
		int GetLabyrinthPaletteIndex(int paletteIndex); // not labyrinth index!
		int Get80x80ImagePaletteIndex(Image80x80 image80X80);
	}
}
