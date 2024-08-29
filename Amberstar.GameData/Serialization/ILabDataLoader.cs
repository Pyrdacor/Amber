namespace Amberstar.GameData.Serialization;

public interface ILabDataLoader
{
	ILabData LoadLabData(int index);

	Dictionary<int, ILabBlock> LoadAllLabBlocks();
}
