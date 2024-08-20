using Amber.Assets.Common;

internal class StructEndianessFixer
{
    public class Builder
    {
        readonly StructEndianessFixer fixer = new();

        public Builder Word(int offset)
        {
			fixer.fixers.Add(new WordFixer(offset));
            return this;
        }

		public Builder WordArray(int offset, int length)
		{
			fixer.fixers.Add(new WordArrayFixer(offset, length));
			return this;
		}

        public Builder WordGap(int startIndex, int groupLength, int groupCount, int gap)
        {
            fixer.fixers.Add(new WordGapFixer(startIndex, groupLength, groupCount, gap));
            return this;
        }

		public StructEndianessFixer Build() => fixer;
    }

	readonly List<Fixer> fixers = [];

	private StructEndianessFixer()
    {

    }

	private abstract class Fixer
    {
        public abstract void Fix(byte[] dsta);
    }

    static void FixWord(byte[] data, int index)
    {
		(data[index], data[index + 1]) = (data[index + 1], data[index]);
	}

	private class WordFixer(int index) : Fixer
    {
        public override void Fix(byte[] data) => FixWord(data, index);
    }

	private class WordArrayFixer(int index, int length) : Fixer
    {
        public override void Fix(byte[] data)
        {
            for (int i = 0; i < length; i++)
                FixWord(data, index + i * 2);
        }
    }

	private class WordGapFixer(int startIndex, int groupLength, int groupCount, int gap) : Fixer
	{
		public override void Fix(byte[] data)
		{
			int offset = startIndex;

			for (int g = 0; g < groupCount; g++)
			{
                for (int i = 0; i < groupLength; i++)
                {
                    FixWord(data, offset);
                    offset += 2;
                }

                offset += gap;
            }
		}
	}

	public void FixData(byte[] data)
    {
        foreach (var fixer in fixers)
        {
            fixer.Fix(data);
        }
    }
}