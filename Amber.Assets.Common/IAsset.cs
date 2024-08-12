using Amber.Common;
using Amber.Serialization;

namespace Amber.Assets.Common;

public interface IAsset
{
	AssetIdentifier Identifier { get; }
	IDataReader GetReader();
}
