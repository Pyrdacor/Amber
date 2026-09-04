using Amber.Common;
using Amber.IO.Common.Serialization;

namespace Amber.Assets.Common;

public interface IAsset
{
	AssetIdentifier Identifier { get; }
	/// <summary>
	/// Returns a reader positioned at the start of the asset's raw data.
	/// </summary>
	IDataReader GetReader();
}
