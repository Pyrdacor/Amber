using Amber.Assets.Common;

namespace Amberstar.GameData.Serialization
{
	public interface IMapLoader
	{
		IMap LoadMap(IAsset asset);
	}
}
