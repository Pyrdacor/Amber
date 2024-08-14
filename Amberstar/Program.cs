using Amber.Common;
using Amber.IO.FileSystem;
using Amberstar.Assets;
using Amberstar.GameData.Atari;

namespace Amberstar
{
	internal class Program
	{
		static void Main(string[] args)
		{
			var fileSystem = FileSystem.FromOperatingSystemPath(@"D:\Projects\Amber\German\AmberfilesST");

			var assetProvider = new AtariAssetProvider(fileSystem.AsReadOnly());

			var assetId = new AssetIdentifier(AssetType.Place, 1);
			var asset = assetProvider.GetAsset(assetId);
		}
	}
}
