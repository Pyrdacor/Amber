using Amber.Assets.Common;
using Amber.Common;

namespace Amberstar.GameData.Legacy;

internal class Text(List<string> textFragments) : IText
{
	readonly List<string> textFragments = textFragments;
	readonly List<word> textIndices = [];
	readonly List<int> textBlockOffsets = [];

	public int TextBlockCount => textBlockOffsets.Count;

	public static Text Load(IAsset asset, List<string> textFragments)
	{
		var reader = asset.GetReader();

		int textCount = reader.ReadByte();
		reader.Position++; // skip fill byte

		var text = new Text(textFragments);

		if (textCount == 0)
			return text;

		var lengths = new int[textCount];
		int offset = reader.ReadWord();

		for (int i = 0; i < textCount; i++)
		{
			int nextOffset = reader.ReadWord();
			lengths[i] = nextOffset - offset;
			offset = nextOffset;
		}

		for (int i = 0; i < textCount; i++)
		{
			text.textBlockOffsets.Add(text.textIndices.Count);

			for (int n = 0; n < lengths[i]; n++)
				text.textIndices.Add(reader.ReadWord());
		}

		return text;
	}

	public static Text LoadSingleString(IAsset asset, List<string> textFragments)
	{
		var reader = asset.GetReader();

		var text = new Text(textFragments);
		text.textIndices.Add(reader.ReadWord());
		text.textBlockOffsets.Add(0);

		return text;
	}

	public IText GetTextBlock(int index)
	{
		if (index < 0 || index >= textBlockOffsets.Count)
			throw new AmberException(ExceptionScope.Application, "Text block index out of range");

		var textBlock = new Text(textFragments);
		int offset = textBlockOffsets[index];
		int nextOffset = index == textBlockOffsets.Count - 1 ? textIndices.Count : textBlockOffsets[index + 1];

		textBlock.textBlockOffsets.Add(0);
		textBlock.textIndices.AddRange(textIndices.Skip(offset).Take(nextOffset - offset));

		return textBlock;
	}

	public string GetString()
	{
		return textIndices.Count == 0 || textIndices[0] == 0 ? string.Empty : textFragments[textIndices[0]];
	}

	public List<string[]> GetParagraphs(int maxWidthInCharacters)
	{
		GetLines(maxWidthInCharacters, out var paragraphs);
		return paragraphs;
	}

	public string[] GetLines(int maxWidthInCharacters)
	{
		return GetLines(maxWidthInCharacters, out _);
	}

	private string[] GetLines(int maxWidthInCharacters, out List<string[]> paragraphs)
	{
		paragraphs = [];

		if (textIndices.Count == 0)
			return [];

		int paragraphOffset = 0;
		List<string> lines = [];
		string currentLine = string.Empty;

		for (int i = 0; i < textIndices.Count; i++)
		{
			word textIndex = textIndices[i];

			switch (textIndex)
			{
				case OpenBracket:
					currentLine += "(";
					break;
				case CarriageReturn:
					lines.Add(currentLine.TrimEnd(' '));
					currentLine = string.Empty;
					break;
				case ParagraphMarker:
					lines.Add(currentLine);
					if (paragraphs.Count == 0)
						paragraphs.Add(lines.ToArray());
					else
						paragraphs.Add(lines.Skip(paragraphOffset).ToArray());
					paragraphOffset = lines.Count;
					currentLine = string.Empty;
					break;
				default:
					if (IsEndPunctuation(textIndex) && currentLine.Length > 0 && currentLine[^1] == ' ')
						currentLine = currentLine[..^1];
					AddText(textFragments[textIndex] + " ");
					break;
			}

			void AddText(string text)
			{
				currentLine += text;

				while (currentLine.Length > maxWidthInCharacters)
				{
					int lastSpaceIndexWhichFits = FindLastSpaceIndexWhichFits();

					if (lastSpaceIndexWhichFits == -1)
						throw new AmberException(ExceptionScope.Data, "Text line too long.");

					lines.Add(currentLine[..lastSpaceIndexWhichFits]);
					currentLine = currentLine[(lastSpaceIndexWhichFits + 1)..];
				}

				int FindLastSpaceIndexWhichFits()
				{
					for (int i = maxWidthInCharacters - 1; i >= 0; i--)
					{
						if (currentLine[i] == ' ')
							return i;
					}

					return -1;
				}
			}
		}

		if (currentLine.Length != 0)
			lines.Add(currentLine.TrimEnd(' '));

		paragraphs.Add(lines.Skip(paragraphOffset).ToArray());

		return lines.ToArray();
	}

	public const int OpenBracket = 1580;
	public const int ClosingBracket = 1581;
	public const int ExclamationMark = 631;
	public const int CarriageReturn = 1576;
	public const int ParagraphMarker = 1577;
	public const int SingleQuote = 1300;
	public const int Comma = 166;
	public const int DoubleColon = 155;
	public const int SemiColon = 1302;
	public const int FullStop = 170;
	public const int QuestionMark = 743;

	private static bool IsEndPunctuation(int word)
	{
		return word == ExclamationMark || word == ClosingBracket ||
			   word == SingleQuote || word == Comma ||
			   word == DoubleColon || word == SemiColon ||
			   word == FullStop || word == QuestionMark;
	}
}