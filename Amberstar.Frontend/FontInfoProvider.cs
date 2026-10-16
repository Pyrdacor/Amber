using Amberstar.Game;

namespace Amberstar.Frontend
{
	internal class FontInfoProvider
	(
		IReadOnlyDictionary<char, int> textGlyphTextureIndices,
		IReadOnlyDictionary<char, int> runeGlyphTextureIndices
	) : IFontInfoProvider
	{
		public IReadOnlyDictionary<char, int> TextGlyphTextureIndices => textGlyphTextureIndices;
		public IReadOnlyDictionary<char, int> RuneGlyphTextureIndices => runeGlyphTextureIndices;
	}
}
