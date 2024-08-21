using Amberstar.GameData.Serialization;

namespace Amberstar.Game
{
	public interface IFontInfoProvider
	{
		IReadOnlyDictionary<char, int> TextGlyphTextureIndices { get; }
		IReadOnlyDictionary<char, int> RuneGlyphTextureIndices { get; }
	}
}
