using Amberstar.GameData;
using Amberstar.GameData.Serialization;
using System.Collections.ObjectModel;

namespace Amberstar.Game;

public interface IPaletteIndexProvider
{
	ReadOnlyDictionary<BuiltinPalette, byte> BuiltinPaletteIndices { get; }

    byte GetTilesetPaletteIndex(int tileset);
	byte GetLabyrinthPaletteIndex(int paletteIndex); // not labyrinth index!
	byte Get80x80ImagePaletteIndex(Image80x80 image80X80);
	byte GetTextPaletteIndex();
	/// <summary>
	/// The palette of a combat background (1-based) which is used by the whole combat screen.
	/// </summary>
	byte GetCombatPaletteIndex(int background);
	/// <summary>
	/// Gets a palette of the outro while it fades (see <see cref="IOutroData.GetPalette"/>).
	/// </summary>
	byte GetOutroPaletteIndex(OutroPalette palette, int fadeStep = 0);
}
