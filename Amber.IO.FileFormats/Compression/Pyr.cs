using Amber.Serialization;

namespace Amber.IO.FileFormats.Compression
{
	public enum DataType
	{
		GeneralBinaryData,
		Text,
		PlanarImageData,
		ChunkyImageData
	}

	public static class Pyr
	{
		static readonly Dictionary<DataType, StaticHuffmanType> mapping = new()
		{
			{ DataType.GeneralBinaryData, StaticHuffmanType.GeneralBinaryData },
			{ DataType.Text, StaticHuffmanType.Text },
			{ DataType.PlanarImageData, StaticHuffmanType.PlanarImageData },
			{ DataType.ChunkyImageData, StaticHuffmanType.ChunkyImageData }
		};

		const int MatchLiteral = 256;
		const int RLELiteral = 257;
		const int BlockCheckSize = 8096 * 8; // 1024 bytes
		public const int DefaultMaxMatchLength = 64;

		public static byte[] Compress(byte[] data, DataType dataType, int maxMatchLength = DefaultMaxMatchLength)
		{
			int blockCount = 0;
			int blockOffset = 0;
			long blockSize = 0;
			var compressedBlockData = new BitStreamWriter();
			var compressedData = new List<byte>();
			var trie = new MatchTrie();
			var compressionType = mapping[dataType];
			var table = HuffmanTables.GetTable(compressionType);
			var codes = new HuffmanTree(table!, MatchLiteral, RLELiteral).GenerateHuffmanCodes();

			// Make room for block count
			compressedData.Add(0);
			compressedData.Add(0);

			void WriteOffset(int offset)
			{
				offset -= 2;
				int bitsNeeded = (int)Math.Log2(offset) + 1;
				int shift = ((bitsNeeded - 1) / 7) * 7;
				int fullBytes = (bitsNeeded - 1) / 7;

				for (int i = 0; i < fullBytes; i++)
				{
					compressedBlockData.WriteBits(8, 0x80 | ((offset >> shift) & 0x7f));
					shift -= 7;
					blockSize += 8;
				}

				compressedBlockData.WriteBits(8, offset & 0x7f);
				blockSize += 8;
			}

			void WriteLength(int length)
			{
				length -= 2;
				int bitsNeeded = (int)Math.Log2(length) + 1;
				int shift = ((bitsNeeded - 1) / 3) * 3;
				int fullBytes = (bitsNeeded - 1) / 3;

				for (int i = 0; i < fullBytes; i++)
				{
					compressedBlockData.WriteBits(4, 0x8 | ((length >> shift) & 0x7));
					shift -= 3;
					blockSize += 4;
				}

				compressedBlockData.WriteBits(4, length & 0x7);
				blockSize += 4;
			}

			void WriteSymbol(int symbol)
			{
				var code = codes[symbol];
				compressedBlockData.WriteBits(code.Length, code.Value);
				blockSize += code.Length;
			}

			for (int i = 0; i < data.Length; i++)
			{
				if (i + 2 < data.Length)
				{
					byte literal = data[i];

					if (data[i + 1] == literal && data[i + 2] == literal)
					{
						// RLE
						int length = 2;
						for (int n = i + 3; n < data.Length; n++)
						{
							if (data[n] != literal)
								break;

							length++;
						}

						WriteSymbol(data[i]);
						WriteSymbol(RLELiteral);
						WriteLength(length);
						CheckBlock(i + 1);
						i += length;
						continue;
					}
				}

				var match = trie.GetLongestMatch(data, i, Math.Min(maxMatchLength, data.Length - i));

				if (match.Value < 4) // no match found
				{
					WriteSymbol(data[i]);
					trie.Add(data, i, Math.Min(maxMatchLength, data.Length - i));
					CheckBlock(i + 1);
				}
				else // match found
				{
					WriteSymbol(MatchLiteral);
					WriteOffset(i - match.Key);
					WriteLength(match.Value);

					for (int j = 0; j < match.Value; ++j)
						trie.Add(data, i + j, Math.Min(maxMatchLength, data.Length - i - j));

					i += match.Value - 1;
					CheckBlock(i + 1);
				}
			}

			void CheckBlock(int index)
			{
				if (blockSize == 0)
					return;

				int blockReadSize = index - blockOffset;
				// TODO: adjust 10. it should be 1 + max write size
				// max write size = max match offset + length encoding size.
				int maxReadSize = (1 << 24) - 10;

				if (blockSize < maxReadSize && blockSize < BlockCheckSize && index < data.Length - 1)
					return;

				if (blockReadSize <= (blockSize + 7) / 8 + 4)
				{
					// Not worth compressing. Store as raw block.
					compressedData.Add((byte)StaticHuffmanType.RawData);
					compressedData.Add((byte)((blockReadSize >> 16) & 0xff));
					compressedData.Add((byte)((blockReadSize >> 8) & 0xff));
					compressedData.Add((byte)(blockReadSize & 0xff));
					compressedData.AddRange(new DataReader(data, blockOffset, blockReadSize).ReadToEnd());
				}
				else
				{
					compressedData.Add((byte)compressionType);
					compressedData.Add((byte)((blockSize >> 16) & 0xff));
					compressedData.Add((byte)((blockSize >> 8) & 0xff));
					compressedData.Add((byte)(blockSize & 0xff));
					compressedData.AddRange(compressedBlockData.ToArray());
				}

				compressedBlockData = new BitStreamWriter();
				blockOffset = index;
				blockSize = 0;
				blockCount++;
			}

			CheckBlock(data.Length);

			// Store block count
			compressedData[0] = (byte)(blockCount >> 8);
			compressedData[1] = (byte)(blockCount & 0xff);

			return compressedData.ToArray();
		}
		
