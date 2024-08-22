using Amber.Common;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy
{
	public class SavegameLoader(Amber.Assets.Common.IAssetProvider assetProvider) : ISavegameLoader
	{
		Savegame? savegame;

		public ISavegame LoadSavegame()
		{
			if (savegame != null)
				return savegame;

			var asset = assetProvider.GetAsset(new AssetIdentifier(AssetType.Savegame, 1));

			if (asset == null)
				throw new AmberException(ExceptionScope.Data, $"No savegame found.");

			return savegame = new Savegame(asset.GetReader());
		}
	}
}
