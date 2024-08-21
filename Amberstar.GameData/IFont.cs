using Amber.Assets.Common;

namespace Amberstar.GameData;

public interface IFont
{
	IGraphic? GetGlyph(char character, bool rune);

	int Advance { get; }
	int GlyphWidth { get; }
	int GlyphHeight { get; }
	int LineHeight { get; }
}
