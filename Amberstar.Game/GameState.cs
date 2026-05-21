using Amber.Common;
using Amberstar.Game.Screens;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace Amberstar.Game;

// Will basically have the savegame data and some additional temporary stuff
internal class GameState(ISavegame savegame, IAssetProvider assetProvider)
{
	class TileChangeComparer : IEqualityComparer<TileChange>
	{
		public bool Equals(TileChange x, TileChange y)
		{
			return GetHashCode(x) == GetHashCode(y);
		}

		public int GetHashCode([DisallowNull] TileChange obj)
		{
			return HashCode.Combine(obj.MapIndex, obj.X, obj.Y);
		}
	}

	readonly Dictionary<int, IPartyMember> partyMembers = assetProvider.PersonLoader.GetPartyMemberCopies();

    public ISavegame ToSavegame(ISavegameLoader savegameLoader)
    {
        int mapIndex = MapIndex;
        int partyX = PartyX;
        int partyY = PartyY;

        // Adjust map index and party position for world maps.
        if (WorldMap && partyX > Map2DScreen.WorldMapWidth)
        {
            mapIndex = Map2DScreen.GetWorldMapIndex(mapIndex, 1, 0);
            partyX -= Map2DScreen.WorldMapWidth;
        }
        if (WorldMap && partyY > Map2DScreen.WorldMapHeight)
        {
            mapIndex = Map2DScreen.GetWorldMapIndex(mapIndex, 0, 1);
            partyY -= Map2DScreen.WorldMapHeight;
        }

        var savegame = savegameLoader.Create();

        // Basic fields
        savegame.Year = Year;
        savegame.Month = Month;
        savegame.Day = Day;
        savegame.Hour = Hour;
        savegame.Minute = Minute;
        savegame.TravelledDays = TravelledDays;
        savegame.RelativeYear = RelativeYear;

        savegame.MapIndex = mapIndex;
        savegame.PartyX = partyX;
        savegame.PartyY = partyY;
        savegame.PartyDirection = PartyDirection;

        savegame.SpecialItems = SpecialItems;
        savegame.TravelType = TravelType;
        savegame.MusicBlock = MusicBlock;

        savegame.PartySize = PartySize;
        savegame.ActivePartyMember = ActivePartyMemberIndex;

        // Helper to copy arrays safely
        static void Copy<T>(T[] source, T[] destination)
        {
            if (source == null || destination == null) return;

            int copyLength = source.Length < destination.Length
                ? source.Length
                : destination.Length;

            for (int i = 0; i < copyLength; i++)
                destination[i] = source[i];
        }

        Copy(ActiveSpells, savegame.ActiveSpells);
        Copy(Transports, savegame.Transports);
        Copy(PartyCharacterIndices, savegame.PartyCharacterIndices);
        Copy(CombatPositions, savegame.CombatPositions);
        Copy(QuestBits, savegame.QuestBits);
        Copy(EventBits, savegame.EventBits);
        Copy(CharacterBits, savegame.CharacterBits);
        Copy(KnownWordsBits, savegame.KnownWordsBits);
        Copy(ChestSlotBits, savegame.ChestSlotBits);
        Copy(ChestGold, savegame.ChestGold);
        Copy(WareCounts, savegame.WareCounts);

        // Tile changes
        savegame.TileChanges = [.. TileChanges];

        return savegame;
    }

    public void LoadFrom(ISavegame savegame)
    {
        Year = savegame.Year;
        Month = savegame.Month;
        Day = savegame.Day;
        Hour = savegame.Hour;
        Minute = savegame.Minute;
        TravelledDays = savegame.TravelledDays;
        RelativeYear = savegame.RelativeYear;

        MapIndex = savegame.MapIndex;
        PartyX = savegame.PartyX;
        PartyY = savegame.PartyY;
        PartyDirection = savegame.PartyDirection;

        SpecialItems = savegame.SpecialItems;
        TravelType = savegame.TravelType;
        MusicBlock = savegame.MusicBlock;

        static void Copy<T>(T[] source, T[] destination)
        {
            if (source == null || destination == null) return;

            int copyLength = source.Length < destination.Length
				? source.Length
				: destination.Length;

            for (int i = 0; i < copyLength; i++)
				destination[i] = source[i];
        }

        Copy(savegame.ActiveSpells, ActiveSpells);
        Copy(savegame.Transports, Transports);
        Copy(savegame.PartyCharacterIndices, PartyCharacterIndices);
        Copy(savegame.CombatPositions, CombatPositions);
        Copy(savegame.QuestBits, QuestBits);
        Copy(savegame.EventBits, EventBits);
        Copy(savegame.CharacterBits, CharacterBits);
        Copy(savegame.KnownWordsBits, KnownWordsBits);
        Copy(savegame.ChestSlotBits, ChestSlotBits);
        Copy(savegame.ChestGold, ChestGold);
        Copy(savegame.WareCounts, WareCounts);

        PartySize = savegame.PartySize;
        ActivePartyMemberIndex = savegame.ActivePartyMember;

        TileChanges.Clear();

        if (savegame.TileChanges != null)
        {
            foreach (var tileChange in savegame.TileChanges)
                TileChanges.Add(tileChange);
        }
    }


