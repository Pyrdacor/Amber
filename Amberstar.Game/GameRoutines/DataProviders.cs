using Amber.Common;
using Amberstar.GameData;

namespace Amberstar.Game;

partial class Game
{
    public const uint GoldWeight = 10;

	internal IText? CurrentText { get; private set; }
    internal IItem? CurrentItem { get; set; }
    internal int CurrentAmount { get; set; } // Result from amount input screen etc (e.g. gold amount to give)
    internal int CurrentMaxAmount { get; set; } // For amount input screen etc (e.g. gold amount to give)
    internal string? CurrentWord { get; set; }
    internal IText LoadUIText(UIText uiText) => AssetProvider.TextLoader.LoadText(new(AssetType.UIText, (int)uiText));
    internal int GetMaxLineLength(IText text)
    {
        var lines = text.GetLines(int.MaxValue);

        return lines.Max(line => line.Count(ch => ch >= 32));
    }
    internal int GetMaxLineWidth(IText text)
    {
        var glyphAdvance = AssetProvider.FontLoader.LoadFont().Advance;

        return GetMaxLineLength(text) * glyphAdvance;
    }
}
