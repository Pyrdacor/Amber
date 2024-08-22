using Amber.Common;
using Amberstar.GameData;
using System.Diagnostics.CodeAnalysis;

namespace Amberstar.Game
{
	// Will basically have the savegame data and some additional temporary stuff
	internal class GameState
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

		public GameState()
		{
			TileChanges = new(new TileChangeComparer());

			// Initial savegame values follow

			#region Known words bits
			KnownWordsBits[5] = 4;
			#endregion

			#region Chest slot bits
			ChestSlotBits[0] = 14;
			ChestSlotBits[1] = 96;
			ChestSlotBits[3] = 30;
			ChestSlotBits[4] = 224;
			ChestSlotBits[5] = 255;
			ChestSlotBits[6] = 15;
			ChestSlotBits[7] = 224;
			ChestSlotBits[8] = 255;
			ChestSlotBits[9] = 7;
			ChestSlotBits[10] = 224;
			ChestSlotBits[11] = 255;
			ChestSlotBits[12] = 3;
			ChestSlotBits[13] = 224;
			ChestSlotBits[15] = 2;
			ChestSlotBits[16] = 32;
			ChestSlotBits[18] = 2;
			ChestSlotBits[19] = 32;
			ChestSlotBits[21] = 254;
			ChestSlotBits[22] = 231;
			ChestSlotBits[23] = 15;
			ChestSlotBits[24] = 254;
			ChestSlotBits[25] = 227;
			ChestSlotBits[26] = 255;
			ChestSlotBits[27] = 3;
			ChestSlotBits[28] = 224;
			ChestSlotBits[29] = 1;
			ChestSlotBits[30] = 2;
			ChestSlotBits[31] = 224;
			ChestSlotBits[32] = 127;
			ChestSlotBits[33] = 254;
			ChestSlotBits[34] = 33;
			ChestSlotBits[36] = 2;
			ChestSlotBits[37] = 32;
			ChestSlotBits[39] = 126;
			ChestSlotBits[40] = 224;
			ChestSlotBits[41] = 31;
			ChestSlotBits[42] = 2;
			ChestSlotBits[43] = 96;
			ChestSlotBits[45] = 6;
			ChestSlotBits[46] = 32;
			ChestSlotBits[48] = 2;
			ChestSlotBits[49] = 32;
			ChestSlotBits[51] = 2;
			ChestSlotBits[52] = 32;
			ChestSlotBits[54] = 2;
			ChestSlotBits[55] = 224;
			ChestSlotBits[56] = 255;
			ChestSlotBits[57] = 255;
			ChestSlotBits[58] = 227;
			ChestSlotBits[59] = 31;
			ChestSlotBits[60] = 254;
			ChestSlotBits[61] = 35;
			ChestSlotBits[63] = 30;
			ChestSlotBits[64] = 32;
			ChestSlotBits[66] = 62;
			ChestSlotBits[67] = 32;
			ChestSlotBits[69] = 2;
			ChestSlotBits[70] = 96;
			ChestSlotBits[72] = 2;
			ChestSlotBits[73] = 32;
			ChestSlotBits[75] = 30;
			ChestSlotBits[76] = 32;
			ChestSlotBits[78] = 2;
			ChestSlotBits[79] = 32;
			ChestSlotBits[81] = 2;
			ChestSlotBits[82] = 32;
			ChestSlotBits[84] = 2;
			ChestSlotBits[85] = 32;
			ChestSlotBits[87] = 14;
			ChestSlotBits[88] = 224;
			ChestSlotBits[90] = 2;
			ChestSlotBits[91] = 32;
			ChestSlotBits[93] = 14;
			ChestSlotBits[94] = 224;
			ChestSlotBits[96] = 6;
			ChestSlotBits[97] = 224;
			ChestSlotBits[99] = 2;
			ChestSlotBits[100] = 224;
			ChestSlotBits[101] = 127;
			ChestSlotBits[102] = 2;
			ChestSlotBits[103] = 32;
			ChestSlotBits[105] = 62;
			ChestSlotBits[106] = 96;
			ChestSlotBits[108] = 254;
			ChestSlotBits[109] = 63;
			ChestSlotBits[111] = 254;
			ChestSlotBits[112] = 239;
			ChestSlotBits[113] = 255;
			ChestSlotBits[114] = 2;
			ChestSlotBits[115] = 224;
			ChestSlotBits[116] = 15;
			ChestSlotBits[117] = 6;
			ChestSlotBits[118] = 32;
			ChestSlotBits[120] = 6;
			ChestSlotBits[121] = 224;
			ChestSlotBits[122] = 15;
			ChestSlotBits[123] = 2;
			ChestSlotBits[124] = 224;
			ChestSlotBits[125] = 255;
			ChestSlotBits[126] = 255;
			ChestSlotBits[127] = 127;
			ChestSlotBits[129] = 254;
			ChestSlotBits[130] = 255;
			ChestSlotBits[131] = 255;
			ChestSlotBits[132] = 255;
			ChestSlotBits[133] = 255;
			ChestSlotBits[134] = 255;
			ChestSlotBits[135] = 63;
			ChestSlotBits[136] = 224;
			ChestSlotBits[137] = 1;
			ChestSlotBits[138] = 254;
			ChestSlotBits[139] = 63;
			ChestSlotBits[141] = 2;
			ChestSlotBits[142] = 32;
			ChestSlotBits[144] = 254;
			ChestSlotBits[145] = 239;
			ChestSlotBits[146] = 255;
			ChestSlotBits[147] = 255;
			ChestSlotBits[148] = 255;
			ChestSlotBits[149] = 255;
			ChestSlotBits[150] = 255;
			ChestSlotBits[151] = 255;
			ChestSlotBits[152] = 7;
			ChestSlotBits[153] = 126;
			ChestSlotBits[154] = 32;
			ChestSlotBits[156] = 2;
			ChestSlotBits[157] = 32;
			ChestSlotBits[159] = 30;
			ChestSlotBits[160] = 32;
			ChestSlotBits[162] = 2;
			ChestSlotBits[163] = 96;
			ChestSlotBits[165] = 2;
			ChestSlotBits[166] = 32;
			ChestSlotBits[168] = 254;
			ChestSlotBits[169] = 225;
			ChestSlotBits[170] = 7;
			ChestSlotBits[171] = 254;
			ChestSlotBits[172] = 255;
			ChestSlotBits[173] = 3;
			ChestSlotBits[174] = 254;
			ChestSlotBits[175] = 224;
			ChestSlotBits[176] = 31;
			ChestSlotBits[177] = 2;
			ChestSlotBits[178] = 32;
			ChestSlotBits[180] = 14;
			ChestSlotBits[181] = 96;
			ChestSlotBits[183] = 2;
			ChestSlotBits[184] = 224;
			ChestSlotBits[186] = 2;
			ChestSlotBits[187] = 32;
			ChestSlotBits[189] = 14;
			ChestSlotBits[190] = 32;
			ChestSlotBits[192] = 2;
			ChestSlotBits[193] = 32;
			ChestSlotBits[195] = 6;
			ChestSlotBits[196] = 32;
			ChestSlotBits[198] = 6;
			ChestSlotBits[199] = 224;
			ChestSlotBits[200] = 255;
			ChestSlotBits[201] = 3;
			ChestSlotBits[202] = 224;
			ChestSlotBits[204] = 2;
			ChestSlotBits[205] = 224;
			ChestSlotBits[206] = 1;
			ChestSlotBits[207] = 254;
			ChestSlotBits[208] = 225;
			ChestSlotBits[210] = 6;
			ChestSlotBits[211] = 224;
			ChestSlotBits[213] = 6;
			ChestSlotBits[214] = 224;
			ChestSlotBits[215] = 1;
			ChestSlotBits[216] = 2;
			ChestSlotBits[217] = 32;
			ChestSlotBits[219] = 30;
			ChestSlotBits[220] = 224;
			ChestSlotBits[221] = 1;
			ChestSlotBits[222] = 2;
			ChestSlotBits[223] = 224;
			ChestSlotBits[224] = 31;
			ChestSlotBits[225] = 6;
			ChestSlotBits[226] = 224;
			ChestSlotBits[227] = 255;
			ChestSlotBits[228] = 31;
			ChestSlotBits[229] = 32;
			ChestSlotBits[231] = 6;
			ChestSlotBits[232] = 32;
			ChestSlotBits[234] = 2;
			ChestSlotBits[235] = 224;
			ChestSlotBits[237] = 6;
			ChestSlotBits[238] = 224;
			ChestSlotBits[239] = 255;
			ChestSlotBits[240] = 7;
			ChestSlotBits[241] = 224;
			ChestSlotBits[242] = 1;
			ChestSlotBits[243] = 254;
			ChestSlotBits[244] = 1;
			#endregion

			#region Chest gold
			ChestGold[2] = 589;
			ChestGold[3] = 125;
			ChestGold[4] = 174;
			ChestGold[5] = 98;
			ChestGold[6] = 487;
			ChestGold[15] = 3;
			ChestGold[16] = 158;
			ChestGold[17] = 89;
			ChestGold[18] = 325;
			ChestGold[21] = 26;
			ChestGold[22] = 125;
			ChestGold[23] = 12;
			ChestGold[27] = 12;
			ChestGold[28] = 1248;
			ChestGold[42] = 4281;
			ChestGold[52] = 1296;
			ChestGold[53] = 657;
			ChestGold[59] = 238;
			ChestGold[60] = 14;
			ChestGold[61] = 198;
			ChestGold[62] = 6;
			ChestGold[64] = 99;
			ChestGold[65] = 3;
			ChestGold[68] = 1215;
			ChestGold[73] = 1589;
			ChestGold[74] = 5000;
			ChestGold[75] = 875;
			ChestGold[76] = 621;
			ChestGold[78] = 2549;
			ChestGold[79] = 2857;
			ChestGold[80] = 2563;
			ChestGold[81] = 4256;
			ChestGold[85] = 795;
			ChestGold[96] = 6420;
			ChestGold[104] = 23;
			ChestGold[105] = 16;
			ChestGold[106] = 32;
			ChestGold[107] = 9;
			ChestGold[108] = 45;
			ChestGold[109] = 15;
			ChestGold[110] = 33;
			ChestGold[111] = 29;
			ChestGold[112] = 89;
			ChestGold[115] = 348;
			ChestGold[118] = 825;
			ChestGold[123] = 248;
			ChestGold[125] = 35;
			ChestGold[134] = 1489;
			ChestGold[136] = 2158;
			ChestGold[146] = 359;
			ChestGold[150] = 4484;
			ChestGold[152] = 845;
			ChestGold[155] = 859;
			ChestGold[156] = 546;
			#endregion

			#region Ware counts
			WareCounts[0] = 255;
			WareCounts[1] = 15;
			WareCounts[2] = 3;
			WareCounts[3] = 2;
			WareCounts[4] = 1;
			WareCounts[5] = 1;
			WareCounts[6] = 25;
			WareCounts[7] = 35;
			WareCounts[8] = 6;
			WareCounts[9] = 2;
			WareCounts[10] = 12;
			WareCounts[11] = 255;
			WareCounts[12] = 255;
			WareCounts[13] = 6;
			WareCounts[14] = 5;
			WareCounts[15] = 2;
			WareCounts[16] = 3;
			WareCounts[17] = 2;
			WareCounts[18] = 3;
			WareCounts[19] = 1;
			WareCounts[20] = 2;
			WareCounts[21] = 255;
			WareCounts[22] = 2;
			WareCounts[24] = 3;
			WareCounts[25] = 1;
			WareCounts[26] = 3;
			WareCounts[27] = 1;
			WareCounts[28] = 2;
			WareCounts[29] = 2;
			WareCounts[30] = 1;
			WareCounts[36] = 5;
			WareCounts[37] = 4;
			WareCounts[38] = 3;
			WareCounts[39] = 2;
			WareCounts[40] = 1;
			WareCounts[41] = 3;
			WareCounts[42] = 2;
			WareCounts[43] = 2;
			WareCounts[44] = 2;
			WareCounts[45] = 1;
			WareCounts[46] = 3;
			WareCounts[47] = 2;
			WareCounts[48] = 255;
			WareCounts[49] = 5;
			WareCounts[50] = 3;
			WareCounts[51] = 2;
			WareCounts[52] = 1;
			WareCounts[53] = 5;
			WareCounts[54] = 10;
			WareCounts[55] = 1;
			WareCounts[56] = 1;
			WareCounts[57] = 3;
			WareCounts[58] = 255;
			WareCounts[59] = 3;
			WareCounts[60] = 5;
			WareCounts[61] = 2;
			WareCounts[62] = 1;
			WareCounts[63] = 3;
			WareCounts[64] = 2;
			WareCounts[65] = 2;
			WareCounts[66] = 1;
			WareCounts[67] = 2;
			WareCounts[68] = 1;
			WareCounts[69] = 2;
			WareCounts[70] = 1;
			WareCounts[71] = 2;
			WareCounts[72] = 255;
			WareCounts[73] = 1;
			WareCounts[74] = 6;
			WareCounts[75] = 4;
			WareCounts[76] = 255;
			WareCounts[77] = 2;
			WareCounts[78] = 3;
			WareCounts[79] = 255;
			WareCounts[80] = 255;
			WareCounts[84] = 3;
			WareCounts[85] = 5;
			WareCounts[86] = 3;
			WareCounts[87] = 1;
			WareCounts[88] = 2;
			WareCounts[89] = 5;
			WareCounts[90] = 3;
			WareCounts[91] = 1;
			WareCounts[92] = 5;
			WareCounts[93] = 2;
			WareCounts[94] = 5;
			WareCounts[95] = 3;
			WareCounts[96] = 255;
			WareCounts[97] = 255;
			WareCounts[98] = 255;
			WareCounts[99] = 255;
			WareCounts[100] = 1;
			WareCounts[101] = 2;
			WareCounts[108] = 255;
			WareCounts[109] = 255;
			WareCounts[110] = 255;
			WareCounts[111] = 255;
			WareCounts[112] = 255;
			WareCounts[113] = 255;
			WareCounts[114] = 255;
			WareCounts[115] = 5;
			WareCounts[116] = 5;
			WareCounts[120] = 255;
			WareCounts[121] = 25;
			WareCounts[122] = 15;
			WareCounts[123] = 10;
			WareCounts[124] = 5;
			WareCounts[125] = 255;
			WareCounts[126] = 255;
			WareCounts[127] = 255;
			WareCounts[132] = 15;
			WareCounts[133] = 5;
			WareCounts[134] = 2;
			WareCounts[135] = 10;
			WareCounts[136] = 20;
			WareCounts[137] = 6;
			WareCounts[138] = 6;
			WareCounts[139] = 5;
			WareCounts[140] = 2;
			WareCounts[141] = 25;
			WareCounts[142] = 3;
			WareCounts[143] = 10;
			WareCounts[144] = 5;
			WareCounts[145] = 2;
			WareCounts[146] = 1;
			WareCounts[147] = 15;
			WareCounts[148] = 10;
			WareCounts[149] = 5;
			WareCounts[150] = 5;
			WareCounts[151] = 3;
			WareCounts[152] = 20;
			WareCounts[153] = 15;
			WareCounts[154] = 5;
			WareCounts[155] = 5;
			WareCounts[156] = 2;
			WareCounts[157] = 1;
			WareCounts[158] = 1;
			WareCounts[159] = 255;
			WareCounts[160] = 5;
			WareCounts[161] = 25;
			WareCounts[162] = 1;
			WareCounts[168] = 10;
			WareCounts[169] = 5;
			WareCounts[170] = 3;
			WareCounts[171] = 3;
			WareCounts[180] = 255;
			WareCounts[181] = 10;
			WareCounts[182] = 3;
			WareCounts[183] = 1;
			WareCounts[184] = 1;
			WareCounts[185] = 1;
			WareCounts[186] = 5;
			WareCounts[187] = 3;
			WareCounts[188] = 5;
			WareCounts[189] = 10;
			WareCounts[190] = 2;
			WareCounts[191] = 5;
			#endregion
		}

