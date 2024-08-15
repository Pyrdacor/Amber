namespace Amberstar.GameData;

public enum SpellSchool
{
	None,
	White,
	Grey,
	Black,
	Unused4,
	Unused5,
	Unused6,
	Special,
}

public static class SpellSchoolExtensions
{
	public static bool CanBeLearned(this SpellSchool school) => school switch
	{
		SpellSchool.White => true,
		SpellSchool.Grey => true,
		SpellSchool.Black => true,
		_ => false
	};

	public static bool IsUsed(this SpellSchool school) => school switch
	{
		SpellSchool.None => true,
		SpellSchool.White => true,
		SpellSchool.Grey => true,
		SpellSchool.Black => true,
		SpellSchool.Special => true,
		_ => false
	};
}