using Amberstar.GameData.Serialization;

namespace Amberstar.Game;

public interface IPaletteIndexProvider
{
	byte UIPaletteIndex { get; }

	byte GetTilesetPaletteIndex(int tileset);
	byte GetLabyrinthPaletteIndex(int paletteIndex); // not labyrinth index!
	byte Get80x80ImagePaletteIndex(Image80x80 image80X80);
	byte GetTextPaletteIndex();
}
