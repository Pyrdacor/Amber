using Amber.Assets.Common;
using Amber.Common;
using Amber.IO.FileSystem;
using Amberstar.GameData.Atari;
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

			byte[] testPal =
			[
				0x00, 0x00,
				0x07, 0x50,
				0x03, 0x33,
				0x02, 0x22,
				0x01, 0x11,
				0x07, 0x42,
				0x06, 0x31,
				0x02, 0x00,
				0x05, 0x66,
				0x03, 0x45,
				0x07, 0x54,
				0x06, 0x43,
				0x05, 0x32,
				0x04, 0x21,
				0x03, 0x10,
				0x07, 0x65
			];

			for (int i = 1; i <= 11; i++)
			{
				var layout = assetProvider.LayoutLoader.LoadLayout(i);
				WriteGraphic($@"D:\Projects\Amber\German\AmberfilesST\Layout\{i:000}.png", layout, testPal, false);
			}

			WriteGraphic($@"D:\Projects\Amber\German\AmberfilesST\Layout\PortraitArea.png", assetProvider.LayoutLoader.LoadPortraitArea(), testPal, false);

			for (int i = 0; i < (int)UIGraphic.LastUIGraphic; i++)
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
		}

		static void WriteGraphic(string filename, IGraphic graphic, byte[] palette, bool transparency)
		{
			var dir = Path.GetDirectoryName(filename);

			Directory.CreateDirectory(dir);

			using var bitmap = new Bitmap(graphic.Width, graphic.Height);
			var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
				System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			byte[] index0Color = [0, 0, 0, (byte)(transparency ?  0 : 0xff)];

			byte[] PalIndexToColor(int index)
			{
				if (index == 0)
					return index0Color;

				var r = palette[index * 2 + 0] & 0xf;
				var gb = palette[index * 2 + 1];
				var g = gb >> 4;
				var b = gb & 0xf;

				return [ (byte)(r | (r << 4)), (byte)(g | (g << 4)), (byte)(b | (b << 4)), 0xff ];
			}

			var pixels = graphic.GetPixelData().SelectMany(paletteIndex => PalIndexToColor(paletteIndex)).ToArray();

			Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);

			bitmap.UnlockBits(data);
			bitmap.Save(filename);
		}
	}
}
