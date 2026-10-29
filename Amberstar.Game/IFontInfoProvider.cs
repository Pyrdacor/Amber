namespace Amberstar.Game;

public interface IFontInfoProvider
{
	IReadOnlyDictionary<char, int> TextGlyphTextureIndices { get; }
	IReadOnlyDictionary<char, int> RuneGlyphTextureIndices { get; }
	/// <summary>
	/// The shadows of the glyphs (without the pixels which are covered by the glyph, so that faded texts look right).
	/// </summary>
	IReadOnlyDictionary<char, int> TextShadowTextureIndices { get; }
	IReadOnlyDictionary<char, int> RuneShadowTextureIndices { get; }
}
