using Amber.Assets.Common;
using Amber.Common;
namespace Amberstar.GameData.Legacy;

internal class Cursor(Position hotspot, IGraphic graphic) : ICursor
{
	public Position Hotspot => hotspot;

	public IGraphic Graphic => graphic;
}
