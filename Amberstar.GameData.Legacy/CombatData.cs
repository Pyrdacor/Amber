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

	public CombatData(byte[] data)
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
}
