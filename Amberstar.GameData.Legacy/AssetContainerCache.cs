using Amber.Assets.Common;
using Amber.Common;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Amberstar.GameData.Atari")]

namespace Amberstar.GameData.Legacy;

internal class AssetContainerCache(AssetType assetType, Func<Dictionary<int, IAsset?>> loader)
{
	readonly Func<Dictionary<int, IAsset?>> loader = loader;
	Dictionary<int, IAsset?> cache = [];
	bool loaded = false;

	public AssetType AssetType { get; } = assetType;

	public virtual IAsset? GetAsset(int index)
	{
		if (!loaded)
		{
			cache = loader();
			loaded = true;
		}

		return cache.TryGetValue(index, out var asset) ? asset : null;
	}
}
