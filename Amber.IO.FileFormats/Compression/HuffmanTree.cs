namespace Amber.IO.FileFormats.Compression
{
	internal struct HuffmanCode
	{
		public int Length;
		public int Value;

		public HuffmanCode AppendBit(int value)
		{
			return new()
			{
				Length = Length + 1,
				Value = (Value << 1) | (value & 0x1),
			};
		}
	}

	class HuffmanNode : IComparable<HuffmanNode>
	{
		public int Value { get; set; }
		public int Frequency { get; set; }
		public HuffmanNode? Left { get; set; }
		public HuffmanNode? Right { get; set; }

		public bool IsLeaf => Left == null && Right == null;

		public int CompareTo(HuffmanNode? other)
		{
			if (other is null)
				return -1;

			return this.Frequency.CompareTo(other.Frequency);
		}
	}

	internal class HuffmanTree
	{
		readonly HuffmanNode root;

		public HuffmanTree(byte[] table, params int[] additionalLiterals)
		{
			var nodes = new List<HuffmanNode>();

			for (int i = 0; i < table.Length; i++)
			{
				nodes.Add(new() { Value = (byte)i, Frequency = table[i] * (table[i] + 1) });
			}

			if (additionalLiterals is not null)
			{
				if (additionalLiterals.Distinct().Count() != additionalLiterals.Length)
					throw new InvalidOperationException("Additional literals must consist of unique values.");

				for (int i = 0; i < additionalLiterals.Length; i++)
				{
					if (additionalLiterals[i] < 256)
						throw new InvalidOperationException("Additional literals must be greater than 255.");
					nodes.Add(new() { Value = additionalLiterals[i], Frequency = additionalLiterals[i] * additionalLiterals[i] });
				}
			}

			var priorityQueue = new List<HuffmanNode>(nodes);
			priorityQueue.Sort();

			while (priorityQueue.Count > 1)
			{
				// Take the two nodes with the lowest frequency
				HuffmanNode left = priorityQueue[0];
				HuffmanNode right = priorityQueue[1];
				priorityQueue.RemoveAt(0);
				priorityQueue.RemoveAt(0);

				// Create a new parent node with these two nodes as children
				HuffmanNode parent = new()
				{
					Value = int.MaxValue, // Non-leaf nodes do not represent a byte
					Frequency = left.Frequency + right.Frequency,
					Left = left,
					Right = right
				};

				// Add the new node back to the priority queue
				priorityQueue.Add(parent);
				priorityQueue.Sort();
			}

			// The remaining node is the root of the Huffman tree
			root = priorityQueue[0];
		}

		public Dictionary<int, HuffmanCode> GenerateHuffmanCodes()
		{
			var huffmanCodes = new Dictionary<int, HuffmanCode>();
			GenerateHuffmanCodesRecursive(root, new(), huffmanCodes);
			return huffmanCodes;
		}

		public int FindValueByCode(BitStreamReader bitStream) => FindValueByCode(root, bitStream);

		private static int FindValueByCode(HuffmanNode node, BitStreamReader bitStream)
		{
			if (node.IsLeaf)
				return node.Value;

			if (bitStream.BitPosition == bitStream.BitLength)
				return -1;

			if (bitStream.ReadBit() == 0)
				return FindValueByCode(node.Left!, bitStream);

			return FindValueByCode(node.Right!, bitStream);
		}

		private static void GenerateHuffmanCodesRecursive(HuffmanNode node, HuffmanCode currentCode, Dictionary<int, HuffmanCode> huffmanCodes)
		{
			if (node == null) return;

			// If this is a leaf node, it represents a byte, so add it to the dictionary
			if (node.IsLeaf)
			{
				huffmanCodes[node.Value] = currentCode;
			}
			else
			{
				// Traverse the left and right children, appending '0' for left and '1' for right
				GenerateHuffmanCodesRecursive(node.Left!, currentCode.AppendBit(0), huffmanCodes);
				GenerateHuffmanCodesRecursive(node.Right!, currentCode.AppendBit(1), huffmanCodes);
			}
		}
	}
}
