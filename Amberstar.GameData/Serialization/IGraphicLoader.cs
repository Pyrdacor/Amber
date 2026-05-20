using Amber.Assets.Common;
using Amber.Common;

namespace Amberstar.GameData.Serialization;

public interface IGraphicLoader
{
	IGraphic[] LoadWindowGraphics(bool dark);
	IPaletteGraphic Load80x80Graphic(Image80x80 index);
	IGraphic LoadItemGraphic(ItemGraphic index);
	IReadOnlyDictionary<int, IGraphic> LoadAllBackgroundGraphics();
    IReadOnlyDictionary<int, IGraphic> LoadAllCloudGraphics();
    IReadOnlyDictionary<DayTime, Color[]> LoadSkyGradients();
    IReadOnlyDictionary<int, IGraphic> LoadPersonPortraits();
}
