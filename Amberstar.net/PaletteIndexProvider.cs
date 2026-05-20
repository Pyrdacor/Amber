using Amberstar.Game;
using Amberstar.GameData.Serialization;
using System.Collections.ObjectModel;

namespace Amberstar
{
	internal class PaletteIndexProvider
	(
        Dictionary<BuiltinPalette, byte> builtinPaletteIndices,
        Dictionary<Image80x80, byte> palettes80x80,
		Dictionary<int, byte> palettesTileset,
		Dictionary<int, byte> generalPalettes
	) : IPaletteIndexProvider
	{
        public ReadOnlyDictionary<BuiltinPalette, byte> BuiltinPaletteIndices => builtinPaletteIndices.AsReadOnly();

        public byte Get80x80ImagePaletteIndex(Image80x80 image80X80) => palettes80x80[image80X80];

		public byte GetLabyrinthPaletteIndex(int paletteIndex) => generalPalettes[paletteIndex];

		public byte GetTextPaletteIndex() => BuiltinPaletteIndices[BuiltinPalette.UI]; // TODO?

		public byte GetTilesetPaletteIndex(int tileset) => palettesTileset[tileset];


	}
}