    #region Time and Date

    public int Year { get; set; } = savegame.Year;
    public int Month { get; set; } = savegame.Month;
    public int Day { get; set; } = savegame.Day;
    public int Hour { get; set; } = savegame.Hour;
    public int Minute { get; set; } = savegame.Minute;
    public int TravelledDays { get; set; } = savegame.TravelledDays;
    public int RelativeYear { get; set; } = savegame.RelativeYear;

    #endregion


    #region Party

    public int? CurrentInventoryIndex { get; set; } = null;
    public int PartySize { get; private set; } = savegame.PartySize;
    public int ActivePartyMemberIndex { get; private set; } = savegame.ActivePartyMember;
    public int[] PartyCharacterIndices { get; } = savegame.PartyCharacterIndices;
    public int[] CombatPositions { get; } = savegame.CombatPositions;
    public ActiveSpell[] ActiveSpells { get; } = savegame.ActiveSpells;
    public SpecialItems SpecialItems { get; set; } = savegame.SpecialItems;
    public TravelType TravelType { get; set; } = savegame.TravelType;
    public bool HasWindChain => SpecialItems.HasFlag(SpecialItems.WindChain);

	public event Action? ActivePartyMemberChanged;
    public event Action? CurrentInventoryChanged;

    public bool TryAddPartyMember(int index)
	{
		if (PartySize == ISavegame.MaxPartyMembers)
			return false;

		PartyCharacterIndices[ISavegame.MaxPartyMembers] = index;
		PartySize++;

		return true;
	}

	public IPartyMember? CurrentInventory
    {
		get
		{
			if (CurrentInventoryIndex == null || CurrentInventoryIndex < 1 || CurrentInventoryIndex > PartySize)
				return null;

			int characterIndex = PartyCharacterIndices[CurrentInventoryIndex!.Value - 1];

			if (characterIndex == 0)
				return null;

			return partyMembers[characterIndex];
		}
    }

    public IPartyMember? ActivePartyMember
    {
        get
        {
            if (ActivePartyMemberIndex < 1 || ActivePartyMemberIndex > PartySize)
                return null;

            int characterIndex = PartyCharacterIndices[ActivePartyMemberIndex - 1];

            if (characterIndex == 0)
                return null;

            return partyMembers[characterIndex];
        }
    }

	public bool HasPartyMemberInSlot(int slotIndex)
	{
        if (slotIndex < 1 || slotIndex > Game.MaxPartyMembers)
            return false;

        int characterIndex = PartyCharacterIndices[slotIndex - 1];

		return characterIndex != 0;
    }

    public IPartyMember? GetPartyMember(int slotIndex)
    {
        if (slotIndex < 1 || slotIndex > Game.MaxPartyMembers)
            return null;

        int characterIndex = PartyCharacterIndices[slotIndex - 1];

        if (characterIndex == 0)
            return null;

        return partyMembers[characterIndex];
    }

	public void SetActivePartyMember(int slotIndex)
	{
		if (slotIndex < 1 || slotIndex > PartySize || PartyCharacterIndices[slotIndex - 1] == 0)
			return;

		ActivePartyMemberIndex = slotIndex;
		ActivePartyMemberChanged?.Invoke();
    }

    public void SetCurrentInventory(int? slotIndex)
    {
        if (slotIndex != null && (slotIndex < 1 || slotIndex > PartySize))
            return;

        CurrentInventoryIndex = slotIndex;
        CurrentInventoryChanged?.Invoke();
    }

    public IEnumerable<IPartyMember> PartyMembers
	{
		get
		{
			for (int i = 0; i < Game.MaxPartyMembers; i++)
			{
				int characterIndex = PartyCharacterIndices[i];

				if (characterIndex == 0)
					continue;

				yield return partyMembers[characterIndex];
			}
		}
    }

    public IEnumerable<(int SlotIndex, IPartyMember? PartyMember)> PartyMembersWithSlot
    {
		get
		{
			for (int i = 0; i < Game.MaxPartyMembers; i++)
			{
				int characterIndex = PartyCharacterIndices[i];

				if (characterIndex != 0)
					yield return (i, partyMembers[characterIndex]);
				else
					yield return (i, null);
			}
		}
    }

    #endregion


    #region Map

