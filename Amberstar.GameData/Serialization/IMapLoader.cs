namespace Amberstar.GameData.Serialization;

public interface IMapLoader
{
	IMap LoadMap(int index);
	bool TryLoadMap2D(int index, out IMap2D? map2D);
	bool TryLoadMap3D(int index, out IMap3D? map3D);
}
