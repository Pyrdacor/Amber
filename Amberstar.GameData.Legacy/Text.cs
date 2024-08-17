using Amber.Assets.Common;
using Amber.Common;

namespace Amberstar.GameData.Legacy;

internal class Text(List<string> textFragments) : IText
{
	readonly List<string> textFragments = textFragments;

	public static Text Load(IAsset asset, List<string> textFragments)
	{
		var reader = asset.GetReader();

		int textCount = reader.ReadByte();
		reader.Position++; // skip fill byte

		var text = new Text(textFragments);
		int offset = reader.ReadWord();

		for (int i = 0; i < textCount; i++)
		{
			int nextOffset = reader.ReadWord();
			text.TextLengths.Add(nextOffset - offset);
			offset = nextOffset;
		}

		for (int i = 0; i < textCount; i++)
		{
			text.TextIndices.Add(reader.ReadWord());
		}

		return text;
	}

	public static Text LoadSingleString(IAsset asset, List<string> textFragments)
	{
		var reader = asset.GetReader();

		var text = new Text(textFragments);
		text.TextIndices.Add(reader.ReadWord());
		text.TextLengths.Add(int.MaxValue);

		return text;
	}

	public string GetString()
	{
		return TextIndices[0] == 0 ? string.Empty : textFragments[TextIndices[0]];
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
		int paragraphOffset = 0;
		List<string> lines = [];
		string currentLine = string.Empty;

		for (int i = 0; i < TextIndices.Count; i++)
		{
			word textIndex = TextIndices[i];
			int length = TextLengths[i];

			if (length <= 0)
				continue;

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
					length = Math.Min(length, textFragments[textIndex].Length);
					AddText(textFragments[textIndex][..length]);
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

		return lines.ToArray();
	}

	public List<int> TextLengths { get; private init; } = [];
	public List<word> TextIndices { get; private init; } = [];

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