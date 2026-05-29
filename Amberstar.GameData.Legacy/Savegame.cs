using Amber.IO.FileFormats.Compression;
using Amber.Serialization;

namespace Amberstar.GameData.Legacy;

internal class Savegame : ISavegame
{
    public Savegame()
    {

    }

	public Savegame(IDataReader dataReader)
	{
        Read(dataReader);
	}

	public void Read(IDataReader dataReader)
	{
        if (dataReader.PeekDword() == 0x59415921) // "YAY!"?
        {
            dataReader.Position += 4;
            dataReader = new DataReader(YAY.Decrypt(dataReader));
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
                Duration = activeSpellDurations[i],
                Value = dataReader.ReadByte(),
            };
        }

        Year = dataReader.ReadWord();
        MapIndex = dataReader.ReadWord();
        PartyCharacterIndices = dataReader.ReadWords(ISavegame.MaxPartyMembers);
        TravelledDays = dataReader.ReadWord();
        RelativeYear = dataReader.ReadWord();

        var transportTypes = dataReader.ReadBytes(ISavegame.MaxTransportCount);
        var transportXPositions = dataReader.ReadBytes(ISavegame.MaxTransportCount);
        var transportYPositions = dataReader.ReadBytes(ISavegame.MaxTransportCount);
        var transportMapIndices = dataReader.ReadWords(ISavegame.MaxTransportCount);

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
        ChestGold = dataReader.ReadWords(ISavegame.MaxChests);
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

    public void Write(IDataWriter dataWriter, bool encrypt)
    {
        if (encrypt)
        {
            var writer = new DataWriter();
            Write(writer, false);
            dataWriter.Write(YAY.Encrypt(writer.ToArray()));
            return;
        }

        dataWriter.Write((byte)Month);
        dataWriter.Write((byte)Day);
        dataWriter.Write((byte)Hour);
        dataWriter.Write((byte)Minute);
        dataWriter.Write((byte)PartyX);
        dataWriter.Write((byte)PartyY);
        dataWriter.WriteEnumAsByte(PartyDirection);

        dataWriter.WriteBytes([.. ActiveSpells.Select(s => s.Duration)]);

        dataWriter.Write((byte)PartySize);
        dataWriter.Write((byte)ActivePartyMember);
        dataWriter.WriteEnumAsByte(TravelType);
        dataWriter.WriteEnumAsByte(SpecialItems);
        dataWriter.Write((byte)(MusicBlock ? 1 : 0)); // TODO: 1 or 0xff expected on Amiga/Atari?

        dataWriter.WriteBytes([.. ActiveSpells.Select(s => s.Value)]);

        dataWriter.Write((word)Year);
        dataWriter.Write((word)MapIndex);
        dataWriter.WriteWords(PartyCharacterIndices);
        dataWriter.Write((word)TravelledDays);
        dataWriter.Write((word)RelativeYear);

        var transportTypes = new byte[ISavegame.MaxTransportCount];
        var transportXPositions = new byte[ISavegame.MaxTransportCount];
        var transportYPositions = new byte[ISavegame.MaxTransportCount];
        var transportMapIndices = new word[ISavegame.MaxTransportCount];

        for (int i = 0; i < ISavegame.MaxTransportCount; i++)
        {
            var transport = Transports[i];

            transportTypes[i] = (byte)transport.Type;
            transportXPositions[i] = (byte)transport.X;
            transportYPositions[i] = (byte)transport.Y;
            transportMapIndices[i] = (word)transport.MapIndex;
        }

        dataWriter.Write(transportTypes);
        dataWriter.Write(transportXPositions);
        dataWriter.Write(transportYPositions);
        dataWriter.WriteWords(transportMapIndices);

        dataWriter.Write(QuestBits);
        dataWriter.Write(EventBits);
        dataWriter.Write(CharacterBits);
        dataWriter.Write(KnownWordsBits);
        dataWriter.Write(ChestSlotBits);
        dataWriter.WriteWords(ChestGold);
        dataWriter.WriteBytes(WareCounts);

        dataWriter.WriteBytes(CombatPositions);

        dataWriter.Write((word)TileChanges.Length); // TODO: Check here if Length exceeds word.MaxValue or when adding them?

        foreach (var tileChange in TileChanges)
        {
            dataWriter.Write((word)tileChange.MapIndex);
            dataWriter.Write((byte)tileChange.X);
            dataWriter.Write((byte)tileChange.Y);
            dataWriter.Write((word)tileChange.TileIndex);
        }
    }

    public int Year { get; set; }

	public int Month { get; set; }

	public int Day { get; set; }

	public int Hour { get; set; }

	public int Minute { get; set; }

	public int TravelledDays { get; set; }

	public int RelativeYear { get; set; }

	public int MapIndex { get; set; }

	public int PartyX { get; set; }

	public int PartyY { get; set; }

	public Direction PartyDirection { get; set; }

	public ActiveSpell[] ActiveSpells { get; private set; } = new ActiveSpell[ISavegame.ActiveSpellCount];

	public Transport[] Transports { get; private set; } = new Transport[ISavegame.MaxTransportCount];

	public int PartySize { get; set; }

	public int ActivePartyMember { get; set; }

    public int[] PartyCharacterIndices { get; private set; } = new int[ISavegame.MaxPartyMembers];

	public int[] CombatPositions { get; private set; } = new int[ISavegame.MaxPartyMembers];

	public TravelType TravelType { get; set; }

	public SpecialItems SpecialItems { get; set; }

	public bool MusicBlock { get; set; }

    public byte[] QuestBits { get; private set; } = new byte[32];

	public byte[] EventBits { get; private set; } = new byte[4064];

    public byte[] CharacterBits { get; private set; } = new byte[1502];

    public byte[] KnownWordsBits { get; private set; } = new byte[626];

    public byte[] ChestSlotBits { get; private set; } = new byte[1500];

    public int[] ChestGold { get; private set; } = new int[ISavegame.MaxChests];

    public int[] WareCounts { get; private set; } = new int[1200];

    public TileChange[] TileChanges { get; set; } = [];
}