		public GameState(ISavegame savegame)
		{
			// Time and Date
			Year = savegame.Year;
			Month = savegame.Month;
			Day = savegame.Day;
			Hour = savegame.Hour;
			Minute = savegame.Minute;
			TravelledDays = savegame.TravelledDays;
			RelativeYear = savegame.RelativeYear;

			// Party
			PartySize = savegame.PartySize;
			ActivePartyMember = savegame.ActivePartyMember;
			PartyCharacterIndices = savegame.PartyCharacterIndices;
			CombatPositions = savegame.CombatPositions;
			ActiveSpells = savegame.ActiveSpells;
			SpecialItems = savegame.SpecialItems;
			TravelType = savegame.TravelType;

			// Map
			MapIndex = savegame.MapIndex;
			PartyX = savegame.PartyX;
			PartyY = savegame.PartyY;
			PartyDirection = savegame.PartyDirection;
			Transports = savegame.Transports;
			TileChanges = new(savegame.TileChanges, new TileChangeComparer());

			// Misc
			MusicBlock = savegame.MusicBlock;
			QuestBits = savegame.QuestBits;
			EventBits = savegame.EventBits;
			CharacterBits = savegame.CharacterBits;
			KnownWordsBits = savegame.KnownWordsBits;
			ChestSlotBits = savegame.ChestSlotBits;
			ChestGold = savegame.ChestGold;
			WareCounts = savegame.WareCounts;
		}

