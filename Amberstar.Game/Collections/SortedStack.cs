using System.Collections.Generic;

namespace Amberstar.Game.Collections
{
	public class SortedStack<TKey, TValue> where TKey : IComparable<TKey>
		where TValue : notnull
	{
		private readonly SortedList<TKey, List<TValue>> _sortedList = [];

		public void Clear()
		{
			_sortedList.Clear();
		}

		public void Remove(Func<TValue, bool> filter)
		{
			foreach (var queue in new SortedList<TKey, List<TValue>>(_sortedList))
			{
				foreach (var entry in new List<TValue>(queue.Value))
				{
					if (filter(entry))
						queue.Value.Remove(entry);
				}

				if (queue.Value.Count == 0)
					_sortedList.Remove(queue.Key);
			}
		}

		public void Push(TKey key, TValue value)
		{
			if (!_sortedList.TryGetValue(key, out List<TValue>? queue))
			{
				queue = new List<TValue>();
				_sortedList[key] = queue;
			}

			queue.Add(value);
		}

		public TValue? Pop(TKey maxKey)
		{
			foreach (var kvp in _sortedList)
			{
				if (kvp.Key.CompareTo(maxKey) <= 0)
				{
					var item = kvp.Value[0];
					kvp.Value.RemoveAt(0);

					if (kvp.Value.Count == 0)
						_sortedList.Remove(kvp.Key);

					return item;
				}
				else
				{
					break;
				}
			}

			return default;
		}
	}
}
