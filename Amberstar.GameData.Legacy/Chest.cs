using Amber.Assets.Common;

namespace Amberstar.GameData.Legacy;

internal class Chest : IChest
{
	readonly IItem?[] items = new IItem?[IChest.SlotCount];

	public static Chest Load(IAsset asset)
	{
		var chest = new Chest();
		var itemLoader = new ItemLoader();
        var reader = asset.GetReader();
		int offset = 0;

		for (int i = 0; i < IChest.SlotCount; i++)
		{
			if (offset + 40 > reader.Size)
				break;

			var item = itemLoader.ReadItem(reader);

			if (item.Index != 0)
				chest.items[i] = item;
        }

		return chest;
	}

	public int Index { get; set; }

	public IItem?[] Items => items;
}