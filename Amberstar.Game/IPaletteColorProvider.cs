using Amber.Common;

namespace Amberstar.Game;

public interface IPaletteColorProvider
{
	Color GetPaletteColor(int paletteIndex, int colorIndex);
}
