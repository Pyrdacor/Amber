using Amber.IO.Common.Serialization;

namespace Amber.Assets.Common;

public interface IWritableAsset : IAsset
{
	/// <summary>
	/// Serializes the asset's data to the given writer.
	/// </summary>
	void Write(IDataWriter writer);
}