		#region Time and Date

		public int Year { get; set; } = 876;
		public int Month { get; set; } = 4;
		public int Day { get; set; } = 15;
		public int Hour { get; set; } = 16;
		public int Minute { get; set; } = 0;
		public int TravelledDays { get; set; } = 0;
		public int RelativeYear { get; set; } = 0;

		#endregion


		#region Party

		public int PartySize { get; private set; } = 1;
		public int ActivePartyMember { get; private set; } = 1;
		public int[] PartyCharacterIndices { get; } = [1, 0, 0, 0, 0, 0];
		public int[] CombatPositions { get; } = [1, 2, 3, 4, 8, 9];
		public ActiveSpell[] ActiveSpells { get; } = new ActiveSpell[ISavegame.ActiveSpellCount];
		public SpecialItems SpecialItems { get; set; } = SpecialItems.None;
		public TravelType TravelType { get; set; } = TravelType.Walk;
		public bool HasWindChain => SpecialItems.HasFlag(SpecialItems.WindChain);

		public bool TryAddPartyMember(int index)
		{
			if (PartySize == ISavegame.MaxPartyMembers)
				return false;

			PartyCharacterIndices[ISavegame.MaxPartyMembers] = index;
			PartySize++;

			return true;
		}

		#endregion


		#region Map

