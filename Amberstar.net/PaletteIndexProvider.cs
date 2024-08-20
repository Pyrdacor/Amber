using Amberstar.Game;
using Amberstar.GameData.Serialization;

namespace Amberstar.net
{
	internal class PaletteIndexProvider
	(
		int uiPaletteIndex,
		Dictionary<Image80x80, int> palettes80x80,
		Dictionary<int, int> palettesTileset,
		Dictionary<int, int> generalPalettes
	) : IPaletteIndexProvider
	{
		public int UIPaletteIndex => uiPaletteIndex;

		public int Get80x80ImagePaletteIndex(Image80x80 image80X80) => palettes80x80[image80X80];

		public int GetLabyrinthPaletteIndex(int paletteIndex) => generalPalettes[paletteIndex];

		public int GetTilesetPaletteIndex(int tileset) => palettesTileset[tileset];
	}
}
