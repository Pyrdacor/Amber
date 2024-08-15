using Amber.Assets.Common;

namespace Amberstar.GameData.Serialization
{
	public interface ITextLoader
	{
		IText LoadText(IAsset asset);
	}
}
