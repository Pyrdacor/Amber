using Amber.Assets.Common;
using Amber.Common;
using Amber.IO.Common.Serialization;

namespace Amberstar.GameData.Legacy;

internal class Asset(AssetIdentifier identifier, IDataReader reader) : IAsset
{
	readonly IDataReader reader = reader;

	public AssetIdentifier Identifier { get; } = identifier;

	public IDataReader GetReader() => reader;
}
