namespace Amber.IO.Common.Serialization;

public interface IDataReader
{
    /// <summary>
    /// Reads a byte as a boolean (0 is false, everything else is true).
    /// </summary>
    bool ReadBool();
    /// <summary>
    /// Reads a single byte.
    /// </summary>
    byte ReadByte();
    /// <summary>
    /// Reads a 16 bit unsigned value in big-endian byte order.
    /// </summary>
    word ReadWord();
    /// <summary>
    /// Reads a 32 bit unsigned value in big-endian byte order.
    /// </summary>
    dword ReadDword();
    /// <summary>
    /// Reads a 64 bit unsigned value in big-endian byte order.
    /// </summary>
    qword ReadQword();
    /// <summary>
    /// Reads a single character.
    /// </summary>
    string ReadChar();
    /// <summary>
    /// Reads a length-prefixed string using the default encoding.
    /// </summary>
    string ReadString();
    /// <summary>
    /// Reads a length-prefixed string using the given encoding.
    /// </summary>
    string ReadString(Encoding encoding);
    /// <summary>
    /// Reads a fixed-length string using the default encoding.
    /// </summary>
    string ReadString(int length);
    /// <summary>
    /// Reads a fixed-length string using the given encoding.
    /// </summary>
    string ReadString(int length, Encoding encoding);
    /// <summary>
    /// Reads a string up to (and consuming) the next null terminator, using the default encoding.
    /// </summary>
    string ReadNullTerminatedString();
    /// <summary>
    /// Reads a string up to (and consuming) the next null terminator, using the given encoding.
    /// </summary>
    string ReadNullTerminatedString(Encoding encoding);
    /// <summary>
    /// Reads a byte without advancing the position.
    /// </summary>
    byte PeekByte();
    /// <summary>
    /// Reads a 16 bit unsigned value (big-endian) without advancing the position.
    /// </summary>
    word PeekWord();
    /// <summary>
    /// Reads a 32 bit unsigned value (big-endian) without advancing the position.
    /// </summary>
    dword PeekDword();
    /// <summary>
    /// Current read position, in bytes.
    /// </summary>
    int Position { get; set; }
    /// <summary>
    /// Total size of the underlying data, in bytes.
    /// </summary>
    int Size { get; }
    /// <summary>
    /// Reads all remaining bytes from the current position to the end.
    /// </summary>
    byte[] ReadToEnd();
    /// <summary>
    /// Reads the given number of bytes.
    /// </summary>
    byte[] ReadBytes(int amount);
    /// <summary>
    /// Searches for a byte sequence starting at the given offset. Returns its position, or -1 if not found.
    /// </summary>
    long FindByteSequence(byte[] sequence, long offset);
    /// <summary>
    /// Searches for a string starting at the given offset. Returns its position, or -1 if not found.
    /// </summary>
    long FindString(string str, long offset);
    /// <summary>
    /// Advances the position to the next word (2 byte) boundary.
    /// </summary>
    void AlignToWord();
    /// <summary>
    /// Advances the position to the next dword (4 byte) boundary.
    /// </summary>
    void AlignToDword();
    /// <summary>
    /// Returns a copy of the underlying data.
    /// </summary>
    byte[] ToArray();
}

public static class DataReaderExtensions
{
    public static int[] ReadWords(this IDataReader reader, int count)
    {
        int[] words = new int[count];

        for (int i = 0; i < count; i++)
            words[i] = reader.ReadWord();

        return words;
    }

    public static byte[] PeekBytes(this IDataReader reader, int count)
    {
        int position = reader.Position;
        byte[] bytes = reader.ReadBytes(count);
        reader.Position = position;

        return bytes;

    }
}