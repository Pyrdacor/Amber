namespace Amberstar.GameData.Serialization;

public interface IMonsterLoader
{
	IMonster LoadMonster(int index);

    IReadOnlyDictionary<int, IMonster> LoadAllMonsters();
}
