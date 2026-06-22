namespace Amber.IO.FileFormats.Compression;

public static class Delta
{
    public static byte[] Decompress(byte[] data)
    {
        var result = new byte[data.Length];

        result[0] = data[0];

        for (int i = 1; i < data.Length; i++)
            result[i] = (byte)(result[i - 1] + data[i]);

        return result;
    }

    public static byte[] Compress(byte[] data)
    {
        var result = new byte[data.Length];

        result[0] = data[0];

        for (int i = 1; i < data.Length; i++)
            result[i] = (byte)(data[i] - data[i - 1]);

        return result;
    }
}
