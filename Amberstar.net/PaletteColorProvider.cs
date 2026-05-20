using Amber.Assets.Common;
using Amber.Common;
using Amberstar.Game;

namespace Amberstar
{
	internal class PaletteColorProvider(Dictionary<int, IGraphic> palettes) : IPaletteColorProvider
	{
		public Color GetPaletteColor(int paletteIndex, int colorIndex)
		{
			return palettes[paletteIndex].GetColorAt(colorIndex, 0);
		}
	}
}