		public static byte[] Decompress(byte[] data)
		{
			var decompressedData = new List<byte>();
			var dataReader = new DataReader(data);

			int numBlocks = dataReader.ReadWord();

			for (int i = 0; i < numBlocks; i++)
			{
				decompressedData.AddRange(DecompressBlock(dataReader, decompressedData));
			}

			return [.. decompressedData];
		}

		private static byte[] DecompressBlock(DataReader dataReader, List<byte> previousData)
		{
			var type = (StaticHuffmanType)dataReader.ReadByte();
			int dataSize = dataReader.ReadByte();
			dataSize <<= 8;
			dataSize |= dataReader.ReadByte();
			dataSize <<= 8;
			dataSize |= dataReader.ReadByte();

			if (type == StaticHuffmanType.RawData)
				return dataReader.ReadBytes(dataSize);

			var table = HuffmanTables.GetTable(type);

			if (table == null)
				throw new NotSupportedException($"Pyr compression type {(int)type} is not supported by this decompressor.");

			var bitStream = new BitStreamReader(dataReader.ReadBytes((dataSize + 7) / 8));
			var tree = new HuffmanTree(table, MatchLiteral, RLELiteral);
			var decompressedData = new List<byte>();
			byte lastLiteral = 0;

			while (bitStream.BitPosition < dataSize)
			{
				int value = tree.FindValueByCode(bitStream);

				if (value >= 0 && value < 256)
				{
					lastLiteral = (byte)value;
					decompressedData.Add(lastLiteral);
				}
				else if (value == MatchLiteral)
				{
					int offset = decompressedData.Count - ReadOffset();
					int length = ReadLength();

					for (int i = 0; i < length; i++)
					{
						if (offset < 0)
							decompressedData.Add(previousData[previousData.Count + offset++]);
						else
							decompressedData.Add(decompressedData[offset++]);
					}
				}
				else if (value == RLELiteral)
				{
					int length = ReadLength();

					for (int i = 0; i < length; i++)
					{
						decompressedData.Add(lastLiteral);
					}
				}
				else if (value == -1)
				{
					// end of bit stream
					break;
				}
				else
				{
					throw new InvalidDataException("No valid Pyr compressed data.");
				}
			}

			return decompressedData.ToArray();

			int ReadOffset()
			{
				long length = 0;

				while (true)
				{
					long next = bitStream.ReadBits(8);

					length |= next & 0x7f;

					if (length > int.MaxValue - 2)
						throw new InvalidDataException("No valid Pyr compressed data.");

					if ((next & 0x80) == 0)
						break;

					length <<= 7;
				}

				return (int)length + 2;
			}

			int ReadLength()
			{
				long length = 0;

				while (true)
				{
					long next = bitStream.ReadBits(4);

					length |= next & 0x7;

					if (length > int.MaxValue - 2)
						throw new InvalidDataException("No valid Pyr compressed data.");

					if ((next & 0x8) == 0)
						break;

					length <<= 3;
				}

				return (int)length + 2;
			}
		}
	}
}
