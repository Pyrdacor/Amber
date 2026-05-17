using Amber.Common;
using Amber.Serialization;

namespace Amberstar.GameData.Serialization;

public interface ITextLoader
{
	IText LoadText(AssetIdentifier assetIdentifier);
    IText ReadText(IDataReader dataReader);
    IText FromString(string text);
    IText FromTextFragmentIndex(word index);
}

public static class TextLoaderExtensions
{
    public static IText Concat(this ITextLoader textLoader, IText left, IText right)
    {
        return textLoader.FromString(left.GetString() + right.GetString());
    }

    public static IText Concat(this ITextLoader textLoader, string left, IText right)
    {
        return textLoader.FromString(left + right.GetString());
    }

    public static IText Concat(this ITextLoader textLoader, IText left, string right)
    {
        return textLoader.FromString(left.GetString() + right);
    }

    public static IText ConcatWithSeparator(this ITextLoader textLoader, IText left, string separator, IText right)
    {
        return textLoader.FromString(left.GetString() + separator + right.GetString());
    }
}