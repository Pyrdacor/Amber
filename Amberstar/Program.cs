using Amber.IO.FileSystem;
using Amberstar.Assets;

namespace Amberstar
{
	internal class Program
	{
		static void Main(string[] args)
		{
			var fileSystem = FileSystem.FromOperatingSystemPath(@"D:\Projects\Amber\German\Amberfiles");

			var assetProvider = new AssetProvider(fileSystem.AsReadOnly());
		}
	}
}
