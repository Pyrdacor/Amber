using Amber.IO.FileFormats.Compression;
using Amber.Serialization;
using Amberstar.Game;
using Amberstar.GameData;
using Amberstar.GameData.Legacy;
using Amberstar.GameData.Serialization;
#pragma warning disable CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
using word = System.UInt16;
#pragma warning restore CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.

namespace Amberstar;

public record SavedItemSlot : ISavedItemSlot
{
    public int Amount { get; set; }

    public uint ItemIndex { get; set; }

    public byte SpellCharges { get; set; }

    public ItemSlotFlags SlotFlags { get; set; }

    public void Read(IDataReader dataReader)
    {
        Amount = dataReader.ReadWord();
        ItemIndex = dataReader.ReadWord();
        SpellCharges = dataReader.ReadByte();
        SlotFlags = (ItemSlotFlags)dataReader.ReadByte();
    }

    public void Write(IDataWriter dataWriter)
    {
        dataWriter.Write((word)Amount);
        dataWriter.Write((word)ItemIndex);
        dataWriter.Write((byte)SpellCharges);
        dataWriter.Write((byte)SlotFlags);
    }
}

public record SavedChest : ISavedChest
{
    public int Index { get; set; }

    public ISavedItemSlot[] ItemsSlots { get; } = new SavedItemSlot[IChest.SlotCount];
    
    public SavedChest()
    {

    }

    public SavedChest(IDataReader dataReader)
    {
        Read(dataReader);
    }

    public void Read(IDataReader dataReader)
    {
        Index = dataReader.ReadWord();

        for (int i = 0; i < IChest.SlotCount; i++)
        {
            (ItemsSlots[i] as SavedItemSlot)!.Read(dataReader);
        }
    }

    public void Write(IDataWriter dataWriter)
    {
        dataWriter.Write((word)Index);

        for (int i = 0; i < IChest.SlotCount; i++)
        {
            ItemsSlots[i].Write(dataWriter);
        }
    }
}

public class StaticItemData : IStaticItemData
{
    public uint Index { get; set; }

    public ItemType Type { get; set; }

    public ItemGraphic GraphicIndex { get; set; }

    public AmmoType UsedAmmoType { get; set; }

    public GenderFlags Genders { get; set; }

    public byte Hands { get; set; }

    public byte Fingers { get; set; }

    public byte HitPoints { get; set; }

    public byte SpellPoints { get; set; }

    public GameData.Attribute Attribute { get; set; }

    public byte AttributeValue { get; set; }

    public Skill Skill { get; set; }

    public byte SkillValue { get; set; }

    public SpellSchool SpellSchool { get; set; }

    public byte SpellIndex { get; set; }

    public AmmoType AmmoType { get; set; }

    public byte Defense { get; set; }

    public byte Damage { get; set; }

    public EquipmentSlot? EquipmentSlot { get; set; }

    public byte MagicWeaponBonus { get; set; }

    public byte MagicArmorBonus { get; set; }

    public byte SpecialIndex { get; set; }

    public byte InitialCharges { get; set; }

    public byte MaxCharges { get; set; }

    public ItemFlags Flags { get; set; }

    public Skill? MalusSkill1 { get; set; }

    public Skill? MalusSkill2 { get; set; }

    public byte Malus1 { get; set; }

    public byte Malus2 { get; set; }

    public byte TextIndex { get; set; }

    public ClassFlags UsableClasses { get; set; }

    public word BuyPrice { get; set; }

    public word Weight { get; set; }

    public word NameIndex { get; set; }

    public StaticItemData()
    {

    }

    public StaticItemData(IDataReader dataReader)
    {
        Read(dataReader);
    }

    public void Read(IDataReader dataReader)
    {
        // TODO
    }

    public void Write(IDataWriter dataWriter)
    {

    }
}

internal class ExtendedSavegame(AssetProvider assetProvider) : IExtendedSavegame
{
    public const string Magic = "AXS"; // Amberstar Extended Savegame
    public const byte MaxSupportFileFormatVersion = 0;

    public byte FileFormatVersion { get; set; }

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

    public Dictionary<uint, IStaticItemData> Items { get; } = [];

    public Dictionary<int, ISavedChest> Chests { get; } = [];

