using Amber.Common;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy;

internal class ChestLoader(AssetProvider assetProvider) : IChestLoader
{
	private readonly Dictionary<int, Chest> chests = [];

    public IChest LoadChest(int index)
    {
        if (!chests.TryGetValue(index, out var chest))
        {
            var asset = assetProvider.GetAsset(new(AssetType.Chest, index));

            if (asset == null)
                throw new AmberException(ExceptionScope.Data, $"Chest {index} not found.");

            chest = Chest.Load(asset);
            chest.Index = index;

            chests.Add(index, chest);
        }

        return chest;
    }
}
