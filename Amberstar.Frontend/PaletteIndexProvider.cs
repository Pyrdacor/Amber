using Amberstar.Game;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;
using System.Collections.ObjectModel;

namespace Amberstar.Frontend
{
	internal class PaletteIndexProvider
	(
        Dictionary<BuiltinPalette, byte> builtinPaletteIndices,
        Dictionary<Image80x80, byte> palettes80x80,
		Dictionary<int, byte> palettesTileset,
		Dictionary<int, byte> generalPalettes,
		Dictionary<int, byte> combatPalettes,
		Dictionary<(OutroPalette, int), byte> outroPalettes
	) : IPaletteIndexProvider
	{
        public ReadOnlyDictionary<BuiltinPalette, byte> BuiltinPaletteIndices => builtinPaletteIndices.AsReadOnly();

        public byte Get80x80ImagePaletteIndex(Image80x80 image80X80) => palettes80x80[image80X80];

		public byte GetLabyrinthPaletteIndex(int paletteIndex) => generalPalettes[paletteIndex];

		public byte GetTextPaletteIndex() => BuiltinPaletteIndices[BuiltinPalette.UI]; // TODO?

		public byte GetTilesetPaletteIndex(int tileset) => palettesTileset[tileset];

		public byte GetCombatPaletteIndex(int background) => combatPalettes[background];

		public byte GetOutroPaletteIndex(OutroPalette palette, int fadeStep = 0) =>
			outroPalettes[(palette, fadeStep >= IOutroData.FadeSteps ? 0 : fadeStep)];


	}
}
