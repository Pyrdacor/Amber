using Amber.Common;
using Amberstar.Game.Collections;
using Amberstar.GameData;

namespace Amberstar.Game;

partial class Game
{
	readonly SortedStack<long, TimedAction> timedActions = new();
	long lastTimedActionKey = -1;
	static readonly Random random = new();

	internal static int Random(int min, int max) => random.Next(min, max);
	internal static int Random(int max) => Random(0, max);
	internal static bool Random() => Random(0, 1) == 1;

	internal bool Probe(int chance) => State.TravelType == TravelType.SuperChicken || chance == 100 || chance >= Random(1, 100);

    internal void ExecuteNextUpdateCycle(Action action)
    {
		AddDelayedAction(0, action);
    }

    internal long AddDelayedAction(long delayInTicks, Action action)
	{
		timedActions.Push(gameTicks + delayInTicks, new(++lastTimedActionKey, action));
		return lastTimedActionKey;
	}

	internal long AddDelayedAction(TimeSpan delay, Action action)
	{
		return AddDelayedAction(MathUtil.Round(delay.TotalSeconds * TicksPerSecond), action);
	}

    internal long AddDelayedActionAt(DateTime timestamp, Action action)
    {
		var delay = timestamp - DateTime.Now;

		if (delay.TotalMilliseconds < 0)
			delay = TimeSpan.FromMilliseconds(0);

		return AddDelayedAction(delay, action);
    }

    internal int DeleteDelayedActions(params long[] keys)
	{
		var lookup = new HashSet<long>(keys);
		return timedActions.Remove(timedAction => lookup.Contains(timedAction.Key)).Length;
	}

    internal int DeleteDelayedActions(Func<long, bool> filter)
    {
        return timedActions.Remove(timedAction => filter(timedAction.Key)).Length;
    }

    internal int ExecuteAndDeleteDelayedActions(long key)
    {
		var removedActions = timedActions.Remove(timedAction => timedAction.Key == key);
        
		foreach (var removedAction in removedActions)
		{
			removedAction.Action();
        }

		return removedActions.Length;
    }

    internal void ClearDelayedActions(params long[] expectKeys)
	{
		if (expectKeys == null || expectKeys.Length == 0)
			timedActions.Clear();
		else
		{
			var lookup = new HashSet<long>(expectKeys.Where(key => key != -1));

			if (lookup.Count == 0)
				timedActions.Clear();
			else
				DeleteDelayedActions(key => !lookup.Contains(key));
        }
	}

    IText GetMapText(int mapIndex, int index)
    {
        var text = AssetProvider.TextLoader.LoadText(new(AssetType.MapText, mapIndex));
        return text.GetTextBlock(index);
    }

	IText GetCurrentMapText(int index) => GetMapText(State.GetIndexOfMapWithPlayer(), index);
}
