using Amberstar.GameData.Serialization;

namespace Amberstar.Game
{
	public interface IUIGraphicIndexProvider
	{
		int GetUIGraphicIndex(UIGraphic graphic);
		int GetButtonIndex(Button button);
		int GetStatusIconIndex(StatusIcon statusIcon);
		int Get80x80ImageIndex(Image80x80 image);
		int GetItemGraphicIndex(ItemGraphic graphic);
	}
}
