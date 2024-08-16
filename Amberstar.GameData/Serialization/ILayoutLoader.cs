using Amber.Assets.Common;

namespace Amberstar.GameData.Serialization
{
	public interface ILayoutLoader
	{
		IGraphic LoadLayout(int index);
		IGraphic LoadPortraitArea();
	}
}
