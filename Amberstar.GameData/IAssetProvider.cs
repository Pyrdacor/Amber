using Amberstar.GameData.Serialization;

namespace Amberstar.GameData;

public interface IAssetProvider : Amber.Assets.Common.IAssetProvider
{
	ITextLoader TextLoader { get; }
	IPlaceLoader PlaceLoader { get; }
	ILayoutLoader LayoutLoader { get; }
	IUIGraphicLoader UIGraphicLoader { get; }
	IMapLoader MapLoader { get; }
	IPaletteLoader PaletteLoader { get; }
	IGraphicLoader GraphicLoader { get; }
	ITilesetLoader TilesetLoader { get; }
}
