using Amber.IO.Common.Serialization;

namespace Amber.Assets.Common;

public interface IWritableAsset : IAsset
{
	void Write(IDataWriter writer);
}
