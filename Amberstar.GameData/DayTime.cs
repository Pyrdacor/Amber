namespace Amberstar.GameData;

public enum DayTime : byte
{
	Night,
	Dawn,
	Day,
	Dusk,
}

public static class DayTimeExtensions
{
	public static int StartingHour(this DayTime dayTime) => dayTime switch
	{
		DayTime.Night => 20,
		DayTime.Dawn => 6,
		DayTime.Day => 8,
		DayTime.Dusk => 18,
		_ => 0
	};

	public static int EndingHour(this DayTime dayTime) => dayTime switch
	{
		DayTime.Night => 6,
		DayTime.Dawn => 8,
		DayTime.Day => 18,
		DayTime.Dusk => 20,
		_ => 0
	};

	public static DayTime HourToDayTime(this int hour)
	{
		if (hour >= 20 || hour < 6)
		{
			return DayTime.Night;
		}
		else if (hour < 8)
		{
			return DayTime.Dawn;
		}
		else if (hour >= 18)
		{
			return DayTime.Dusk;
		}
		else
		{
			return DayTime.Day;
		}
	}
}
