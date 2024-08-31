namespace Amber.IO.FileFormats.Compression
{
	internal class BitStreamReader(byte[] data)
	{
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

		public int BitLength => data.Length << 3;

		public void AlignToByte()
		{
			if (currentBitNumber != 0)
			{
				++currentByteNumber;
				currentBitNumber = 0;
			}
		}

		public byte ReadByte()
		{
			AlignToByte();
			return data[currentByteNumber++];
		}

		public byte PeekByte()
		{
			return data[currentBitNumber == 0 ? currentByteNumber : currentByteNumber + 1];
		}

		public int ReadBit()
		{
			int value = data[currentByteNumber];
			value >>= (7 - currentBitNumber);
			++BitPosition;
			return value & 0x1;
		}

		public int ReadBits(int count)
		{
			int value = PeekBits(count);
			BitPosition += count;
			return value;
		}

		public int PeekBits(int count)
		{
			if (count == 0)
				return 0;

			if (count < 0 || count > 31)
				throw new ArgumentOutOfRangeException(nameof(count));

			long value = 0;

			if (currentBitNumber == 0)
			{
				int numFullBytes = count >> 3;

				for (int i = 0; i < numFullBytes; i++)
				{
					value |= data[currentByteNumber + i];
					value <<= 8;
				}

				int shift = count & 0x7;

				if (shift != 0)
					value |= (long)data[currentByteNumber + numFullBytes] >> (8 - shift);
				else
					value >>= 8;

				return (int)value;
			}
			else
			{
				if (currentBitNumber + count <= 8)
				{
					long mask = ((1 << (8 - currentBitNumber)) - 1);
					value = data[currentByteNumber] & mask;

					int shift = currentBitNumber + count;

					if (shift != 0)
						value >>= (8 - shift);

					return (int)value;
				}
				else
				{
					int remainingBitsInByte = 8 - currentBitNumber;
					value = data[currentByteNumber] & ((1 << remainingBitsInByte) - 1);
					value <<= (count - remainingBitsInByte);

					(var tempByte, var tempBit) = (currentByteNumber, currentBitNumber);
					++currentByteNumber;
					currentBitNumber = 0;
					value |= (long)PeekBits(count - remainingBitsInByte);
					(currentByteNumber, currentBitNumber) = (tempByte, tempBit);
					return (int)value;
				}
			}
		}
	}
}
