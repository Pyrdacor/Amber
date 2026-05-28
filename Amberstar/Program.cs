using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Amber.Assets.Common;
using Amber.Common;
using Amber.IO.FileSystem;
using Amberstar.GameData;
using Amberstar.GameData.Events;
using Amberstar.GameData.Legacy;
using Amberstar.GameData.Serialization;

#pragma warning disable CA1416 // Validate platform compatibility

namespace Amberstar
{
	internal class Program
	{
		static void Main(string[] args)
		{
            //string basePath = @"D:\Projects\Amber\English\AmberfilesST";
            string basePath = @"D:\Projects\Amber\German\AmberfilesST";

			var fileSystem = FileSystem.FromOperatingSystemPath(basePath);

			var assetProvider = new AssetProvider(fileSystem.AsReadOnly());

			void WriteTexts(AssetType assetType, int count, int start = 0, bool skipEmpty = false, bool textBlocks = false)
			{
				string folder = $@"{basePath}\{assetType}s";
				Directory.CreateDirectory(folder);

				for (int i = 0; i < count; i++)
				{
					var assetId = new AssetIdentifier(assetType, start + i);
					var text = assetProvider.TextLoader.LoadText(assetId);

					if (textBlocks)
					{
						if (skipEmpty && (text.TextBlockCount == 0 || string.IsNullOrWhiteSpace(text.GetString())))
							continue;

						var subFolder = count == 1 ? folder : Path.Combine(folder, $"{start + i:000}");

						Directory.CreateDirectory(subFolder);

						for (int b = 0; b < text.TextBlockCount; b++)
						{
							var content = string.Join('\n', text.GetTextBlock(b).GetLines(int.MaxValue));
							File.WriteAllText(Path.Combine(subFolder, $"{b:000}.txt"), content, System.Text.Encoding.UTF8);
						}
					}
					else
					{
						var content = string.Join('\n', text.GetLines(int.MaxValue));

						if (skipEmpty && string.IsNullOrWhiteSpace(content))
							continue;

						File.WriteAllText(Path.Combine(folder, $"{start + i:000}.txt"), content, System.Text.Encoding.UTF8);
					}
				}
			}

			WriteTexts(AssetType.ClassName, 11);
            WriteTexts(AssetType.RaceName, 15);
            WriteTexts(AssetType.AttributeName, 9);
            WriteTexts(AssetType.SkillName, 10);
			WriteTexts(AssetType.CharInfoText, 5);
			WriteTexts(AssetType.LanguageName, 7);
			WriteTexts(AssetType.ConditionName, 16, 1);
			WriteTexts(AssetType.ItemTypeName, 19);
            WriteTexts(AssetType.SpellSchoolName, 7, 1);
			WriteTexts(AssetType.SpellName, 7*30, 1);
			WriteTexts(AssetType.MapText, 152, 1, true, true);
			WriteTexts(AssetType.PuzzleText, 1, 1, false, true);
			WriteTexts(AssetType.ItemText, 2, 1, false, true);
            WriteTexts(AssetType.UIText, Enum.GetValues<UIText>().Length);
            WriteTexts(AssetType.Message, 202);

            byte[] uiPalette = assetProvider.PaletteLoader.LoadBuiltinPalette(BuiltinPalette.UI).GetData();
            byte[] itemPalette = assetProvider.PaletteLoader.LoadBuiltinPalette(BuiltinPalette.Item).GetData();

            for (int i = 1; i <= 11; i++)
			{
				var layout = assetProvider.LayoutLoader.LoadLayout(i);
				WriteGraphic($@"{basePath}\Layout\{i:000}.png", layout, uiPalette, false);
			}

			WriteGraphic($@"{basePath}\Layout\PortraitArea.png", assetProvider.LayoutLoader.LoadPortraitArea(), uiPalette, false);

			for (int i = 0; i <= (int)UIGraphic.LastUIGraphic; i++)
			{
				var graphic = (UIGraphic)i;
				WriteGraphic($@"{basePath}\UIGraphics\{graphic}.png", assetProvider.UIGraphicLoader.LoadGraphic(graphic), uiPalette, false);
			}

			for (int i = 0; i <= (int)ButtonType.LastButton; i++)
			{
				var button = (ButtonType)i;
				WriteGraphic($@"{basePath}\Buttons\{button}.png", assetProvider.UIGraphicLoader.LoadButtonGraphic(button), uiPalette, false);
			}

			for (int i = 0; i <= (int)StatusIcon.LastStatusIcon; i++)
			{
				var statusIcon = (StatusIcon)i;
				WriteGraphic($@"{basePath}\StatusIcons\{statusIcon}.png", assetProvider.UIGraphicLoader.LoadStatusIcon(statusIcon), uiPalette, false);
			}

			for (int i = 1; i <= (int)Image80x80.LastImage; i++)
			{
				var image = (Image80x80)i;
				var graphic = assetProvider.GraphicLoader.Load80x80Graphic(image);
				WriteGraphic($@"{basePath}\80x80Images\{i:000}.png", graphic, graphic.Palette.GetData(), false);
			}

			var backgrounds = assetProvider.GraphicLoader.LoadAllBackgroundGraphics();
			foreach (var background in backgrounds)
			{
				WriteGraphic($@"{basePath}\Backgrounds\{background.Key:000}.png", background.Value, uiPalette, false);
			}

			ITileset[] tilesets = [assetProvider.TilesetLoader.LoadTileset(1), assetProvider.TilesetLoader.LoadTileset(2)];
			for (int i = 1; i <= 2; i++)
			{
				var tileset = tilesets[i - 1];
				var tilesetGraphics = tileset.Graphics;
				var palette = tileset.Palette;
				int tileIndex = 1;

                foreach (var tilesetGraphic in tilesetGraphics)
				{
					WriteGraphic($@"{basePath}\Tilesets\{i:000}\{tileIndex++:000}.png", tilesetGraphic, palette.GetData(), false);
				}
			}

            var labBlocks = assetProvider.LabDataLoader.LoadAllLabBlocks();

			foreach (var labBlock in labBlocks)
			{
				for (int i = 0; i < labBlock.Value.Perspectives.Length; i++)
					WriteGraphic($@"{basePath}\LabBlocks\{labBlock.Key:000}\Perspective{i:000}.png", labBlock.Value.Perspectives[i].Frames.ToGraphic(), uiPalette, true);
			}

            for (int i = 0; i <= (int)ItemGraphic.LastItemGraphic; i++)
			{
                var graphic = (ItemGraphic)i;
                WriteGraphic($@"{basePath}\Items\{graphic}.png", assetProvider.GraphicLoader.LoadItemGraphic(graphic), itemPalette, true);
            }

			WriteMapOverviews(basePath, @"D:\Projects\Ambermoon\Graphics\AutomapGfx", assetProvider);
        }

