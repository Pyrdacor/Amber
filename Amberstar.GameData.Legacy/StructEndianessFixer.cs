internal static class StructEndianessFixer
{
    public abstract class Fixer
    {
        public abstract void Fix(byte[] dsta);
    }

    public class WordFixer(int index) : Fixer
    {
        public override void Fix(byte[] data)
        {
            (data[0], data[1]) = (data[1], data[0]);
        }
    }

    public class WordArrayFixer(int index, int length) : Fixer
    {
        public override void Fix(byte[] data)
        {
            int offset = index;

            for (int i = 0; i < length; i++)
                (data[offset], data[offset + 1]) = (data[++offset], data[offset++ - 1]);
        }
    }

    public byte[] FixData(byte[] data, params Fixer[] fixers)
    {
        foreach (var fixer in fixers)
        {
            fixer.Fix(data);
        }
    }
}