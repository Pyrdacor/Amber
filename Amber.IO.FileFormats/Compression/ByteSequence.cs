using System.Collections;

namespace Amber.IO.FileFormats.Compression;

internal class ByteSequence(byte[] sequence, int offset, int length) : IEnumerable<byte>
{
	readonly byte[] sequence = sequence;
	readonly int offset = offset;
	readonly int length = length;

	public int Count => length;

	public byte this[int index]
	{
		get
		{
			if (index < 0 || index >= length)
				throw new ArgumentOutOfRangeException(nameof(index), "Index must be greater 0 and less than the sequence length.");

			return sequence[offset + index];
		}
	}

	public Tuple<ByteSequence, ByteSequence> Split(int length)
	{
		if (length >= this.length)
			throw new ArgumentOutOfRangeException(nameof(length), "Split length must be less than the sequence length.");

		return Tuple.Create<ByteSequence, ByteSequence>(new(sequence, offset, length), new(sequence, offset + length, this.length - length));
	}

	public ByteSequence SkipFirst(int count) => new(sequence, offset + count, length - count);

	public IEnumerator<byte> GetEnumerator()
	{
		for (int i = 0; i < length; i++)
			yield return sequence[offset + i];
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