		static void WriteMapOverviews(string basePath, string automapGraphicFolder, AssetProvider assetProvider)
		{
			Bitmap upperLeftCornerImage = Bitmap.FromFile(Path.Combine(automapGraphicFolder, "001.png")) as Bitmap;
            Bitmap upperRightCornerImage = Bitmap.FromFile(Path.Combine(automapGraphicFolder, "002.png")) as Bitmap;
            Bitmap lowerLeftCornerImage = Bitmap.FromFile(Path.Combine(automapGraphicFolder, "003.png")) as Bitmap;
            Bitmap lowerRightCornerImage = Bitmap.FromFile(Path.Combine(automapGraphicFolder, "004.png")) as Bitmap;

            Bitmap[] upperParts = Enumerable.Range(5, 4).Select(index => Bitmap.FromFile(Path.Combine(automapGraphicFolder, $"{index:000}.png")) as Bitmap).ToArray();
            Bitmap[] lowerParts = Enumerable.Range(11, 4).Select(index => Bitmap.FromFile(Path.Combine(automapGraphicFolder, $"{index:000}.png")) as Bitmap).ToArray();
            Bitmap[] leftParts = Enumerable.Range(15, 2).Select(index => Bitmap.FromFile(Path.Combine(automapGraphicFolder, $"{index:000}.png")) as Bitmap).ToArray();
            Bitmap[] rightParts = Enumerable.Range(9, 2).Select(index => Bitmap.FromFile(Path.Combine(automapGraphicFolder, $"{index:000}.png")) as Bitmap).ToArray();

			Dictionary<int, Bitmap> mapIcons = [];

			Bitmap GetMapIcon(int index)
			{
                if (mapIcons.TryGetValue(index, out Bitmap? icon))
					return icon;

				icon = Bitmap.FromFile(Path.Combine(automapGraphicFolder, $"{index:000}.png")) as Bitmap;

				mapIcons[index] = icon!;

				return icon!;
            }

            const int free = -1;
            const int wall = 0;
            const int place = 1;
            const int door = 2;
            const int chest = 3;
            const int text = 4;
            const int riddle = 5;
            const int teleporter = 6;
            const int exit = 7;
            const int spinner = 8;
            const int trap = 9;
            const int trapDoor = 10;
			const int wallText = 11;
            const int doorOpen = 12;

            int[] iconMappings = [37, 36, 34, 38, -1, 27, 28, 39, 29, 30, 31, -1, 35];

            var mapIndices = assetProvider.GetAssetKeys(AssetType.Map);

			Directory.CreateDirectory($@"{basePath}\Automaps");

			foreach (var mapIndex in mapIndices)
			{
				AssembleAutomapGraphic(mapIndex, $@"{basePath}\Automaps\{mapIndex:000}.png");
            }

            void AssembleAutomapGraphic(int mapIndex, string outputFilename)
			{
				var map = assetProvider.MapLoader.LoadMap(mapIndex);

				if (map.Type == MapType.Map2D)
					return;

				int width = 64 + map.Width * 16;
				int height = 64 + 16 + map.Height * 16;

				if (height % 32 == 16)
					height += 16;

                using var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				using var graphics = Graphics.FromImage(bitmap);
				using var wallBrush = new SolidBrush(System.Drawing.Color.FromArgb(0x88, 0x55, 0x11));
				using var fillBrush = new SolidBrush(System.Drawing.Color.FromArgb(0xaa, 0x77, 0x44));
                using var font = new Font(FontFamily.GenericMonospace, 18, FontStyle.Bold);
                using var smallFont = new Font(FontFamily.GenericMonospace, 10, FontStyle.Regular);

                int[,] tiles = new int[map.Width, map.Height];
                string?[,] tileInfos = new string?[map.Width, map.Height];

                var map3d = (map as IMap3D)!;
				var labdata = assetProvider.LabDataLoader.LoadLabData(map3d.LabDataIndex);

                string GetMapText(int mapIndex, int index)
                {
                    var text = assetProvider.TextLoader.LoadText(new(AssetType.MapText, mapIndex));
                    return text.GetTextBlock(index).GetLines(32).First();
                }

                for (int y = 0; y < map.Height; y++)
				{
					for (int x = 0; x < map.Width; x++)
					{
						var tile = map3d.Tiles[y * map.Width + x];
						bool typeSet = false;

						if (tile.Event != 0)
						{
							typeSet = true;
							var @event = map.Events[tile.Event - 1];

							switch (@event.Type)
							{
								case EventType.ShowPictureText:
									tiles[x, y] = text;
									tileInfos[x, y] = GetMapText(map.Index, (@event as IShowPictureTextEvent)!.TextIndex);
									typeSet = false;
									break;
								case EventType.RiddleMouth:
									tiles[x, y] = riddle;
									break;
								case EventType.Teleporter:
									tiles[x, y] = teleporter;
									break;
								case EventType.MapExit:
								case EventType.DoorExit:
								case EventType.TravelExit:
									tiles[x, y] = exit;
									break;
								case EventType.Spinner:
									tiles[x, y] = spinner;
									break;
								case EventType.ExecuteTrap:
								case EventType.DamageField:
									tiles[x, y] = trap;
									break;
								case EventType.TrapDoor:
									tiles[x, y] = trapDoor;
									break;
								case EventType.Door:
									tiles[x, y] = door;
									break;
								case EventType.Chest:
									tiles[x, y] = chest;
									break;
								case EventType.Place:
									tiles[x, y] = place;
									var placeEvent = (@event as IPlaceEvent)!;
									tileInfos[x, y] = $"{(int)(byte)placeEvent.PlaceType},{assetProvider.PlaceLoader.LoadPlace(placeEvent.PlaceType, placeEvent.PlaceIndex).Name}";
									break;
								default:
									typeSet = false;
									break;
							}
						}

						if (!typeSet)
						{
                            var labTile = map3d.LabTiles[tile.LabTileIndex - 1];

                            var primary = labdata.LabBlocks[labTile.PrimaryLabBlockIndex - 1];
                            bool isWall;

                            if (primary.Type == LabBlockType.Overlay && labTile.SecondaryLabBlockIndex != 0)
                            {
                                isWall = labdata.LabBlocks[labTile.SecondaryLabBlockIndex - 1].Type == LabBlockType.Wall;
                            }
                            else
                            {
                                isWall = primary.Type == LabBlockType.Wall;
                            }

                            if (isWall)
							{
								if (tiles[x, y] != text)
								{
									if (!labTile.Flags.HasFlag(LabTileFlags.BlockAllMovement) &&
										labTile.Flags.HasFlag(LabTileFlags.AllowWalk1))
                                        tiles[x, y] = doorOpen;
                                    else
										tiles[x, y] = wall;
								}
								else
									tiles[x, y] = wallText;
                            }
							else if (tiles[x, y] != text)
								tiles[x, y] = free;
						}
					}
				}

                void RenderMapBackground(Graphics graphics, int width, int height)
                {
                    // Draw corners
                    graphics.DrawImageUnscaled(upperLeftCornerImage, 0, 0);
                    graphics.DrawImageUnscaled(upperRightCornerImage, width - upperRightCornerImage.Width, 0);
                    graphics.DrawImageUnscaled(lowerLeftCornerImage, 0, height - lowerLeftCornerImage.Height);
                    graphics.DrawImageUnscaled(lowerRightCornerImage, width - lowerRightCornerImage.Width, height - lowerRightCornerImage.Height);

                    // Draw upper and lower border
                    for (int x = 0; x < map.Width; x++)
                    {
                        var upper = upperParts[x % 4];
                        var lower = lowerParts[x % 4];

                        graphics.DrawImageUnscaled(upper, 32 + x * 16, 0);
                        graphics.DrawImageUnscaled(lower, 32 + x * 16, height - 32);
                    }

                    // Draw left and right border
                    int loopHeight = (height - 64) / 32;

                    for (int y = 0; y < loopHeight; y++)
                    {
                        var left = leftParts[y % 2];
                        var right = rightParts[y % 2];

                        graphics.DrawImageUnscaled(left, 0, 32 + y * 32);
                        graphics.DrawImageUnscaled(right, width - 32, 32 + y * 32);
                    }

                    // Fill
                    graphics.FillRectangle(fillBrush, new(32, 32, width - 64, height - 64));
                }

				RenderMapBackground(graphics, bitmap.Width, bitmap.Height);

				var legend = new Stack<string>();
				var textDrawActions = new Queue<Action>();

				// Place walls and icons
                for (int y = 0; y < map.Height; y++)
				{
					for (int x = 0; x < map.Width; x++)
					{
						int type = tiles[x, y];

						if (type == free)
							continue;

						int imageIndex = -1;
                        string? text = null;

						if (type != wall)
						{
                            if (type == place)
                            {
								var placeInfos = tileInfos[x, y]!.Split(',');
                                var placeType = int.Parse(placeInfos[0]);
								text = placeInfos[1];

                                if (placeType == (byte)PlaceType.Inn)
                                    type--;
                            }

                            imageIndex = iconMappings[type];

							if (imageIndex == -1) // text
							{
								text = tileInfos[x, y]!;
                            }

							if (text != null)
							{
                                char letter = (char)('A' + legend.Count);
                                legend.Push(text);

								var drawX = 32 + x * 16 + 1.5f;
								var drawY = 48 + y * 16;

                                textDrawActions.Enqueue(() => graphics.DrawString(letter.ToString(), smallFont, Brushes.White, drawX, drawY));
							}
                        }

                        int lx = 32 + x * 16;
                        int ly = 48 + y * 16;
						const int wallSize = 3;

                        if (imageIndex == -1)
						{
							bool IsWall(int x, int y) => tiles[x, y] == wall || tiles[x, y] == wallText;

                            // Draw wall
                            bool wallUpLeft = x > 0 && y > 0 && IsWall(x - 1, y - 1);
                            bool wallUp = y > 0 && IsWall(x, y - 1);
                            bool wallUpRight = x < map.Width - 1 && y > 0 && IsWall(x + 1, y - 1);
                            bool wallDownLeft = x > 0 && y < map.Height - 1 && IsWall(x - 1, y + 1);
                            bool wallDown = y < map.Height - 1 && IsWall(x, y + 1);
                            bool wallDownRight = x < map.Width - 1 && y < map.Height - 1 && IsWall(x + 1, y + 1);
                            bool wallLeft = x > 0 && IsWall(x - 1, y);
                            bool wallRight = x < map.Width - 1 && IsWall(x + 1, y);

                            if (!wallUp)
							{
								graphics.FillRectangle(wallBrush, lx, ly, 16, wallSize);
							}

                            if (!wallDown)
                            {
                                graphics.FillRectangle(wallBrush, lx, ly + 16 - wallSize, 16, wallSize);
                            }

                            if (!wallLeft)
                            {
                                graphics.FillRectangle(wallBrush, lx, ly, wallSize, 16);
                            }

                            if (!wallRight)
                            {
                                graphics.FillRectangle(wallBrush, lx + 16 - wallSize, ly, wallSize, 16);
                            }

							if (!wallUpLeft)
                            {
                                graphics.FillRectangle(wallBrush, lx, ly, wallSize, wallSize);
                            }

                            if (!wallUpRight)
                            {
                                graphics.FillRectangle(wallBrush, lx + 16 - wallSize, ly, wallSize, wallSize);
                            }

                            if (!wallDownLeft)
                            {
                                graphics.FillRectangle(wallBrush, lx, ly + 16 - wallSize, wallSize, wallSize);
                            }

                            if (!wallDownRight)
                            {
                                graphics.FillRectangle(wallBrush, lx + 16 - wallSize, ly + 16 - wallSize, wallSize, wallSize);
                            }
                        }
						else
						{
                            var icon = GetMapIcon(imageIndex);
							var dest = new Rectangle(32 + x * 16, 48 + y * 16 - 16, 32, 32);

                            graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                            graphics.DrawImage(icon, dest, new RectangleF(-0.25f, -0.25f, 15.75f, 15.75f), GraphicsUnit.Pixel);
                        }
					}
				}

                // Add map name
                float nameWidth = graphics.MeasureString(map.Name, font).Width;
                graphics.DrawString(map.Name, font, Brushes.Black, new PointF(0.5f * (bitmap.Width - nameWidth), 30));

				// Legend
				while (textDrawActions.Count != 0)
				{
					textDrawActions.Dequeue().Invoke();
                }

				bool legend2Columns = bitmap.Width >= 624;
                int legendBitmapHeight = legend2Columns
					? 64 + 8 + ((legend.Count + 1) / 2) * 12
					: 64 + 8 + legend.Count * 12;

				if ((legendBitmapHeight - 64) % 32 != 0)
					legendBitmapHeight += (32 - (legendBitmapHeight - 64) % 32);

                using var legendBitmap = new Bitmap(bitmap.Width, legendBitmapHeight);

				{
					using var legendGraphics = Graphics.FromImage(legendBitmap);

					RenderMapBackground(legendGraphics, legendBitmap.Width, legendBitmap.Height);

					char legendLetter = (char)('A' + legend.Count - 1);
					const int firstColumnX = 32;
					int secondColumnX = legendBitmap.Width / 2 + 8;
                    int textX = legend2Columns && legend.Count % 2 == 0 ? secondColumnX : firstColumnX;
					int textY = legendBitmap.Height - 32 - 12;

					while (legend.Count != 0)
					{
						string text = legend.Pop();

                        legendGraphics.DrawString($"{legendLetter}: {text}", smallFont, Brushes.White, textX, textY);
						legendLetter--;

						if (textX == firstColumnX)
						{
							textY -= 12;
							textX = legend2Columns ? secondColumnX : firstColumnX;
						}
						else
						{
							textX = firstColumnX;
						}
					}
				}

				using var compoundBitmap = new Bitmap(bitmap.Width, bitmap.Height + legendBitmap.Height);
				using var compoundGraphics = Graphics.FromImage(compoundBitmap);

				compoundGraphics.DrawImageUnscaled(bitmap, 0, 0);
                compoundGraphics.DrawImageUnscaled(legendBitmap, 0, bitmap.Height);

                compoundBitmap.Save(outputFilename);
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
