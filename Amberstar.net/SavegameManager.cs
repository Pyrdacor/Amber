using Amber.Serialization;
using Amberstar.Game;
using Amberstar.GameData;
using Amberstar.GameData.Legacy;

namespace Amberstar;

internal class SavegameManager(AssetProvider assetProvider) : ISavegameManager
{
    public ISavegame LoadFromFile(string filename)
    {
        var externalSavegame = new ExtendedSavegame(assetProvider);
        var dataReader = new DataReader(File.ReadAllBytes(filename));

        try
        {
            externalSavegame.Read(dataReader);
            return externalSavegame;
        }
        catch (FormatException)
        {
            return assetProvider.SavegameLoader.LoadSavegame();
        }
    }

    public void SaveToFile(string filename, ISavegame savegame)
    {
        var dataWriter = new DataWriter();
        ExtendedSavegame.CreateFrom(assetProvider, savegame, false).Write(dataWriter, false);

        File.WriteAllBytes(filename, dataWriter.ToArray());
    }
}
