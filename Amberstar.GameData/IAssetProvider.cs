using Amberstar.GameData.Serialization;

namespace Amberstar.GameData;

public interface IAssetProvider : Amber.Assets.Common.IAssetProvider
{
	ITextLoader TextLoader { get; }
	IPlaceLoader PlaceLoader { get; }
	ILayoutLoader LayoutLoader { get; }
	IUIGraphicLoader UIGraphicLoader { get; }
	IMapLoader MapLoader { get; }
	ILabDataLoader LabDataLoader { get; }
	IPaletteLoader PaletteLoader { get; }
	IGraphicLoader GraphicLoader { get; }
	ICursorLoader CursorLoader { get; }
	ITilesetLoader TilesetLoader { get; }
	IFontLoader FontLoader { get; }
	ISavegameLoader SavegameLoader { get; }
}
