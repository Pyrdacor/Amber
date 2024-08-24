using Amber.Serialization;

namespace Amber.IO.FileFormats.Compression;

public static class Pyr
{
	const byte Header0 = 0x00;
	const byte Header1 = 0x80;

	enum DictionaryEntrySize
	{
		S8 = 1 << 8,
		S12 = 1 << 12,
		S16 = 1 << 16,
	}

	private static int Read8Bits(byte[] data, ref int offset) => data[offset++];

	private static int Read12Bits(byte[] data, ref int offset, ref int? savedByte)
	{
		if (savedByte == null)
		{
			var b0 = data[offset++];
			var b1 = data[offset++];

			savedByte = (b0 & 0xf0) << 8;

			return ((b0 << 8) | b1) & 0xfff;
		}

		int result = data[offset++] | savedByte.Value;

		savedByte = null;

		return result;
	}

	private static int Read16Bits(byte[] data, ref int offset)
	{
		return (data[offset++] << 8) | data[offset++];
	}

	private static int ReadBits(byte[] data, ref int offset, ref int? savedByte, DictionaryEntrySize size)
	{
		switch (size)
		{
			case DictionaryEntrySize.S8:
				return Read8Bits(data, ref offset);
			case DictionaryEntrySize.S12:
				return Read12Bits(data, ref offset, ref savedByte);
			default:
				return Read16Bits(data, ref offset);
		}
	}

	private static void WriteBits(DataWriter writer, int value, ref int? saveByteIndex, DictionaryEntrySize size)
	{
		switch (size)
		{
			case DictionaryEntrySize.S8:
				writer.Write((byte)value);
				break;
			case DictionaryEntrySize.S12:
				if (saveByteIndex == null)
				{
					saveByteIndex = writer.Position;
					writer.Write((ushort)value);
				}
				else
				{
					writer.OrByte(saveByteIndex.Value, (byte)((value >> 4) & 0xf0));
					saveByteIndex = null;
					writer.Write((byte)(value & 0xff));
				}
				break;
			default:
				writer.Write((ushort)value);
				break;
		}
	}

