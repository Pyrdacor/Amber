using Amber.Serialization;

namespace Amberstar.GameData.Legacy;

internal class Savegame : ISavegame
{
	public Savegame(IDataReader dataReader)
	{
		int[] ReadWords(int count)
		{
			int[] words = new int[count];

			for (int i = 0; i < count; i++)
				words[i] = dataReader.ReadWord();

			return words;
		}

		Month = dataReader.ReadByte();
		Day = dataReader.ReadByte();
		Hour = dataReader.ReadByte();
		Minute = dataReader.ReadByte();
		PartyX = dataReader.ReadByte();
		PartyY = dataReader.ReadByte();
		PartyDirection = (Direction)dataReader.ReadByte();

		int[] activeSpellDurations = dataReader.ReadBytes(ISavegame.ActiveSpellCount).Select(b => (int)b).ToArray();

		PartySize = dataReader.ReadByte();
		ActivePartyMember = dataReader.ReadByte();
		TravelType = (TravelType)dataReader.ReadByte();
		SpecialItems = (SpecialItems)dataReader.ReadByte();
		MusicBlock = dataReader.ReadByte() != 0;

		for (int i = 0; i < ISavegame.ActiveSpellCount; i++)
		{
			ActiveSpells[i] = new()
			{
				Duration = activeSpellDurations[i] & 0xff,
				Value = dataReader.ReadByte(),
			};
		}

		Year = dataReader.ReadWord();
		MapIndex = dataReader.ReadWord();
		PartyCharacterIndices = ReadWords(ISavegame.MaxPartyMembers);
		TravelledDays = dataReader.ReadWord();
		RelativeYear = dataReader.ReadWord();

		var transportTypes = dataReader.ReadBytes(ISavegame.MaxTransportCount);
		var transportXPositions = dataReader.ReadBytes(ISavegame.MaxTransportCount);
		var transportYPositions = dataReader.ReadBytes(ISavegame.MaxTransportCount);
		var transportMapIndices = ReadWords(ISavegame.MaxTransportCount);

		for (int i = 0; i < ISavegame.MaxTransportCount; i++)
		{
			Transports[i] = new()
			{
				MapIndex = transportMapIndices[i],
				X = transportXPositions[i],
				Y = transportYPositions[i],
				Type = (TransportType)transportTypes[i]
			};
		}

		QuestBits = dataReader.ReadBytes(32);
		EventBits = dataReader.ReadBytes(4064);
		CharacterBits = dataReader.ReadBytes(1502);
		KnownWordsBits = dataReader.ReadBytes(626);
		ChestSlotBits = dataReader.ReadBytes(1500);
		ChestGold = ReadWords(ISavegame.MaxChests);
		WareCounts = dataReader.ReadBytes(1200).Select(b => (int)b).ToArray();

		CombatPositions = dataReader.ReadBytes(ISavegame.MaxPartyMembers).Select(b => (int)b).ToArray();

		int numTileChanges = dataReader.ReadWord();

		TileChanges = new TileChange[numTileChanges];

		for (int i = 0; i < numTileChanges; i++)
		{
			var mapIndex = dataReader.ReadWord();
			var x = dataReader.ReadByte();
			var y = dataReader.ReadByte();
			var tileIndex = dataReader.ReadWord();

			TileChanges[i] = new()
			{
				MapIndex = mapIndex,
				X = x,
				Y = y,
				TileIndex = tileIndex
			};
		}
	}

	public int Year { get; }

	public int Month { get; }

	public int Day { get; }

	public int Hour { get; }

	public int Minute { get; }

	public int TravelledDays { get; }

	public int RelativeYear { get; }

	public int MapIndex { get; }

	public int PartyX { get; }

	public int PartyY { get; }

	public Direction PartyDirection { get; }

	public ActiveSpell[] ActiveSpells { get; } = new ActiveSpell[ISavegame.ActiveSpellCount];

	public Transport[] Transports { get; } = new Transport[ISavegame.MaxTransportCount];

	public int PartySize { get; }

	public int ActivePartyMember { get; }

	public int[] PartyCharacterIndices { get; }

	public int[] CombatPositions { get; } = new int[ISavegame.MaxPartyMembers];

	public TravelType TravelType { get; }

	public SpecialItems SpecialItems { get; }

	public bool MusicBlock { get; }

	public byte[] QuestBits { get; }

	public byte[] EventBits { get; }

	public byte[] CharacterBits { get; }

	public byte[] KnownWordsBits { get; }

	public byte[] ChestSlotBits { get; }

	public int[] ChestGold { get; }

	public int[] WareCounts { get; }

	public TileChange[] TileChanges { get; }
}