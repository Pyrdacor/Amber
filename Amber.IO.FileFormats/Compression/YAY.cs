using System.Buffers.Binary;
using Amber.IO.Common.Serialization;

namespace Amber.IO.FileFormats.Compression;

public static class YAY
{
    private static unsafe void RunXor(byte[] data, word key)
    {
        int count = data.Length / 2;

        if (count == 0)
            return;

        word add = 0x57;

        fixed (byte* p = data)
        {
            word* ptr = (word*)p;

            while (count-- != 0)
            {
                word mask = key;
                *ptr = BinaryPrimitives.ReverseEndianness(
                    (word)(BinaryPrimitives.ReverseEndianness(*ptr) ^ mask)
                );
                ptr++;
                key <<= 4;
                key += mask;
                key += add;
            }
        }
    }

    /// <summary>
    /// This will decrypt data with the YAY! encryption.
    /// </summary>
    public static byte[] Decrypt(byte[] data)
    {
        // First 2 bytes of data are the encryption key!
        word key = data[0];
        key <<= 8;
        key |= data[1];
        key ^= 0x4a48; // "JH"

        // The XOR runs on the key as well
        RunXor(data, key);

        // Now skip the key
        return data[2..];
    }

    public static byte[] Decrypt(IDataReader reader)
    {
        return Decrypt(reader.ReadToEnd());
    }

    /// <summary>
    /// This will encrypt data with the YAY! encryption.
    /// 
    /// In original the key is generated randomly.
    /// </summary>
    public static byte[] Encrypt(byte[] data, word key = 0xd2f9)
    {
        // Prepend "JH"
        data = [0x4a, 0x48, .. data];

        RunXor(data, key);

        return data;
    }
}
