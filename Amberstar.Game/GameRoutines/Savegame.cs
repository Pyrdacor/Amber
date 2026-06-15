using Amber.Serialization;

namespace Amberstar.Game;

partial class Game
{
    const string TempSaveFile = @"D:\Projects\Amber\Amberstar.net\temp.save";

    public void SaveGame()
    {
        // TODO: For now we just store it in some local file.
        var writer = new DataWriter();
        State.ToSavegame(AssetProvider.SavegameLoader).Write(writer, false);

        using var fileStream = File.Create(TempSaveFile);
        writer.CopyTo(fileStream);
    }

    public void LoadGame()
    {
        using var fileStream = File.OpenRead(TempSaveFile);
        var reader = new DataReader(fileStream);
        var savegame = AssetProvider.SavegameLoader.Create();
        savegame.Read(reader);

        State.LoadFrom(savegame);

        ResetStatusIcons();
        UpdatePartyMembers();
    }
}