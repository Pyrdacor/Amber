namespace Amberstar.GameData;

[Flags]
public enum PhysicalCondition : byte
{
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
}
