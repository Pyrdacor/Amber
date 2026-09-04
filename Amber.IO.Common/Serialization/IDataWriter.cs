namespace Amber.IO.Common.Serialization;

public interface IDataWriter
{
    /// <summary>
    /// Current write position, in bytes. Only grows as data is appended.
    /// </summary>
    int Position { get; }
    /// <summary>
    /// Total size of the written data, in bytes.
    /// </summary>
    int Size { get; }
    /// <summary>
    /// Appends a boolean as a single byte (1 for true, 0 for false).
    /// </summary>
    void Write(bool value);
    /// <summary>
    /// Appends a single byte.
    /// </summary>
    void Write(byte value);
    /// <summary>
    /// Appends a 16 bit unsigned value in big-endian byte order.
    /// </summary>
    void Write(word value);
    /// <summary>
    /// Appends a 32 bit unsigned value in big-endian byte order.
    /// </summary>
    void Write(dword value);
    /// <summary>
    /// Appends a 64 bit unsigned value in big-endian byte order.
    /// </summary>
    void Write(qword value);
    /// <summary>
    /// Appends a single character using the default encoding.
    /// </summary>
    void Write(char value);
    /// <summary>
    /// Appends a length-prefixed string using the default encoding (max 255 bytes).
    /// </summary>
    void Write(string value);
    /// <summary>
    /// Pads or truncates the string to the given length, then appends it length-prefixed using the default encoding.
    /// </summary>
    void Write(string value, int length, char fillChar = ' ');
    /// <summary>
    /// Appends a length-prefixed string using the given encoding (max 255 bytes).
    /// </summary>
    void Write(string value, Encoding encoding);
    /// <summary>
    /// Pads or truncates the string to the given length, then appends it length-prefixed using the given encoding.
    /// </summary>
    void Write(string value, Encoding encoding, int length, char fillChar = ' ');
    /// <summary>
    /// Appends raw bytes.
    /// </summary>
    void Write(byte[] bytes);
    /// <summary>
    /// Overwrites the boolean byte at the given offset. The offset must already lie within the written data.
    /// </summary>
    void Replace(int offset, bool value);
    /// <summary>
    /// Overwrites the byte at the given offset. The offset must already lie within the written data.
    /// </summary>
    void Replace(int offset, byte value);
    /// <summary>
    /// Overwrites 2 bytes at the given offset with a big-endian value. The range must already lie within the written data.
    /// </summary>
    void Replace(int offset, word value);
    /// <summary>
    /// Overwrites 4 bytes at the given offset with a big-endian value. The range must already lie within the written data.
    /// </summary>
    void Replace(int offset, dword value);
    /// <summary>
    /// Overwrites 8 bytes at the given offset with a big-endian value. The range must already lie within the written data.
    /// </summary>
    void Replace(int offset, qword value);
    /// <summary>
    /// Overwrites the data at the given offset with all of the given bytes. The range must already lie within the written data.
    /// </summary>
    void Replace(int offset, byte[] data);
    /// <summary>
    /// Overwrites the data at the given offset with the given bytes, starting at <paramref name="dataOffset"/>. The range must already lie within the written data.
    /// </summary>
    void Replace(int offset, byte[] data, int dataOffset);
    /// <summary>
    /// Overwrites <paramref name="length"/> bytes at the given offset with bytes taken from <paramref name="data"/> starting at <paramref name="dataOffset"/>. The range must already lie within the written data.
    /// </summary>
    void Replace(int offset, byte[] data, int dataOffset, int length);
    /// <summary>
    /// Writes all data to the given stream.
    /// </summary>
    void CopyTo(Stream stream);
    /// <summary>
    /// Returns a copy of the written data.
    /// </summary>
    byte[] ToArray();
    /// <summary>
    /// Returns a copy of a range of the written data.
    /// </summary>
    byte[] GetBytes(int offset, int length);
    /// <summary>
    /// Appends an enum value as a single byte.
    /// </summary>
    void WriteEnumAsByte<T>(T value) where T : struct, Enum, IConvertible;
    /// <summary>
    /// Appends an enum value as a 16 bit word.
    /// </summary>
    void WriteEnumAsWord<T>(T value) where T : struct, Enum, IConvertible;
    /// <summary>
    /// Appends the string followed by a null terminator, using the default encoding, with no length prefix.
    /// </summary>
    void WriteNullTerminated(string value);
    /// <summary>
    /// Appends the string followed by a null terminator, using the given encoding, with no length prefix.
    /// </summary>
    void WriteNullTerminated(string value, Encoding encoding);
    /// <summary>
    /// Appends the string using the default encoding, with no length prefix and no terminator.
    /// </summary>
    void WriteWithoutLength(string value);
    /// <summary>
    /// Appends the string using the given encoding, with no length prefix and no terminator.
    /// </summary>
    void WriteWithoutLength(string value, Encoding encoding);
}

public static class DataWriterExtensions
{
    public static void WriteBytes(this IDataWriter writer, int[] bytes)
    {
        foreach (var b in bytes)
        {
            writer.Write((byte)b);
        }
    }

    public static void WriteWords(this IDataWriter writer, int[] words)
    {
        foreach (var word in words)
        {
            writer.Write((word)word);
        }
    }

    public static void WriteWords(this IDataWriter writer, word[] words)
    {
        foreach (var word in words)
        {
            writer.Write(word);
        }
    }
}