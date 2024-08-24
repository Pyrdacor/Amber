using Amberstar.GameData;

namespace Amberstar.Game;

// Note: Maps could specify the length of minutes, hours, days, etc but
// this is not really done in Amberstar so we don't bother.
internal class Time(Game game)
{
	const int TicksPerTimeChange = 500;
	long lastTimeChangeTicks = 0;
	long totalTicks = 0;
	long moveTicks = 0;

	/// <summary>
	/// Progress active spells
	/// </summary>
	public event Action? MinuteChanged;
	/// <summary>
	/// Apply poison
	/// </summary>
	public event Action? HourChanged;
	/// <summary>
	/// Apply disease effects (dicrease random attribute by 1, but at least keep it at 1)
	/// </summary>
	public event Action? DayChanged;
	public event Action? MonthChanged;
	/// <summary>
	/// Age players (kill at max age)
	/// </summary>
	public event Action? YearChanged;

	public void Update(long elapsed)
	{
		totalTicks += elapsed;

		long elapsedTimeTicks = totalTicks - lastTimeChangeTicks;

		while (elapsedTimeTicks >= TicksPerTimeChange)
		{
			elapsedTimeTicks -= TicksPerTimeChange;
			Tick();
		}
	}

	public void Moved2D()
	{
		if (++moveTicks >= game.State.TravelType.MovesPerTimeProgress())
			Tick();
	}

	public void Moved3D()
	{
		// TODO
	}

	public void Tick()
	{
		bool hourChanged = false;
		bool dayChanged = false;
		bool monthChanged = false;
		bool yearChanged = false;

		moveTicks = 0;
		game.State.Minute += 5;

		if (game.State.Minute == 60)
		{
			game.State.Minute = 0;
			game.State.Hour++;
			hourChanged = true;

			if (game.State.Hour == 24)
			{
				game.State.Hour = 0;
				game.State.Day++;
				game.State.TravelledDays++;
				dayChanged = true;

				if (game.State.Day == 31)
				{
					game.State.Day = 1;
					game.State.Month++;
					monthChanged = true;

					if (game.State.Month == 13)
					{
						game.State.Month = 1;
						game.State.Year++;
						game.State.RelativeYear++;
						yearChanged = true;
					}
				}
			}
		}

		lastTimeChangeTicks = totalTicks;

		InvokeChangeEvent(MinuteChanged);
		if (hourChanged)
			InvokeChangeEvent(HourChanged);
		if (dayChanged)
			InvokeChangeEvent(DayChanged);
		if (monthChanged)
			InvokeChangeEvent(MonthChanged);
		if (yearChanged)
			InvokeChangeEvent(YearChanged);
	}

	private void InvokeChangeEvent(Action? action)
	{
		// Note: Every party member could die and there can also be UI interactions like picking a new leader.
		// So all event handlers of the XChanged events should not block but use timed events.
		// Thus we call them this way here.
		if (action != null)
			game.AddDelayedAction(0, action);
	}
}
