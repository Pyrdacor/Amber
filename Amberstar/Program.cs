using Amber.Assets.Common;
using Amber.Common;
using Amber.IO.FileSystem;
using Amberstar.GameData.Legacy;
using Amberstar.GameData.Serialization;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Amberstar
{
	internal class Program
	{
		static void Main(string[] args)
		{
			var fileSystem = FileSystem.FromOperatingSystemPath(@"D:\Projects\Amber\German\AmberfilesST");

			var assetProvider = new AssetProvider(fileSystem.AsReadOnly());

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

			byte[] testPal = assetProvider.PaletteLoader.LoadUIPalette().GetData();

			for (int i = 1; i <= 11; i++)
			{
				var layout = assetProvider.LayoutLoader.LoadLayout(i);
				WriteGraphic($@"D:\Projects\Amber\German\AmberfilesST\Layout\{i:000}.png", layout, testPal, false);
			}

			WriteGraphic($@"D:\Projects\Amber\German\AmberfilesST\Layout\PortraitArea.png", assetProvider.LayoutLoader.LoadPortraitArea(), testPal, false);

			for (int i = 0; i <= (int)UIGraphic.LastUIGraphic; i++)
			{
				var graphic = (UIGraphic)i;
				WriteGraphic($@"D:\Projects\Amber\German\AmberfilesST\UIGraphics\{graphic}.png", assetProvider.UIGraphicLoader.LoadGraphic(graphic), testPal, false);
			}

			for (int i = 0; i <= (int)Button.LastButton; i++)
			{
				var button = (Button)i;
				WriteGraphic($@"D:\Projects\Amber\German\AmberfilesST\Buttons\{button}.png", assetProvider.UIGraphicLoader.LoadButtonGraphic(button), testPal, false);
			}

			for (int i = 0; i <= (int)StatusIcon.LastStatusIcon; i++)
			{
				var statusIcon = (StatusIcon)i;
				WriteGraphic($@"D:\Projects\Amber\German\AmberfilesST\StatusIcons\{statusIcon}.png", assetProvider.UIGraphicLoader.LoadStatusIcon(statusIcon), testPal, false);
			}

			for (int i = 1; i <= (int)Image80x80.LastImage; i++)
			{
				var image = (Image80x80)i;
				var graphic = assetProvider.GraphicLoader.Load80x80Graphic(image);
				WriteGraphic($@"D:\Projects\Amber\German\AmberfilesST\80x80Images\{i:000}.png", graphic, graphic.Palette.GetData(), false);
			}
		}

		static void WriteGraphic(string filename, IGraphic graphic, byte[] palette, bool transparency)
		{
			var dir = Path.GetDirectoryName(filename);

			Directory.CreateDirectory(dir);

			using var bitmap = new Bitmap(graphic.Width, graphic.Height);
			var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
				System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			byte[] index0Color = [0, 0, 0, (byte)(transparency ?  0 : 0xff)];

			// Out: B G R A
			//  In: R G B A
			byte[] PalIndexToColor(int index)
			{
				if (index == 0)
					return index0Color;

				return [ palette[index * 4 + 2], palette[index * 4 + 1], palette[index * 4 + 0], 0xff];
			}

			var pixels = graphic.GetData().SelectMany(paletteIndex => PalIndexToColor(paletteIndex)).ToArray();

			Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);

			bitmap.UnlockBits(data);
			bitmap.Save(filename);
		}
	}
}
