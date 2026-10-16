using Amber.Common;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy;

// 394 bytes per class:
// - 10 bytes: maximum skill values
// - 90 longs: experience needed for the next level
// - 11 words: special values (attacks per round level, hit points, spell points
//             and spell learning points per level, the rest is unknown)
// - 1 byte: spell schools
// - 1 byte: unknown
internal sealed class ClassData(Class @class, byte[] data) : IClassData
{
	public const int DataSize = 394;
	const int ExperienceOffset = 10;
	const int SpecialValuesOffset = ExperienceOffset + 90 * 4;
	const int SpellSchoolsOffset = SpecialValuesOffset + 11 * 2;

	public Class Class { get; } = @class;

	public byte GetSkillMaxValue(Skill skill) => skill == Skill.None ? (byte)0 : data[(int)skill - 1];

	public uint GetExperienceForNextLevel(int level)
	{
		int offset = ExperienceOffset + level * 4;

		return (uint)(data[offset] << 24 | data[offset + 1] << 16 | data[offset + 2] << 8 | data[offset + 3]);
	}

	word GetSpecialValue(int index) => (word)(data[SpecialValuesOffset + index * 2] << 8 | data[SpecialValuesOffset + index * 2 + 1]);

	public word AttacksPerRoundLevel => GetSpecialValue(0);

	public word HitPointsPerLevel => GetSpecialValue(1);

	public word SpellPointsPerLevel => GetSpecialValue(2);

	public word SpellLearningPointsPerLevel => GetSpecialValue(3);

	public SpellSchoolFlags SpellSchools => (SpellSchoolFlags)data[SpellSchoolsOffset];
}

internal sealed class ClassDataLoader(Func<IReadOnlyList<byte[]>> classData) : IClassDataLoader
{
	readonly Dictionary<Class, IClassData> loadedClassData = [];

	public IClassData LoadClassData(Class @class)
	{
		if (!loadedClassData.TryGetValue(@class, out var data))
		{
			var allData = classData();

			if ((int)@class >= allData.Count)
				throw new AmberException(ExceptionScope.Data, $"No data for class {@class}.");

			data = new ClassData(@class, allData[(int)@class]);
			loadedClassData.Add(@class, data);
		}

		return data;
	}
}
