namespace Amber.Serialization;

public interface IDataReader
{
    bool ReadBool();
    byte ReadByte();
    word ReadWord();
    dword ReadDword();
    qword ReadQword();
    string ReadChar();
    string ReadString();
    string ReadString(Encoding encoding);
    string ReadString(int length);
    string ReadString(int length, Encoding encoding);
    string ReadNullTerminatedString();
    string ReadNullTerminatedString(Encoding encoding);
    byte PeekByte();
    word PeekWord();
    dword PeekDword();
    int Position { get; set; }
    int Size { get; }
    byte[] ReadToEnd();
    byte[] ReadBytes(int amount);
    long FindByteSequence(byte[] sequence, long offset);
    long FindString(string str, long offset);
    void AlignToWord();
    void AlignToDword();
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