using Amber.Assets.Common;

namespace Amberstar.GameData.Serialization
{
	public interface IMapLoader
	{
		IMap LoadMap(IAsset asset);
		bool TryLoadMap2D(IAsset asset, out IMap2D? map2D);
		bool TryLoadMap3D(IAsset asset, out IMap3D? map3D);
	}
}
