namespace Amberstar.Game.Collections
{
	public class SortedStack<TKey, TValue> where TKey : IComparable<TKey>
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

		public List<TValue> Pop(TKey maxKey)
		{
			List<TValue> itemsToPop = [];
			List<TKey> keysToRemove = [];

			foreach (var kvp in _sortedList)
			{
				if (kvp.Key.CompareTo(maxKey) <= 0)
				{
					while (kvp.Value.Count > 0)
					{
						itemsToPop.Add(kvp.Value.Dequeue());
					}

					keysToRemove.Add(kvp.Key);
				}
			}

			foreach (var key in keysToRemove)
			{
				_sortedList.Remove(key);
			}

			return itemsToPop;
		}
	}
}
