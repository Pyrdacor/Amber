using Amber.Assets.Common;
using Amber.Common;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Atari
{
	internal class TextLoader(List<string> textFragments) : ITextLoader
	{
		readonly List<string> textFragments = textFragments;
		readonly Dictionary<AssetIdentifier, IText> texts = [];

		public IText LoadText(IAsset asset)
		{
			if (!texts.TryGetValue(asset.Identifier, out var map))
			{
				map = Text.Load(asset, textFragments);
				texts.Add(asset.Identifier, map);
			}

			return map;
		}
	}
}