    public int MapIndex { get; set; } = savegame.MapIndex;
    int PartyX { get; set; } = savegame.PartyX;
    int PartyY { get; set; } = savegame.PartyY;
    public Direction PartyDirection { get; set; } = savegame.PartyDirection;
    Transport[] Transports { get; } = savegame.Transports;
    HashSet<TileChange> TileChanges { get; } = new(savegame.TileChanges, new TileChangeComparer());
    public Position LastPosition { get; private set; } = new(7, 10);
    public Position PartyPosition => new(PartyX - 1, PartyY - 1);
	bool WorldMap { get; set; } = false;

	public int GetIndexOfMapWithPlayer()
	{
		int mapIndex = MapIndex;

		if (!WorldMap)
			return mapIndex;

		// Adjust map index for world maps.
		if (WorldMap && PartyX > Map2DScreen.WorldMapWidth)
			mapIndex = Map2DScreen.GetWorldMapIndex(mapIndex, 1, 0);
		if (WorldMap && PartyY > Map2DScreen.WorldMapHeight)
			mapIndex = Map2DScreen.GetWorldMapIndex(mapIndex, 0, 1);

		return mapIndex;
	}

	public void SetPartyPosition(int x, int y)
	{
		LastPosition = new(PartyX - 1, PartyY - 1);
        PartyX = x + 1;
		PartyY = y + 1;
	}

    public bool ResetPartyPosition()
    {
        int oldX = PartyX;
        int oldY = PartyY;

        PartyX = LastPosition.X + 1;
        PartyY = LastPosition.Y + 1;

        return oldX != PartyX || oldY != PartyY;
    }

    public void SetIsWorldMap(bool worldMap)
	{
		WorldMap = worldMap;
	}

	public Transport? GetTransportAtLocation(int x, int y, int? mapIndex = null)
	{
		mapIndex ??= GetIndexOfMapWithPlayer();

		for (int i = 0; i < ISavegame.MaxTransportCount; i++)
		{
			if (Transports[i].MapIndex == mapIndex && Transports[i].X == x && Transports[i].Y == y)
				return Transports[i];
		}

		return null;
	}

	public Transport[] GetTransportsOnMap(int? mapIndex = null)
	{
		mapIndex ??= GetIndexOfMapWithPlayer();

		return Transports.Where(transport => transport.MapIndex == mapIndex).ToArray();
	}

	public void SaveTileChange(int mapIndex, int x, int y, int tileIndex)
	{
		var tileChange = new TileChange()
		{
			MapIndex = mapIndex,
			X = x,
			Y = y,
			TileIndex = tileIndex
		};

		// Remove as we might change the tile index.
		// It finds entries with same map, x and y.
		TileChanges.Remove(tileChange);

		TileChanges.Add(tileChange);
	}

    #endregion


    #region Conversation

    public int? CurrentConversationCharacterIndex { get; set; } = null;

    #endregion


    #region Misc

    /// <summary>
    /// ?, docs say (0 / -1)
    /// </summary>
    bool MusicBlock { get; set; } = savegame.MusicBlock;
    /// <summary>
    /// 32 bytes = 256 bits
    /// </summary>
    byte[] QuestBits { get; } = savegame.QuestBits;
    /// <summary>
    /// If bit is set, the event is deactivated / removed.
    /// 
    /// 4064 bytes (64 or 65 events per map?, 500 maps with 64 would be 4000 bytes, with 65 it would be 4062 bytes ...)
    /// </summary>
    byte[] EventBits { get; } = savegame.EventBits;
    /// <summary>
    /// If bit is set, the character is deactivated / removed.
    /// 
    /// 1502 bytes (24 characters per map, max 500 maps, 3 bytes per map)
    /// </summary>
    byte[] CharacterBits { get; } = savegame.CharacterBits;
    byte[] KnownWordsBits { get; } = savegame.KnownWordsBits;
    /// <summary>
    /// 12000 bits, as there are 12 item slots I think 1000 chests are possible.
    /// </summary>
    byte[] ChestSlotBits { get; } = savegame.ChestSlotBits;
    /// <summary>
    /// One word value for every of the 1000 chests.
    /// </summary>
    int[] ChestGold { get; } = savegame.ChestGold;
    /// <summary>
    /// The item count per item slot at merchants.
    /// There are up to 1000 merchants with 12 items slots.
    /// So 1200 values here.
    /// </summary>
    int[] WareCounts { get; } = savegame.WareCounts;

    static void SetBit(byte[] bits, int bit, bool set)
	{
		if (set)
		{
			int mask = 1 << (bit & 0x7);
			bits[bit >> 3] |= (byte)mask;
		}
		else
		{
			int mask = 1 << (bit & 0x7);
			bits[bit >> 3] &= (byte)(~mask & 0xff);
		}
	}

	static bool IsBitSet(byte[] bits, int bit)
	{
		return (bits[bit >> 3] & (1 << (bit & 0x7))) != 0;
	}

