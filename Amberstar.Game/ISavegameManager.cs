using Amberstar.GameData;

namespace Amberstar.Game;

public interface ISavegameManager
{
    ISavegame LoadFromFile(string filename);
    void SaveToFile(string filename, ISavegame savegame);
}
