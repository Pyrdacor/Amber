namespace Amberstar.GameData;

public interface IText
{
	string[] GetLines(int maxWidthInCharacters);
	List<string[]> GetParagraphs(int maxWidthInCharacters);
}