		public int MapIndex { get; set; } = 65; // 65 is the Twinlake graveyard
		int PartyX { get; set; } = 8;
		int PartyY { get; set; } = 11;
		public Direction PartyDirection { get; set; } = Direction.Right;
		Transport[] Transports { get; } = new Transport[ISavegame.MaxTransportCount];
		HashSet<TileChange> TileChanges { get; } = [];
		public Position PartyPosition => new(PartyX - 1, PartyY - 1);

		public void SetPartyPosition(int x, int y)
		{
			PartyX = x + 1;
			PartyY = y + 1;
		}

		public Transport? GetTransportAsLocation(int x, int y, int? mapIndex = null)
		{
			mapIndex ??= MapIndex;

			for (int i = 0; i < ISavegame.MaxTransportCount; i++)
			{
				if (Transports[i].MapIndex == mapIndex && Transports[i].X == x && Transports[i].Y == y)
					return Transports[i];
			}

			return null;
		}

		public Transport[] GetTransportsOnMap(int? mapIndex = null)
		{
			mapIndex ??= MapIndex;

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


		#region Misc

		/// <summary>
		/// ?, docs say (0 / -1)
		/// </summary>
		bool MusicBlock { get; set; } = false;
		/// <summary>
		/// 32 bytes = 256 bits
		/// </summary>
		byte[] QuestBits { get; } = new byte[32];
		/// <summary>
		/// If bit is set, the event is deactivated / removed.
		/// 
		/// 4064 bytes (64 or 65 events per map?, 500 maps with 64 would be 4000 bytes, with 65 it would be 4062 bytes ...)
		/// </summary>
		byte[] EventBits { get; } = new byte[4064];
		/// <summary>
		/// If bit is set, the character is deactivated / removed.
		/// 
		/// 1502 bytes (24 characters per map, max 500 maps, 3 bytes per map)
		/// </summary>
		byte[] CharacterBits { get; } = new byte[1502];
		byte[] KnownWordsBits { get; } = new byte[626];
		/// <summary>
		/// 12000 bits, as there are 12 item slots I think 1000 chest are possible.
		/// </summary>
		byte[] ChestSlotBits { get; } = new byte[1500];
		/// <summary>
		/// One word value for every of the 1000 chests.
		/// </summary>
		int[] ChestGold { get; } = new int[ISavegame.MaxChests];
		/// <summary>
		/// The item count per item slot at merchants.
		/// There are up to 1000 merchants with 12 items slots.
		/// So 1200 values here.
		/// </summary>
		int[] WareCounts { get; } = new int[ISavegame.MaxMerchants * ISavegame.MaxMerchantSlots];

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

		int BitFromMapEventIndex(int mapIndex, int eventIndex) => (mapIndex - 1) * 65 + eventIndex;

		public bool IsEventActive(int mapIndex, int eventIndex) => IsBitSet(EventBits, BitFromMapEventIndex(mapIndex, eventIndex));

		public void SaveEvent(int mapIndex, int mapCharIndex) => SetBit(EventBits, BitFromMapEventIndex(mapIndex, mapCharIndex), true);

		int BitFromMapCharIndex(int mapIndex, int mapCharIndex) => (mapIndex - 1) * 24 + mapCharIndex;

		public bool IsMapCharacterActive(int mapIndex, int mapCharIndex) => IsBitSet(CharacterBits, BitFromMapCharIndex(mapIndex, mapCharIndex));

		public void SetMapCharacterActive(int mapIndex, int mapCharIndex, bool active) => SetBit(CharacterBits, BitFromMapCharIndex(mapIndex, mapCharIndex), !active);

		public void LearnWord(int index) => SetBit(KnownWordsBits, index, true);

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

		#endregion
	}
}
