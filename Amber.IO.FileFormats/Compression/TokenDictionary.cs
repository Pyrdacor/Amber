namespace Amber.IO.FileFormats.Compression;

internal class TokenDictionary(byte[] data)
{
	const int MaxNodeCount = ushort.MaxValue + 1;
	private static long currentPriority = 0;
	private readonly byte[] data = data;
	private Dictionary<byte, TrieNode> rootNodes = [];
	private Dictionary<long, TrieNode> priorityQueue = [];
	int freeIndex = 0;
	int nodeCount = 0;

	public int NodeCount
	{
		get => nodeCount;
		private set => nodeCount = Math.Min(value, MaxNodeCount);
	}

	class TrieNode
	{
		public byte Key { get; private set; }
		public Dictionary<byte, TrieNode> Children { get; } = [];
		public ByteSequence Sequence { get; private set; }
		public bool IsEndOfSequence { get; }
		// null for nodes which are not end of sequences
		public int? Index { get; private set; }
		public long Priority { get; private set; }
		public TrieNode? Parent { get; private set; }
		public bool IsLeaf => Children.Count == 0;

		public TrieNode(int? index, byte key, ByteSequence sequence, TrieNode? parent, bool endOfSequence, TokenDictionary tokenDictionary)
		{
			if (index != null && index >= MaxNodeCount)
				index = tokenDictionary.freeIndex;

			Key = key;
			Sequence = sequence;
			Index = index;
			Priority = currentPriority++;
			Parent = parent;
			IsEndOfSequence = endOfSequence;

			if (index != null && endOfSequence)
				tokenDictionary.priorityQueue[Priority] = this;
		}

		private void UpdatePriority(TokenDictionary tokenDictionary)
		{
			tokenDictionary.priorityQueue.Remove(Priority);
			Priority = currentPriority++;
			tokenDictionary.priorityQueue[Priority] = this;
		}

		public TrieNode Insert(byte key, ByteSequence sequence, ref int nodeCount, TokenDictionary tokenDictionary)
		{
			if (sequence.Count == 0)
			{
				UpdatePriority(tokenDictionary);
				Index ??= nodeCount++;
				return this;
			}

			int matchLength = 0;
			int checkLength = Math.Min(sequence.Count, Sequence.Count);

			for (int i = 0; i < checkLength; i++)
			{
				if (sequence[i] != Sequence[i])
					break;

				matchLength++;
			}

			if (matchLength == Sequence.Count)
			{
				UpdatePriority(tokenDictionary);

				// Full match
				if (sequence.Count == Sequence.Count)
				{
					Index ??= nodeCount++;
					return this;
				}
			
				// Find or append child.
				byte nextLiteral = sequence[matchLength];

				if (Children.TryGetValue(nextLiteral, out TrieNode? childNode))
				{
					return childNode.Insert(sequence[matchLength], sequence.SkipFirst(matchLength + 1), ref nodeCount, tokenDictionary);
				}

				childNode = new TrieNode(nodeCount++, sequence[matchLength], sequence.SkipFirst(matchLength + 1), this, true, tokenDictionary);
				Children[nextLiteral] = childNode;
				return childNode;
			}
			else
			{
				// Partial match, split node.
				if (nodeCount == MaxNodeCount) // Ensure we have room to create a new indexed node
					tokenDictionary.FreeIndex();

				bool sequenceContained = matchLength == sequence.Count;
				(var head, var tail) = sequenceContained ? Sequence.Split(matchLength) : sequence.Split(matchLength);
				var newParent = new TrieNode(sequenceContained ? nodeCount++ : null, Key, head, Parent, sequenceContained, tokenDictionary);
				var newSibling = sequenceContained ? null : new TrieNode(nodeCount++, tail[0], tail.SkipFirst(1), newParent, true, tokenDictionary);

				if (Parent != null)
					Parent.Children[Key] = newParent;
				else
					tokenDictionary.rootNodes[Key] = newParent;

				Parent = newParent;
				Key = Sequence[matchLength];
				Sequence = Sequence.SkipFirst(matchLength + 1);

				newParent.Children.Add(Key, this);

				if (newSibling != null)
				{
					newParent.Children.Add(newSibling.Key, newSibling);
					return newSibling;
				}

				return newParent;
			}
		}

		public int Find(byte[] data, int offset, TokenDictionary tokenDictionary, ref int length)
		{
			if (Sequence.Count > data.Length - offset)
				return -1;

			for (int i = 0; i < Sequence.Count; i++)
			{
				if (data[offset + i] != Sequence[i])
					return -1;
			}

			if (Sequence.Count > 0 && data.Length - offset == Sequence.Count)
			{
				length += Sequence.Count;
				UpdatePriority(tokenDictionary);
				return Index!.Value;
			}

			offset += Sequence.Count;

			if (data.Length <= offset + 1)
				return -1;

			byte key = data[offset];
			var result = Children.GetValueOrDefault(key)?.Find(data, offset + 1, tokenDictionary, ref length) ?? -1;

			if (result != -1)
			{
				length += Sequence.Count;
				UpdatePriority(tokenDictionary);
				length++;
				return result;
			}

			if (Index != null)
			{
				length += Sequence.Count;
				UpdatePriority(tokenDictionary);
				return Index.Value;
			}

			return -1;
		}
	}

	public int Insert(int offset, int length)
	{
		int remainingLength = data.Length - offset;

		if (remainingLength == 0)
			throw new EndOfStreamException("End of data reached.");

		length = Math.Min(length, remainingLength);

		byte literal = data[offset];

		if (!rootNodes.TryGetValue(literal, out TrieNode? node))
		{
			node = new TrieNode(NodeCount++, literal, new(data, offset + 1, length - 1), null, true, this);
			rootNodes[literal] = node;
			return node.Index!.Value;
		}

		int nodeCount = NodeCount;
		node = node.Insert(literal, new(data, offset + 1, length - 1), ref nodeCount, this);
		NodeCount = nodeCount;

		return node.Index!.Value!;
	}

	public int Find(int offset, out int length)
	{
		byte key = data[offset];
		length = 0;

		int index = rootNodes.GetValueOrDefault(key)?.Find(data, offset + 1, this, ref length) ?? -1;

		if (index != -1)
			length++;

		return index;
	}

	private void DeleteNode(TrieNode node)
	{
		if (node.Parent != null)
			node.Parent.Children.Remove(node.Key);
		else
			rootNodes.Remove(node.Key);
	}

	private TrieNode? FindLowestPriorityLeafNode(TrieNode parent)
	{
		PriorityQueue<TrieNode, long> list = new();

		foreach (var child in parent.Children)
		{
			if (child.Value.IsLeaf)
			{
				list.Enqueue(child.Value, child.Value.Priority);
				continue;
			}

			var lowestPrioChild = FindLowestPriorityLeafNode(child.Value);

			if (lowestPrioChild != null)
				list.Enqueue(lowestPrioChild, lowestPrioChild.Priority);
		}

		return list.Count == 0 ? null : list.Dequeue();
	}

	public int FreeIndex()
	{
		if (priorityQueue.Count == 0)
			return -1;

		var minPrio = priorityQueue.Keys.Min();
		var node = priorityQueue[minPrio];

		if (node.IsLeaf)
		{
			priorityQueue.Remove(minPrio);
			DeleteNode(node);
			return freeIndex = node.Index!.Value;
		}

		var lowestPrioChild = FindLowestPriorityLeafNode(node);

		if (lowestPrioChild == null)
			return -1;

		DeleteNode(node);
		priorityQueue.Remove(lowestPrioChild.Priority);

		return freeIndex = lowestPrioChild.Index!.Value;
	}


}
