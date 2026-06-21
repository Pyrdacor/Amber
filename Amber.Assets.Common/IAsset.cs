using Amber.Common;
using Amber.IO.Common.Serialization;

namespace Amber.Assets.Common;

public interface IAsset
{
	AssetIdentifier Identifier { get; }
	IDataReader GetReader();
}
