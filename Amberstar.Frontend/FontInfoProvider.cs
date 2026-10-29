using Amberstar.Game;

namespace Amberstar.Frontend
{
	internal class FontInfoProvider
	(
		IReadOnlyDictionary<char, int> textGlyphTextureIndices,
		IReadOnlyDictionary<char, int> runeGlyphTextureIndices,
		IReadOnlyDictionary<char, int> textShadowTextureIndices,
		IReadOnlyDictionary<char, int> runeShadowTextureIndices
	) : IFontInfoProvider
	{
		public IReadOnlyDictionary<char, int> TextGlyphTextureIndices => textGlyphTextureIndices;
		public IReadOnlyDictionary<char, int> RuneGlyphTextureIndices => runeGlyphTextureIndices;
		public IReadOnlyDictionary<char, int> TextShadowTextureIndices => textShadowTextureIndices;
		public IReadOnlyDictionary<char, int> RuneShadowTextureIndices => runeShadowTextureIndices;
	}
}
