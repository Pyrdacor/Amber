using Amber.Assets.Common;

namespace Amberstar.GameData.Serialization
{
	public interface IGraphicLoader
	{
		IPaletteGraphic Load80x80Graphic(Image80x80 index);
		IGraphic LoadItemGraphic(ItemGraphic index);
		Dictionary<int, IGraphic> LoadAllBackgroundGraphics();
	}
}
