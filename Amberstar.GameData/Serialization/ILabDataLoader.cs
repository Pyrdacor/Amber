namespace Amberstar.GameData.Serialization;

public interface ILabDataLoader
{
	ILabData LoadLabData(int index);

    IReadOnlyDictionary<int, ILabBlock> LoadAllLabBlocks();
}
