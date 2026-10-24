using Amber.Assets.Common;
using Amber.Serialization;

namespace Amberstar.GameData.Legacy;

/// <summary>
/// The damage table of the black magic spells in the program data. It has 6 bytes per spell
/// (min, max and animation words) starting with an unused entry 0. Its last 4 bytes are the
/// immunity flag bits of the 4 elements.
/// </summary>
internal class CombatData : ICombatData
{
	public const int DamageSpellCount = 20;
	const int EntrySize = 6;
	readonly SpellDamage[] spellDamages = new SpellDamage[DamageSpellCount + 1];
	readonly byte[] elementBits = [0, 2, 1, 3];
	// The base palette has 16 colors. The colors 12-14 are given for each background
	// by a table which starts inside the palette (at color 13).
	const int PaletteSize = 32;
	const int BackgroundColorsOffset = 26;
	const int MonsterGraphicCount = 21;
	readonly IGraphic[] palettes = new IGraphic[ICombatData.CombatBackgroundCount + 1];
	const int ColumnCount = 6;
	const int MonsterRowCount = 3;
	readonly MonsterGraphicPosition[] monsterPositions = new MonsterGraphicPosition[ColumnCount * MonsterRowCount];
	readonly int[][] monsterFrameCounts = new int[MonsterGraphicCount + 1][];

	public static int PaletteDataSize => BackgroundColorsOffset + (ICombatData.CombatBackgroundCount + 1) * 6;

	public static int MonsterGraphicDataSize => ColumnCount * MonsterRowCount * 6 + MonsterGraphicCount * 4;

	public CombatData(byte[] data, byte[] paletteData, byte[] monsterGraphicData)
		: this(data)
	{
		for (int background = 1; background <= ICombatData.CombatBackgroundCount; background++)
		{
			var palette = paletteData.Length == 0 ? new byte[PaletteSize] : paletteData[..PaletteSize];

			if (paletteData.Length != 0)
				Array.Copy(paletteData, BackgroundColorsOffset + background * 6, palette, 12 * 2, 6);

			palettes[background] = PaletteLoader.LoadPalette(new DataReader(palette));
		}

		palettes[0] = palettes[1];

		var reader = new DataReader(monsterGraphicData.Length == 0 ? new byte[MonsterGraphicDataSize] : monsterGraphicData);

		for (int i = 0; i < monsterPositions.Length; i++)
			monsterPositions[i] = new(reader.ReadWord(), reader.ReadWord(), reader.ReadWord());

		monsterFrameCounts[0] = [0, 0, 0];

		for (int i = 1; i <= MonsterGraphicCount; i++)
		{
			var counts = reader.ReadBytes(4);
			monsterFrameCounts[i] = [counts[0], counts[1], counts[2]];
		}
	}

	CombatData(byte[] data)
	{
		if (data.Length == 0)
		{
			Array.Fill(spellDamages, new(0, 0, 0));
			return;
		}

		var reader = new DataReader(data);
		reader.Position = 2;
		elementBits = reader.ReadBytes(4);

		for (int i = 1; i <= DamageSpellCount; i++)
		{
			reader.Position = i * EntrySize;
			spellDamages[i] = new(reader.ReadWord(), reader.ReadWord(), reader.ReadWord());
		}
	}

	public static int Size => (DamageSpellCount + 1) * EntrySize;

	public SpellDamage GetSpellDamage(int blackSpellNumber) => spellDamages[blackSpellNumber];

	public MonsterElementalFlags GetElementImmunity(int element) => (MonsterElementalFlags)(1 << elementBits[element & 3]);

	public IGraphic GetPalette(int background) => palettes[Math.Clamp(background, 1, ICombatData.CombatBackgroundCount)];

	/// <summary>
	/// The rows are given from the back row (0) to the front row (2).
	/// </summary>
	public MonsterGraphicPosition GetMonsterPosition(int column, int row) => monsterPositions[row * ColumnCount + column];

	public IReadOnlyList<int> GetMonsterFrameCounts(int monsterGraphic) => monsterFrameCounts[monsterGraphic];
}
