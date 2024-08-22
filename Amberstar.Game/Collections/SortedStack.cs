namespace Amberstar.Game.Collections
{
	public class SortedStack<TKey, TValue> where TKey : IComparable<TKey>
		where TValue : notnull
	{
		private readonly SortedList<TKey, Queue<TValue>> _sortedList = [];

		public void Push(TKey key, TValue value)
		{
			if (!_sortedList.TryGetValue(key, out Queue<TValue>? queue))
			{
				queue = new Queue<TValue>();
				_sortedList[key] = queue;
			}

			queue.Enqueue(value);
		}

		public TValue? Pop(TKey maxKey)
		{
			foreach (var kvp in _sortedList)
			{
				if (kvp.Key.CompareTo(maxKey) <= 0)
				{
					var item = kvp.Value.Dequeue();

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