    public static ExtendedSavegame CreateFrom(AssetProvider assetProvider, ISavegame savegame, bool cloneIfExtended)
    {
        if (savegame is ExtendedSavegame extendedSavegame && !cloneIfExtended)
            return extendedSavegame;

        var created = new ExtendedSavegame(assetProvider)
        {
            FileFormatVersion = MaxSupportFileFormatVersion,
            Year = savegame.Year,
            Month = savegame.Month,
            Day = savegame.Day,
            Hour = savegame.Hour,
            Minute = savegame.Minute,
            TravelledDays = savegame.TravelledDays,
            RelativeYear = savegame.RelativeYear,
            MapIndex = savegame.MapIndex,
            PartyX = savegame.PartyX,
            PartyY = savegame.PartyY,
            PartyDirection = savegame.PartyDirection,
            PartySize = savegame.PartySize,
            ActivePartyMember = savegame.ActivePartyMember,
            TravelType = savegame.TravelType,
            SpecialItems = savegame.SpecialItems,
            MusicBlock = savegame.MusicBlock,
            ActiveSpells = savegame.ActiveSpells.Select(s => new ActiveSpell { Duration = s.Duration, Value = s.Value }).ToArray(),
            Transports = savegame.Transports.Select(t => new Transport { MapIndex = t.MapIndex, X = t.X, Y = t.Y, Type = t.Type }).ToArray(),
            PartyCharacterIndices = (int[])savegame.PartyCharacterIndices.Clone(),
            CombatPositions = (int[])savegame.CombatPositions.Clone(),
            QuestBits = (byte[])savegame.QuestBits.Clone(),
            EventBits = (byte[])savegame.EventBits.Clone(),
            CharacterBits = (byte[])savegame.CharacterBits.Clone(),
            KnownWordsBits = (byte[])savegame.KnownWordsBits.Clone(),
            ChestSlotBits = (byte[])savegame.ChestSlotBits.Clone(),
            ChestGold = (int[])savegame.ChestGold.Clone(),
            WareCounts = (int[])savegame.WareCounts.Clone(),
            TileChanges = savegame.TileChanges.Select(tc => new TileChange { MapIndex = tc.MapIndex, X = tc.X, Y = tc.Y, TileIndex = tc.TileIndex }).ToArray(),
        };

        if (savegame is IExtendedSavegame extendedSave)
        {
            created.Items.Clear();
            foreach (var kv in extendedSave.Items)
                created.Items.Add(kv.Key, kv.Value);

            created.Chests.Clear();
            foreach (var kv in extendedSave.Chests)
                created.Chests.Add(kv.Key, kv.Value);
        }

        return created;
    }

    public void Read(IDataReader dataReader)
    {
        var header = dataReader.PeekBytes(4);
        
        if (header[0] != Magic[0] ||
            header[1] != Magic[1] ||
            header[2] != Magic[2])
            throw new FormatException("Not an extended savegame.");

        FileFormatVersion = header[3];

        if (FileFormatVersion > MaxSupportFileFormatVersion)
            throw new IOException("Unsupported version of extended savegame.");

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

        int numItems = dataReader.ReadWord();

        Items.Clear();

        for (int i = 0; i < numItems; i++)
        {
            var itemData = new StaticItemData(dataReader);

            Items.Add(itemData.Index, itemData);
        }

        int numChests = dataReader.ReadWord();

        Chests.Clear();

        for (int i = 0; i < numChests; i++)
        {
            var chest = new SavedChest(dataReader);

            Chests.Add(chest.Index, chest);
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

        dataWriter.WriteWithoutLength(Magic);
        dataWriter.Write(MaxSupportFileFormatVersion);

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

        var items = Chests
            .SelectMany(chest => chest.Value.ItemsSlots.Select(slot => slot.Amount == 0 ? 0u : (uint)slot.ItemIndex))
            .Distinct()
            .Select(assetProvider.ItemLoader.LoadItem)
            .ToList();

        dataWriter.Write((word)items.Count);

        foreach (var item in items)
        {
            item.Write(dataWriter);
        }

        dataWriter.Write((word)Chests.Count);

        foreach (var chest in Chests.Values.OrderBy(chest => chest.Index))
        {
            chest.Write(dataWriter);
        }
    }
}
