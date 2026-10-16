using Amber.Common;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy;

// 4 bytes per spell, 30 spells for each of the 7 spell schools:
// - location flags
// - spell point cost
// - spell learning point cost
// - target flags
internal sealed class SpellData(SpellSchool school, int spellNumber, byte[] data) : ISpellData
{
	public const int DataSize = 4;
	public const int TotalSize = 7 * ISpellData.SpellsPerSchool * DataSize;

	public SpellSchool School { get; } = school;
	public int SpellNumber { get; } = spellNumber;
	public byte LocationFlags => data[0];
	public byte SpellPointCost => data[1];
	public byte SpellLearningPointCost => data[2];
	public byte TargetFlags => data[3];
}

internal sealed class SpellDataLoader(Func<byte[]> spellData) : ISpellDataLoader
{
	public ISpellData LoadSpellData(SpellSchool school, int spellNumber)
	{
		if (school == SpellSchool.None || spellNumber < 1 || spellNumber > ISpellData.SpellsPerSchool)
			throw new AmberException(ExceptionScope.Data, $"Invalid spell {spellNumber} of school {school}.");

		int offset = (((int)school - 1) * ISpellData.SpellsPerSchool + spellNumber - 1) * SpellData.DataSize;

		return new SpellData(school, spellNumber, spellData()[offset..(offset + SpellData.DataSize)]);
	}
}
