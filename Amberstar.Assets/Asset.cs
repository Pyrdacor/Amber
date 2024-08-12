using Amber.Assets.Common;
using Amber.Common;
using Amber.Serialization;

namespace Amberstar.Assets
{
	internal class Asset(AssetIdentifier identifier, IDataReader reader) : IAsset
	{
		readonly IDataReader reader = reader;

		public AssetIdentifier Identifier { get; } = identifier;

		public IDataReader GetReader() => reader;
	}
}
