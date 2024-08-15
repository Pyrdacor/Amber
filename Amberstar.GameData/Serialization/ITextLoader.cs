using Amber.Common;

namespace Amberstar.GameData.Serialization
{
	public interface ITextLoader
	{
		IText LoadText(AssetIdentifier assetIdentifier);
	}
}
