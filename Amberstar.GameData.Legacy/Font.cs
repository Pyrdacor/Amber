using Amber.Assets.Common;
namespace Amberstar.GameData.Legacy;

internal class Font(byte[] textConversionTable, byte[] runeConversionTable, Dictionary<int, IGraphic> glyphGraphics) : IFont
{
	public int Advance => 6;

	public int GlyphWidth => 8;

	public int GlyphHeight => 5;

	public int LineHeight => 7;

	public IGraphic? GetGlyph(char character, bool rune)
	{
		if (character <= 32)
			return null;

		int index = character - 32;

		var mappingTable = rune ? runeConversionTable : textConversionTable;

		if (index >= mappingTable.Length)
			return null;

		index = mappingTable[index];

		if (index < 0)
			return null;

		return glyphGraphics.GetValueOrDefault(index);
	}
}
