namespace Amberstar.GameData;

public interface IText
{
	/// <summary>
	/// Gets a single string. If the text consists of more than one string, only the first fragment is returned.
	/// 
	/// If the text fragment reference is 0, an empty string is returned.
	/// </summary>
	string GetString();
	string[] GetLines(int maxWidthInCharacters);
	List<string[]> GetParagraphs(int maxWidthInCharacters);
}
