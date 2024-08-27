using Amber.Common;
using Amberstar.Game.Collections;

namespace Amberstar.Game
{
	partial class Game
	{
		readonly SortedStack<long, TimedAction> timedActions = new();
		long lastTimedActionKey = -1;
		static readonly Random random = new();

		internal static int Random(int min, int max) => random.Next(min, max);
		internal static int Random(int max) => Random(0, max);
		internal static bool Random() => Random(0, 1) == 1;

		internal long AddDelayedAction(long delayInTicks, Action action)
		{
			timedActions.Push(gameTicks + delayInTicks, new(++lastTimedActionKey, action));
			return lastTimedActionKey;
		}

		internal long AddDelayedAction(TimeSpan delay, Action action)
		{
			return AddDelayedAction(MathUtil.Round(delay.TotalSeconds * TicksPerSecond), action);
		}

		internal void DeleteDelayedActions(params long[] keys)
		{
			var lookup = new HashSet<long>(keys);
			timedActions.Remove(timedAction => lookup.Contains(timedAction.Key));
		}

		internal void ClearDelayedActions()
		{
			timedActions.Clear();
		}
	}
}
