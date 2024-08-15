using Amber.Common;
using Amber.IO.FileSystem;
using Amberstar.GameData.Atari;

namespace Amberstar
{
	internal class Program
	{
		static void Main(string[] args)
		{
			var fileSystem = FileSystem.FromOperatingSystemPath(@"D:\Projects\Amber\German\AmberfilesST");

			var assetProvider = new AtariAssetProvider(fileSystem.AsReadOnly());

			void PrintTexts(AssetType assetType, int count, int start = 0)
			{
				Console.WriteLine();
				Console.WriteLine("-------------------------------------");
				Console.WriteLine($"{assetType}");
				Console.WriteLine("-------------------------------------");
				Console.WriteLine();

				for (int i = 0; i < count; i++)
				{
					var assetId = new AssetIdentifier(assetType, start + i);

					var text = assetProvider.TextLoader.LoadText(assetId);
					var name = text.GetString();

					Console.WriteLine(name.Length != 0 ? name : $"{assetType} {start + i}");
				}
			}

			PrintTexts(AssetType.ClassName, 11);
			PrintTexts(AssetType.SkillName, 10);
			PrintTexts(AssetType.CharInfoText, 5);
			PrintTexts(AssetType.RaceName, 7);
			PrintTexts(AssetType.ConditionName, 16, 1);
			PrintTexts(AssetType.ItemTypeName, 19);
			PrintTexts(AssetType.SpellSchoolName, 7, 1);
			PrintTexts(AssetType.SpellName, 7*30, 1);
		}
	}
}