	public bool IsQuestBitSet(int bit) => IsBitSet(QuestBits, bit);

	public void SetQuestBit(int bit, bool set = true) => SetBit(QuestBits, bit, set);

    // NOTE: This assumes that eventIndex is 1-based!
    static int BitFromMapEventIndex(int mapIndex, int eventIndex) => (mapIndex - 1) * 65 + eventIndex;

	public bool IsEventActive(int mapIndex, int eventIndex) => !IsBitSet(EventBits, BitFromMapEventIndex(mapIndex, eventIndex));

	public void SaveEvent(int mapIndex, int eventIndex) => SetBit(EventBits, BitFromMapEventIndex(mapIndex, eventIndex), true);

    // NOTE: This assumes that mapCharIndex is 1-based!
    static int BitFromMapCharIndex(int mapIndex, int mapCharIndex) => (mapIndex - 1) * 24 + mapCharIndex;

	public bool IsMapCharacterActive(int mapIndex, int mapCharIndex) => !IsBitSet(CharacterBits, BitFromMapCharIndex(mapIndex, mapCharIndex));

	public void SetMapCharacterActive(int mapIndex, int mapCharIndex, bool active) => SetBit(CharacterBits, BitFromMapCharIndex(mapIndex, mapCharIndex), !active);

	public void LearnWord(int index) => SetBit(KnownWordsBits, index, true);

    // NOTE: This assumes that index is 1-based!
    public bool IsWordKnown(int index) => IsBitSet(KnownWordsBits, index);

    public int[] GetWareCounts(int merchantIndex)
	{
		return WareCounts.Skip((merchantIndex - 1) * ISavegame.MaxMerchantSlots).Take(ISavegame.MaxMerchantSlots).ToArray();
	}

	public void SetWareCounts(int merchantIndex, int[] wareCounts)
	{
		int offset = (merchantIndex - 1) * ISavegame.MaxMerchantSlots;

		for (int i = 0; i < ISavegame.MaxMerchantSlots; i++)
		{
			WareCounts[offset + i] = wareCounts[i];
		}
	}

	public int GetChestGold(int chestIndex) => ChestGold[chestIndex];
	
	public int PutGoldToChest(int chestIndex, int amount)
	{
		ChestGold[chestIndex] += amount;

		if (ChestGold[chestIndex] > short.MaxValue)
		{
			int remaining = ChestGold[chestIndex] - short.MaxValue;
			ChestGold[chestIndex] = short.MaxValue;
			return remaining;
		}

		return 0;
	}

    public bool ChestHasItems(int chestIndex) => GetChestSlotBits(chestIndex) != 0;

    public int GetChestSlotBits(int chestIndex)
    {
        // Note: While the ChestGold array directly uses the chestIndex as
        // the index, for the ChestBits, the index is calculated differently.
        int index = chestIndex - 1;
        int offset = 3 * (index / 2);
        int chestBits;

        // The chest slot bits are stored as 12 bits per chest.
        // Binary: AAAAAAA0 BBBAAAAA BBBBBBBB CCCCCCCB DDDCCCCC ...

        // Read 12 bits per chest.
        if ((index & 0x1) == 0)
        {
            // Read 12 bits even (1.5 bytes)
            chestBits = ChestSlotBits[offset + 1];
            chestBits <<= 8;
            chestBits |= ChestSlotBits[offset];
            chestBits >>= 1;
        }
        else
        {
            chestBits = ChestSlotBits[offset + 2];
            chestBits <<= 8;
            chestBits |= ChestSlotBits[offset + 1];
            chestBits >>= 5;

            if ((ChestSlotBits[offset + 3] & 0x1) == 0x1)
                chestBits |= 0x800;
        }

        return chestBits & 0xfff;
    }

    public void SetChestSlotBit(int chestIndex, int bit, bool set)
    {
        int index = chestIndex - 1;
        int offset = 3 * (index / 2);

        void ChangeBit(int byteIndex, int bitIndex)
        {
            if (set)
                ChestSlotBits[byteIndex] |= (byte)(1 << bitIndex);
            else
                ChestSlotBits[byteIndex] &= (byte)~(1 << bitIndex);
        }

        // The chest slot bits are stored as 12 bits per chest.
        // Binary: AAAAAAA0 BBBAAAAA BBBBBBBB CCCCCCCB DDDCCCCC ...

        if ((index & 0x1) == 0)
        {
            if (bit < 7)
                ChangeBit(offset, bit + 1);
            else
                ChangeBit(offset + 1, bit - 7);
        }
        else
        {
            if (bit < 3)
                ChangeBit(offset + 1, bit + 5);
            else if (bit == 11)
                ChangeBit(offset + 3, 0);
            else
                ChangeBit(offset + 2, bit - 3);
        }
    }

    #endregion
}
