namespace Amber.IO.FileFormats.Compression
{
	internal class BitStreamWriter()
	{
		readonly List<byte> data = [];
		int currentByteNumber;
		int currentBitNumber;

		public int BitPosition
		{
			get => (currentByteNumber << 3) | currentBitNumber;
			set
			{
				currentByteNumber = value >> 3;
				currentBitNumber = value & 0x7;
			}
		}

		public int BitLength => data.Count << 3;

		public void AlignToByte()
		{
			if (currentBitNumber != 0)
			{
				++currentByteNumber;
				currentBitNumber = 0;
			}
		}

		public void WriteByte(byte value)
		{
			AlignToByte();
			data.Add(value);
			++currentByteNumber;
		}

		public void WriteBit(int bit)
		{
			bit &= 0x1;

			if (currentBitNumber == 0)
			{
				data.Add(0x80);
			}
			else
			{
				bit <<= (7 - currentBitNumber);
				data[currentByteNumber] |= (byte)bit;
			}

			++BitPosition;
		}

		public void WriteBits(int count, int bits)
		{
			if (count == 0)
				return;

			if (count < 0 || count > 31)
				throw new ArgumentOutOfRangeException(nameof(count));

			bits &= (1 << count) - 1;

			if (currentBitNumber == 0)
			{
				int numFullBytes = count >> 3;
				int shift = count - 8;

				for (int i = 0; i < numFullBytes; i++)
				{
					data.Add((byte)((bits >> shift) & 0xff));
					shift -= 8;
					++currentByteNumber;
				}

				shift = count & 0x7;

				if (shift != 0)
				{
					currentBitNumber += shift;
					bits <<= (8 - shift);
					data.Add((byte)(bits & 0xff));
				}
			}
			else
			{
				if (currentBitNumber + count <= 8)
				{
					bits <<= (8 - currentBitNumber - count);
					data[currentByteNumber] |= (byte)bits;
					currentBitNumber += count;

					if (currentBitNumber == 8)
					{
						currentBitNumber = 0;
						currentByteNumber++;
					}
				}
				else
				{
					int remainingBitsInByte = 8 - currentBitNumber;
					data[currentByteNumber++] |= (byte)((bits >> (count - remainingBitsInByte)) & ((1 << remainingBitsInByte) - 1));
					currentBitNumber = 0;
					count -= remainingBitsInByte;
					WriteBits(count, bits & ((1 << count) - 1));
				}
			}
		}

		public byte[] ToArray() => [.. data];
	}
}
