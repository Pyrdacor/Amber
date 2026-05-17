using Amber.Serialization;

namespace Amberstar.GameData;

public struct ActiveSpell
{
	public int Duration { get; set; }
	public int Value { get; set; }
}

public struct Transport
{
	public TransportType Type { get; set; }
	public int X { get; set; }
	public int Y { get; set; }
	public int MapIndex { get; set; }
}

public struct TileChange
{
	public int X { get; set; }
	public int Y { get; set; }
	public int MapIndex { get; set; }
	public int TileIndex { get; set; }
}

public interface ISavegame
{
	int Year { get; set; }
	int Month { get; set; }
	int Day { get; set; }
	int Hour { get; set; }
	int Minute { get; set; }
	int TravelledDays { get; set; }
	int RelativeYear { get; set; }
	int MapIndex { get; set; }
	int PartyX { get; set; }
	int PartyY { get; set; }
	Direction PartyDirection { get; set; }
	/// <summary>
	/// 6
	/// </summary>
	ActiveSpell[] ActiveSpells { get; }
	/// <summary>
	/// 30
	/// </summary>
	Transport[] Transports { get; }
	/// <summary>
	/// 0-6
	/// </summary>
	int PartySize { get; set; }
	/// <summary>
	/// 0-6
	/// </summary>
	int ActivePartyMember { get; set; }
	int[] PartyCharacterIndices { get; }
	int[] CombatPositions { get; }
	TravelType TravelType { get; set; }
	SpecialItems SpecialItems { get; set; }
	/// <summary>
	/// ?, docs say (0 / -1)
	/// </summary>
	bool MusicBlock { get; set; }
	/// <summary>
	/// 32 bytes = 256 bits
	/// </summary>
	byte[] QuestBits { get; }
	/// <summary>
	/// If bit is set, the event is deactivated / removed.
	/// 
	/// The original code allows to save up to 65 events per map.
	/// There are 500 maps at max, so 500 * 65 = 4062 bytes.
	/// However this is stored as 4064 bytes in the savegame.
	/// Most likely a typo/miscalucaltion but it is what it is.
	/// </summary>
	byte[] EventBits { get; }
	/// <summary>
	/// If bit is set, the character is deactivated / removed.
	/// 
	/// 1502 bytes (24 characters per map, max 500 maps, 3 bytes per map)
	/// </summary>
	byte[] CharacterBits { get; }
	byte[] KnownWordsBits { get; }
	/// <summary>
	/// 12000 bits, as there are 12 item slots I think 1000 chest are possible.
	/// </summary>
	byte[] ChestSlotBits { get; }
	/// <summary>
	/// One word value for every of the 1000 chests.
	/// </summary>
	int[] ChestGold { get; }
	/// <summary>
	/// The item count per item slot at merchants.
	/// There are up to 1000 merchants with 12 items slots.
	/// So 1200 values here.
	/// </summary>
	int[] WareCounts { get; }

	TileChange[] TileChanges { get; set; }

	void Read(IDataReader dataReader);
	void Write(IDataWriter dataWriter);


    public const int MaxTransportCount = 30;
	public const int ActiveSpellCount = 6;
	public const int MaxPartyMembers = 6;
	public const int MaxQuestBits = 32 * 8;
	public const int MaxEventBits = 4064 * 8;
	public const int MaxSavedEventsPerMap = 65;
	public const int MaxCharacterBits = 1502 * 8;
	public const int MaxKnownWordsBits = 626 * 8;
	public const int MaxKnownWords = MaxKnownWordsBits;
	public const int MaxChestSlotBits = 1500 * 8;
	public const int MaxChests = 1000;
	public const int MaxChestSlots = 12;
	public const int MaxMerchants = 1000;
	public const int MaxMerchantSlots = 12;
}