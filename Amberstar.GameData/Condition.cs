using Amberstar.GameData.Serialization;

namespace Amberstar.GameData;

[Flags]
public enum PhysicalCondition : byte
{
	None = 0,
	Stunned = 0x01,
	Poisoned = 0x02,
	Petrified = 0x04,
	Diseased = 0x08,
	Aging = 0x10,
	Dead = 0x20,
	Ashes = 0x40,
	Dust = 0x80,
}

[Flags]
public enum MentalCondition : byte
{
    MentalCondition = 0,
    Irritated = 0x01,
	Mad = 0x02,
	Sleeping = 0x04,
	Panicked = 0x08,
	Blind = 0x10,
	Overloaded = 0x20,
}

[Flags]
public enum Condition : word
{
	None = 0,
	Irritated = 0x0001,
	Mad = 0x0002,
	Sleeping = 0x0004,
	Panicked = 0x0008,
	Blind = 0x0010,
	Overloaded = 0x0020,
	Stunned = 0x0100,
	Poisoned = 0x0200,
	Petrified = 0x0400,
	Diseased = 0x0800,
	Aging = 0x1000,
	Dead = 0x2000,
	Ashes = 0x4000,
	Dust = 0x8000,
}

public static class ConditionExtensions
{
	public static PhysicalCondition ToPhysical(this Condition condition) => (PhysicalCondition)((int)condition >> 8);
	public static MentalCondition ToMental(this Condition condition) => (MentalCondition)((int)condition);
	public static Condition ToCondition(this PhysicalCondition condition) => (Condition)((int)condition << 8);
	public static Condition ToCondition(this MentalCondition condition) => (Condition)((int)condition);

	public static StatusIcon ToStatusIcon(this PhysicalCondition condition) => condition switch
    {
        PhysicalCondition.Stunned => StatusIcon.Stunned,
        PhysicalCondition.Poisoned => StatusIcon.Poisoned,
        PhysicalCondition.Petrified => StatusIcon.Petrified,
        PhysicalCondition.Diseased => StatusIcon.Diseased,
        PhysicalCondition.Aging => StatusIcon.Aging,
        PhysicalCondition.Dead => StatusIcon.Dead,
        PhysicalCondition.Ashes => StatusIcon.Dead,
        PhysicalCondition.Dust => StatusIcon.Dead,
        _ => throw new ArgumentOutOfRangeException(nameof(condition)),
    };

    public static StatusIcon ToStatusIcon(this MentalCondition condition) => condition switch
    {
        MentalCondition.Irritated => StatusIcon.Irritated,
        MentalCondition.Mad => StatusIcon.Mad,
        MentalCondition.Sleeping => StatusIcon.Sleeping,
        MentalCondition.Panicked => StatusIcon.Panicked,
        MentalCondition.Blind => StatusIcon.Blind,
        MentalCondition.Overloaded => StatusIcon.Overloaded,
        _ => throw new ArgumentOutOfRangeException(nameof(condition)),
    };
}
