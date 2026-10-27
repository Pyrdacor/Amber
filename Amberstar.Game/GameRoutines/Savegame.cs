using Amber.Serialization;

namespace Amberstar.Game;

/// <summary>
/// Like in the original there is one savegame. It contains the party data
/// (PARTYDAT.SAV), the character records of the party members and dismissed
/// characters (CHARDATA.AMB)
/// and the automaps (AUTOMAP.AMB) which the original stores on the disk.
/// </summary>
partial class Game
{
	const dword SavegameMagic = 0x414d4253; // "AMBS"
	const byte SavegameVersion = 1;

	/// <summary>
	/// Path of the savegame file. It is set by the frontend.
	/// </summary>
	public string? SavegamePath { get; set; }

	public bool HasSavegame => SavegamePath != null && File.Exists(SavegamePath);

	public void SaveGame()
	{
		if (SavegamePath == null)
			throw new InvalidOperationException("No savegame path is set.");

		var writer = new DataWriter();
		writer.Write(SavegameMagic);
		writer.Write(SavegameVersion);

		var savegameWriter = new DataWriter();
		State.ToSavegame(AssetProvider.SavegameLoader).Write(savegameWriter, false);
		var savegameData = savegameWriter.ToArray();
		writer.Write((dword)savegameData.Length);
		writer.Write(savegameData);

		var partyMembers = State.SavedCharactersWithCharacterIndex.ToList();
		writer.Write((word)partyMembers.Count);

		foreach (var (characterIndex, partyMember) in partyMembers)
		{
			var record = AssetProvider.PersonLoader.WritePartyMember(partyMember);
			writer.Write((word)characterIndex);
			writer.Write((word)record.Length);
			writer.Write(record);
		}

		var automaps = State.Automaps;
		writer.Write((word)automaps.Count);

		foreach (var (mapIndex, automap) in automaps)
		{
			writer.Write((word)mapIndex);
			writer.Write((word)automap.Length);
			writer.Write(automap);
		}

		var directory = Path.GetDirectoryName(SavegamePath);

		if (!string.IsNullOrEmpty(directory))
			Directory.CreateDirectory(directory);

		File.WriteAllBytes(SavegamePath, writer.ToArray());
	}

	public void LoadGame()
	{
		if (!HasSavegame)
			throw new InvalidOperationException("There is no savegame.");

		var reader = new DataReader(File.ReadAllBytes(SavegamePath!));

		if (reader.ReadDword() != SavegameMagic)
			throw new InvalidDataException("The file is no savegame.");

		if (reader.ReadByte() > SavegameVersion)
			throw new InvalidDataException("The savegame was written by a newer version.");

		var savegame = AssetProvider.SavegameLoader.Create();
		savegame.Read(new DataReader(reader.ReadBytes((int)reader.ReadDword())));

		State.LoadFrom(savegame);
		State.ResetPartyMembers();

		int partyMemberCount = reader.ReadWord();

		for (int i = 0; i < partyMemberCount; i++)
		{
			int characterIndex = reader.ReadWord();
			var record = reader.ReadBytes(reader.ReadWord());
			State.ReplacePartyMember(characterIndex, AssetProvider.PersonLoader.ReadPartyMember(characterIndex, record));
		}

		var automaps = new Dictionary<int, byte[]>();
		int automapCount = reader.ReadWord();

		for (int i = 0; i < automapCount; i++)
		{
			int mapIndex = reader.ReadWord();
			automaps[mapIndex] = reader.ReadBytes(reader.ReadWord());
		}

		State.SetAutomaps(automaps);

		ResetStatusIcons();
		UpdatePartyMembers();
		SetMusic(!State.MusicBlock);
	}

	/// <summary>
	/// Loads the savegame and shows the saved location.
	/// </summary>
	internal void LoadGameAndContinue()
	{
		LoadGame();
		ScreenHandler.ClearAllScreens();
		Teleport(1 + State.PartyPosition.X, 1 + State.PartyPosition.Y, State.PartyDirection, State.MapIndex, true);
	}
}
