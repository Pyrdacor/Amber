using Amber.Common;
using Amberstar.GameData;

namespace Amberstar.Game;

partial class Game
{
    public const uint GoldWeight = 10;
    public const uint FoodWeight = 250;

	internal IText? CurrentText { get; private set; }
    internal IItem? CurrentItem { get; set; }
    internal int CurrentAmount { get; set; } // Result from amount input screen etc (e.g. gold amount to give)
    internal int CurrentMaxAmount { get; set; } // For amount input screen etc (e.g. gold amount to give)
    internal string? CurrentWord { get; set; }
    internal bool CurrentResult { get; set; } = false;
    /// <summary>
    /// The confirmation uses the large window of the combat requester.
    /// </summary>
    internal bool CurrentConfirmationLarge { get; set; } = false;
    /// <summary>
    /// Gets the whole text as a string. In contrast to <see cref="IText.GetString"/>
    /// this contains all text fragments (e.g. all words of a message).
    /// </summary>
    internal static string GetFullText(IText text) => string.Join(" ", text.GetParagraphs(1000).Select(lines => string.Join(" ", lines)));

    internal IText LoadUIText(UIText uiText) => AssetProvider.TextLoader.LoadText(new(AssetType.UIText, (int)uiText));
    internal IText LoadMessageText(Message message) => AssetProvider.TextLoader.LoadText(new(AssetType.Message, (int)message));
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
