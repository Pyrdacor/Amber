using Amberstar.Game;
using Amberstar.GameData.Serialization;

namespace Amberstar.net
{
	internal class PaletteIndexProvider
	(
		byte uiPaletteIndex,
		Dictionary<Image80x80, byte> palettes80x80,
		Dictionary<int, byte> palettesTileset,
		Dictionary<int, byte> generalPalettes
	) : IPaletteIndexProvider
	{
		public byte UIPaletteIndex => uiPaletteIndex;

		public byte Get80x80ImagePaletteIndex(Image80x80 image80X80) => palettes80x80[image80X80];

		public byte GetLabyrinthPaletteIndex(int paletteIndex) => generalPalettes[paletteIndex];

		public byte GetTextPaletteIndex() => uiPaletteIndex; // TODO?

		public byte GetTilesetPaletteIndex(int tileset) => palettesTileset[tileset];


	}
}
