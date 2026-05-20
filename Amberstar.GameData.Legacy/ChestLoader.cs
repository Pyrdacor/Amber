using Amber.Common;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy;

internal class ChestLoader(AssetProvider assetProvider) : IChestLoader
{
	private readonly Dictionary<int, IChest> chests = [];

    public IChest LoadChest(int index)
    {
        if (!chests.TryGetValue(index, out var chest))
        {
            var asset = assetProvider.GetAsset(new(AssetType.Chest, index));

            if (asset == null)
                throw new AmberException(ExceptionScope.Data, $"Chest {index} not found.");

            chest = Chest.Load(asset) with { Index = index };

            chests.Add(index, chest);
        }

        return chest;
    }

    public IReadOnlyDictionary<int, IChest> LoadAllChests()
    {
        var keys = assetProvider.GetAssetKeys(AssetType.Chest);

        if (chests.Count == keys.Count)
            return chests.AsReadOnly();

        foreach (var key in keys)
        {
            if (chests.ContainsKey(key))
                continue;

            var asset = assetProvider.GetAsset(new(AssetType.Chest, key));

            if (asset == null)
                throw new AmberException(ExceptionScope.Data, $"Chest {key} not found.");

            var chest = Chest.Load(asset) with { Index = key };
            chests.Add(key, chest);
        }

        return chests.AsReadOnly();
    }
}
