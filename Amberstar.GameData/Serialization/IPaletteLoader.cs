using Amber.Assets.Common;

namespace Amberstar.GameData.Serialization;

public interface IPaletteLoader
{
	IGraphic LoadPalette(int index);
	IGraphic LoadUIPalette();
}
