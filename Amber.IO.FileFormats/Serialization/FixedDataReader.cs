using System.Runtime.InteropServices;

namespace Amber.Serialization;

public unsafe sealed class FixedDataReader : IDataReader
{
    public static readonly Encoding Encoding;
    private readonly byte* ptr;
    private int position = 0;
    public int Position
    {
        get => position;
        set
        {
            if (value < 0 || value > Size)
                throw new IndexOutOfRangeException("Data index out of range.");

            position = value;
        }
    }
    public int Size { get; } = 0;

    public byte this[int index] => *(ptr + index);

    static FixedDataReader()
    {
		Encoding = new AmberEncoding();
	}

    public FixedDataReader(byte* ptr, int size)
    {
        this.ptr = ptr;
        Size = size;
    }

    public bool ReadBool()
    {
        CheckOutOfRange(1);
        return this[Position++] != 0;
    }

    public byte ReadByte()
    {
        CheckOutOfRange(1);
        return this[Position++];
    }

    public word ReadWord()
    {
        CheckOutOfRange(2);
        return (word)((this[Position++] << 8) | this[Position++]);
    }

    public dword ReadDword()
    {
        CheckOutOfRange(4);
        return (((dword)this[Position++] << 24) | ((dword)this[Position++] << 16) | ((dword)this[Position++] << 8) | this[Position++]);
    }

    public qword ReadQword()
    {
        CheckOutOfRange(8);
        return (((qword)this[Position++] << 56) | ((qword)this[Position++] << 48) | ((qword)this[Position++] << 40) |
            ((qword)this[Position++] << 32) | ((qword)this[Position++] << 24) | ((qword)this[Position++] << 16) |
            ((qword)this[Position++] << 8) | this[Position++]);
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
        var str = encoding.GetString(ptr + Position, length);
        str = str.Replace(encoding.GetString(new byte[] { 0xb4 }), "'");
        Position += length;
        return str;
    }

    public string ReadNullTerminatedString()
    {
        return ReadNullTerminatedString(Encoding);
    }

    public string ReadNullTerminatedString(Encoding encoding)
    {
        List<byte> buffer = [];
        byte b;
        bool needMoreBytes = false;

        while (Position < Size && ((b = ReadByte()) != 0 || needMoreBytes))
        {
            buffer.Add(b);

            // When parsing multi-byte encodings there might be characters which
            // end with a 00-byte. As this is also used for termination we have
            // to check for character ending if the next byte is 00.
            if (!encoding.IsSingleByte && Position < Size && PeekByte() == 0)
            {
                try
                {
                    encoding.GetString(buffer.ToArray());
                }
                catch (ArgumentException)
                {
                    needMoreBytes = true;
                }
            }
        }

        try
        {
            return encoding.GetString(buffer.ToArray());
        }
        catch (ArgumentException)
        {
            return encoding.GetString(buffer.Take(buffer.Count - 1).ToArray()) + "?";
        }
    }

    public byte PeekByte()
    {
        CheckOutOfRange(1);
        return this[Position];
    }

    public word PeekWord()
    {
        CheckOutOfRange(2);
        return (word)((this[Position] << 8) | this[Position + 1]);
    }

    public dword PeekDword()
    {
        CheckOutOfRange(4);
        return (dword)((this[Position] << 24) | (this[Position + 1] << 16) | (this[Position + 2] << 8) | this[Position + 3]);
    }

    public byte[] ReadToEnd()
    {
        return ReadBytes(Size - Position);
    }

    public byte[] ReadBytes(int amount)
    {
        var data = new byte[amount];
        Marshal.Copy(new IntPtr(ptr + Position), data, 0, data.Length);
        Position += amount;
        return data;
    }

    private void CheckOutOfRange(int sizeToRead)
    {
        if (Position + sizeToRead > Size)
            throw new EndOfStreamException("Read beyond the data size.");
    }

    public long FindByteSequence(byte[] sequence, long offset)
    {
        if (ptr == null)
            return -1;

        if (offset + sequence.Length > Size)
            return -1;

        long lastIndex = Size - sequence.Length;

        for (long i = offset; i <= lastIndex; ++i)
        {
            int j = 0;
            byte* check = ptr + i;

            for (; j < sequence.Length; ++j)
            {
                if (*check++ != sequence[j])
                    break;
            }

            if (j == sequence.Length)
                return i;
        }

        return -1;
    }

    public long FindString(string str, long offset)
    {
        return FindByteSequence(Encoding.GetBytes(str), offset);
    }

    public void AlignToWord()
    {
        if (Position % 2 == 1)
            ++Position;
    }

    public void AlignToDword()
    {
        if (Position % 4 != 0)
            Position += 4 - Position % 4;
    }

    public byte[] ToArray()
    {
		var data = new byte[Size];
		Marshal.Copy(new IntPtr(ptr), data, 0, data.Length);
        return data;
	}
}
