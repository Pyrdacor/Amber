using Amber.Assets.Common;

namespace Amberstar.GameData.Serialization
{
	public interface ILabDataLoader
	{
		ILabData LoadLabData(int index);
	}
}
