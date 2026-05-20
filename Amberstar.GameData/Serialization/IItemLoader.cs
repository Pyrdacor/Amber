using Amber.Assets.Common;
using Amber.Serialization;

namespace Amberstar.GameData.Serialization;

public interface IItemLoader
{
	IStaticItemData LoadItem(uint index);
    IItem ReadItem(IDataReader reader);
}
