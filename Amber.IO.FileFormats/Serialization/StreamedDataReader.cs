using System.Runtime.CompilerServices;
using Amber.IO.Common.Serialization;

namespace Amber.IO.FileFormats.Serialization;

public sealed class StreamedDataReader : IDataReader, IDisposable
{
    public static readonly Encoding Encoding = DataReader.Encoding;
    private readonly Stream stream;
    private readonly long start;
    private readonly long end;
    private readonly int length;

    public int Position
    {
        get => (int)(stream.Position - start);
        set
        {
            if (value < 0 || value > Size)
                throw new IndexOutOfRangeException("Data index out of range.");

            stream.Position = start + value;
        }
    }
    public int Size => length;

    public StreamedDataReader(Stream stream, long offset, int length)
    {
        this.stream = stream;
        this.start = offset;
        this.length = Math.Clamp(length, 0, (int)(stream.Length - offset));
        this.end = start + length;

        stream.Position = start;
    }

    public StreamedDataReader(Stream stream)
        : this(stream, stream.Position, (int)(stream.Length - stream.Position))
    {

    }

    public StreamedDataReader(StreamedDataReader streamedReader, long offset, int length)
        : this(streamedReader.stream, streamedReader.start + offset, Math.Clamp(length, 0, (int)((streamedReader.start + streamedReader.length) - offset)))
    {

    }

    public bool ReadBool()
    {
        CheckOutOfRange(1);
        return stream.ReadByte() != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte()
    {
        CheckOutOfRange(1);
        return (byte)stream.ReadByte();
    }

    public word ReadWord()
    {
        CheckOutOfRange(2);
        return (word)((ReadByte() << 8) | ReadByte());
    }

    public dword ReadDword()
    {
        CheckOutOfRange(4);
        return (((dword)ReadByte() << 24) | ((dword)ReadByte() << 16) | ((dword)ReadByte() << 8) | ReadByte());
    }

    public qword ReadQword()
    {
        CheckOutOfRange(8);
        return (((qword)ReadByte() << 56) | ((qword)ReadByte() << 48) | ((qword)ReadByte() << 40) |
                ((qword)ReadByte() << 32) | ((qword)ReadByte() << 24) | ((qword)ReadByte() << 16) |
                ((qword)ReadByte() << 8)  | ReadByte());
    }

    public string ReadChar()
    {
        return ReadString(1);
    }

    public string ReadString()
    {
        return ReadString(Encoding);
    }

    public string ReadString(Encoding encoding)
    {
        CheckOutOfRange(1);
        int length = ReadByte();
        return ReadString(length, encoding);
    }

    public string ReadString(int length)
    {
        return ReadString(length, Encoding);
    }

    public string ReadString(int length, Encoding encoding)
    {
        if (length == 0)
            return string.Empty;

        CheckOutOfRange(length);
        Span<byte> buffer = length > 512 ? new byte[length] : stackalloc byte[length];
        stream.ReadExactly(buffer);
        return encoding.GetString(buffer);
    }

    public string ReadNullTerminatedString()
    {
        return ReadNullTerminatedString(Encoding);
    }

    public string ReadNullTerminatedString(Encoding encoding)
    {
        if (encoding.IsSingleByte)
        {
            int start = Position;

            while (Position < Size)
            {
                if (ReadByte() == 0)
                    break;
            }
            int length = Position - start - 1;

            if (length <= 0)
                return string.Empty;

            Position = start;
            string str = ReadString(length, encoding);

            Position += 1; // skip terminating 0

            return str;
        }

        // Multi-byte encodings
        var bytes = new List<byte>();
        var decoder = encoding.GetDecoder();

        while (Position < Size)
        {
            byte b = ReadByte();

            if (b == 0)
            {
                // Check if the decoder is mid-character (expecting more bytes)
                char[] test = new char[1];
                decoder.Convert([0], 0, 1, test, 0, 1, true, out _, out int charsUsed, out _);

                if (charsUsed == 0 && bytes.Count > 0)
                {
                    // 0x00 is part of a multi-byte character, keep going
                    bytes.Add(b);
                    continue;
                }

                break; // real terminator
            }

            bytes.Add(b);
            decoder.GetChars([b], 0, 1, new char[2], 0, false);
        }

        return encoding.GetString([.. bytes]);
    }

    public byte PeekByte()
    {
        var b = ReadByte();
        Position -= 1;
        return b;
    }

    public word PeekWord()
    {
        var w = ReadWord();
        Position -= 2;
        return w;
    }

    public dword PeekDword()
    {
        var dw = ReadDword();
        Position -= 4;
        return dw;
    }

    public byte[] ReadToEnd()
    {
        return ReadBytes(Size - Position);
    }

    public byte[] ReadBytes(int amount)
    {
        CheckOutOfRange(amount);
        Span<byte> buffer = amount > 512 ? new byte[amount] : stackalloc byte[amount];
        stream.ReadExactly(buffer);
        return buffer.ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckOutOfRange(int sizeToRead)
    {
        if (Position + sizeToRead > end)
            throw new EndOfStreamException("Read beyond the data size.");
    }

    public void AlignToWord()
    {
        if (Position % 2 == 1)
            Position += 1;
    }

    public void AlignToDword()
    {
        if (Position % 4 != 0)
            Position += 4 - Position % 4;
    }

    public byte[] ToArray()
    {
        int oldPosition = Position;
        Position = 0;
        var data = ReadToEnd();
        Position = oldPosition;
        return data;
    }

    public long FindByteSequence(byte[] sequence, long offset)
    {
        if (length == 0)
            return -1;

        if (offset + sequence.Length > length)
            return -1;

        int oldPosition = Position;
        long lastIndex = length - sequence.Length;

        for (long i = offset; i <= lastIndex; ++i)
        {
            int j = 0;

            Position = (int)i;

            for (; j < sequence.Length; ++j)
            {
                if (ReadByte() != sequence[j])
                    break;
            }

            if (j == sequence.Length)
            {
                Position = oldPosition;
                return i;
            }
        }

        Position = oldPosition;
        return -1;
    }

    public long FindString(string str, long offset)
    {
        return FindByteSequence(Encoding.GetBytes(str), offset);
    }

    public void Dispose()
    {
        stream.Dispose();
    }
}
