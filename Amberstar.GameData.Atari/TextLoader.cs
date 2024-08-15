using Amber.Assets.Common;
using Amber.Common;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Atari;

internal class TextLoader(IAssetProvider assetProvider, List<string> textFragments) : ITextLoader
{
	readonly List<string> textFragments = textFragments;
	readonly Dictionary<AssetIdentifier, IText> texts = [];

	public IText LoadText(AssetIdentifier assetIdentifier)
	{
		bool singleString = false;

		switch (assetIdentifier.Type)
		{
			case AssetType.SpellName:
			case AssetType.SpellSchoolName:
			case AssetType.ClassName:
			case AssetType.SkillName:
			case AssetType.CharInfoText:
			case AssetType.RaceName:
			case AssetType.ConditionName:
			case AssetType.ItemTypeName:
				singleString = true;
				// TODO: add more text asset types
				break;
			default:
				throw new AmberException(ExceptionScope.Application, $"Invalid text asset type {assetIdentifier.Type}.");
		}

		if (!texts.TryGetValue(assetIdentifier, out var text))
		{
			var asset = assetProvider.GetAsset(assetIdentifier);

			if (asset == null)
				throw new AmberException(ExceptionScope.Data, $"Asset {assetIdentifier} not found.");

			text = singleString ? Text.LoadSingleString(asset, textFragments) : Text.Load(asset, textFragments);
			texts.Add(assetIdentifier, text);
		}

		return text;
	}
}
