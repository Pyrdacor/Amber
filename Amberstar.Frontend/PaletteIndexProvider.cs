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
		Dictionary<(OutroPalette, int), byte> outroPalettes,
		List<byte> loadingScreenPalettes
	) : IPaletteIndexProvider
	{
        public ReadOnlyDictionary<BuiltinPalette, byte> BuiltinPaletteIndices => builtinPaletteIndices.AsReadOnly();

        public byte Get80x80ImagePaletteIndex(Image80x80 image80X80) => palettes80x80[image80X80];

		public byte GetLabyrinthPaletteIndex(int paletteIndex) => generalPalettes[paletteIndex];

		public byte GetTextPaletteIndex() => BuiltinPaletteIndices[BuiltinPalette.UI]; // TODO?

		public byte GetTilesetPaletteIndex(int tileset) => palettesTileset[tileset];

		public byte GetCombatPaletteIndex(int background) => combatPalettes[background];

		// the last fade in step is the normal palette
		public byte GetOutroPaletteIndex(OutroPalette palette, int fadeStep = 0) =>
			outroPalettes.TryGetValue((palette, fadeStep), out byte index) ? index : outroPalettes[(palette, 0)];

		public byte GetLoadingScreenPaletteIndex(int fadeStep) => loadingScreenPalettes[Math.Clamp(fadeStep, 0, loadingScreenPalettes.Count - 1)];


	}
}