	public static byte[] Compress(byte[] data)
	{
		if (data.Length <= 8)
			return [(byte)data.Length, ..data];

		var dict = new TokenDictionary(data);
		var dictSize = DictionaryEntrySize.S8;

		List<byte> literals = [];
		List<int> dictTokens = [];
		List<bool> tokenFlags = [];
		List<int> dictSizeIncreaseIndices = new(2);
		int offset = 0;
		int lastLength = 1;

		// Always insert first two literals as is and add a dictionary entry.
		literals.Add(data[offset++]);
		literals.Add(data[offset++]);
		tokenFlags.Add(false);
		tokenFlags.Add(false);
		dict.Insert(0, 2);

		void InsertToken(int offset, int length)
		{
			if (dict.NodeCount >= (int)DictionaryEntrySize.S16)
				dict.FreeIndex();
			dict.Insert(offset, length);

			if (dictSize == DictionaryEntrySize.S8 && dict.NodeCount > (int)DictionaryEntrySize.S8)
			{
				dictSize = DictionaryEntrySize.S12;
				dictSizeIncreaseIndices.Add(tokenFlags.Count);
			}
			else if (dictSize == DictionaryEntrySize.S12 && dict.NodeCount > (int)DictionaryEntrySize.S12)
			{
				dictSize = DictionaryEntrySize.S16;
				dictSizeIncreaseIndices.Add(tokenFlags.Count);
			}
		}

		while (offset < data.Length)
		{
			int tokenIndex = dict.Find(offset, out int tokenLength);

			if (tokenIndex == -1)
			{
				literals.Add(data[offset++]);
				tokenFlags.Add(false);

				if (offset == data.Length)
					break;

				InsertToken(offset - lastLength, lastLength + 1);
				lastLength = 1;
			}
			else
			{
				dictTokens.Add(tokenIndex);
				tokenFlags.Add(true);

				if (offset + tokenLength == data.Length)
					break;

				for (int i = 1; i <= lastLength; i++)
					InsertToken(offset - i, i + tokenLength);

				offset += tokenLength;
				lastLength = tokenLength;
			}
		}

		int processingIndex = 0;
		int literalIndex = 0;
		int dictTokenIndex = 0;
		List<byte> headerData = [];
		var dataWriter = new DataWriter();
		dictSize = DictionaryEntrySize.S8;
		int? saveByteIndex = null;

		void CheckSizeIncrease()
		{
			if (dictSizeIncreaseIndices.Contains(processingIndex))
			{
				if (dictSize == DictionaryEntrySize.S8)
					dictSize = DictionaryEntrySize.S12;
				else
					dictSize = DictionaryEntrySize.S16;
			}
		}

		while (processingIndex < tokenFlags.Count)
		{
			byte header = Header1;
			int numLiterals = 0;
			int numDictTokens = 0;

			// Check if we use Header0 format
			if (tokenFlags.Count - processingIndex >= 8)
			{
				for (int i = 0; i < tokenFlags.Count - processingIndex; i++)
				{
					if (!tokenFlags[processingIndex + i])
					{
						if (numDictTokens != 0)
							break;

						numLiterals++;
					}
					else
					{
						numDictTokens++;
					}

					if (numLiterals + numDictTokens > 7)
					{
						header = Header1;
						break;
					}
				}
			}

			if (header == Header1)
			{
				for (int i = 0; i < Math.Min(7, tokenFlags.Count - processingIndex); i++)
				{
					if (tokenFlags[processingIndex++])
					{
						header |= (byte)(1 << i);
						WriteBits(dataWriter, dictTokens[dictTokenIndex++], ref saveByteIndex, dictSize);
					}
					else
					{
						dataWriter.Write(literals[literalIndex++]);
					}

					CheckSizeIncrease();
				}

				headerData.Add(header);
			}
			else
			{
				int literalCount = 0;

				while (!tokenFlags[processingIndex++]) // literals
				{
					literalCount++;
					dataWriter.Write(literals[literalIndex++]);
					CheckSizeIncrease();
				}

				int dictTokenCount = 0;

				while (tokenFlags[processingIndex++]) // dict tokens
				{
					dictTokenCount++;
					WriteBits(dataWriter, dictTokens[dictTokenIndex++], ref saveByteIndex, dictSize);
					CheckSizeIncrease();
				}

				if (literalCount < 15)
				{
					header |= (byte)literalCount;
				}
				else
				{
					header |= 0xf;
				}

				if (dictTokenCount < 7)
				{
					header |= (byte)(literalCount << 4);
				}
				else
				{
					header |= 0x70;
				}

				headerData.Add(header);

				if (literalCount >= 15)
				{
					literalCount -= 15;

					while (true)
					{
						int count = Math.Min(255, literalCount);
						headerData.Add((byte)count);

						if (count < 255)
							break;

						literalCount -= 255;
					}
				}

				if (dictTokenCount >= 6)
				{
					dictTokenCount -= 6;

					while (true)
					{
						int count = Math.Min(255, dictTokenCount);
						headerData.Add((byte)count);

						if (count < 255)
							break;

						dictTokenCount -= 255;
					}
				}
			}
		}

		return [..headerData, ..dataWriter.ToArray()];
	}

	public static DataReader Decompress(IDataReader reader, uint decodedSize)
	{
		var decodedData = new byte[decodedSize];
		uint decodeIndex = 0;
		uint matchOffset;
		uint matchLength;
		uint matchIndex;

		while (decodeIndex < decodedSize)
		{
			byte header = reader.ReadByte();

			for (int i = 0; i < 8; ++i)
			{
				if ((header & 0x80) == 0) // match
				{
					matchOffset = reader.ReadByte();
					matchLength = (matchOffset & 0x000f) + 3;
					matchOffset <<= 4;
					matchOffset &= 0xff00;
					matchOffset |= reader.ReadByte();
					matchIndex = decodeIndex - matchOffset;

					while (matchLength-- != 0)
					{
						decodedData[decodeIndex++] = decodedData[matchIndex++];
					}
				}
				else // normal byte
				{
					decodedData[decodeIndex++] = reader.ReadByte();
				}

				if (decodeIndex == decodedSize)
					break;

				header <<= 1;
			}
		}

		return new DataReader(decodedData);
	}
}
