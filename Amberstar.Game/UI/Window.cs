using Amber.Assets.Common;
using Amber.Common;
using Amber.Renderer;

namespace Amberstar.Game.UI
{
	internal class Window
	{
		public const int TileWidth = 16;
		public const int TileHeight = 16;
		readonly ISprite[] borders;
		readonly IColoredRect fill;

		public Rect ClientArea { get; }

		public Window(Game game, int x, int y, int widthInTiles, int heightInTiles, bool dark, byte displayLayer, byte? paletteIndex = null)
		{
			if (widthInTiles < 3)
				widthInTiles = 3;
			if (heightInTiles < 3)
				heightInTiles = 3;

			ClientArea = new(x + TileWidth, y + TileHeight, (widthInTiles - 2) * TileWidth, (heightInTiles - 2) * TileHeight);
			borders = new ISprite[2 * widthInTiles + 2 * heightInTiles - 4];

			paletteIndex ??= game.PaletteIndexProvider.UIPaletteIndex;
			int windowColorIndex = dark ? 3 : 2;
			var windowColor = game.PaletteColorProvider.GetPaletteColor(paletteIndex.Value, windowColorIndex);
			int baseImageIndex = game.GraphicIndexProvider.GetWindowGraphicIndex(dark);
			var layer = game.GetRenderLayer(Layer.UI);
			var textureAtlas = layer.Config.Texture!;
			var textureOffset = textureAtlas.GetOffset(baseImageIndex);			
			int borderIndex = 0;
			int imageIndex = 0;
			int lastRowX = x + (widthInTiles - 1) * TileWidth;
			int lastRowY = y + (heightInTiles - 1) * TileHeight;

			// upper left corner
			borders[borderIndex++] = CreateBorder(x, y, imageIndex++);
			// left border
			borders[borderIndex++] = CreateBorder(x, y + TileHeight, imageIndex++);
			for (int i = 0; i < heightInTiles - 4; i++)
				borders[borderIndex++] = CreateBorder(x, y + TileHeight + (i + 1) * TileHeight, imageIndex);
			imageIndex++;
			borders[borderIndex++] = CreateBorder(x, lastRowY - TileHeight, imageIndex++);
			// lower left corner
			borders[borderIndex++] = CreateBorder(x, lastRowY, imageIndex++);

			// second column upper border
			borders[borderIndex++] = CreateBorder(x + TileWidth, y, imageIndex++);
			// second column lower border
			borders[borderIndex++] = CreateBorder(x + TileWidth, lastRowY, imageIndex++);

			// middle upper borders
			for (int i = 0; i < widthInTiles - 4; i++)
				borders[borderIndex++] = CreateBorder(x + (2 + i) * TileWidth, y, imageIndex);
			imageIndex++;
			// middle lower borders
			for (int i = 0; i < widthInTiles - 4; i++)
				borders[borderIndex++] = CreateBorder(x + (2 + i) * TileWidth, lastRowY, imageIndex);
			imageIndex++;

			// second last column upper border
			borders[borderIndex++] = CreateBorder(lastRowX - TileWidth, y, imageIndex++);
			// second last column lower border
			borders[borderIndex++] = CreateBorder(lastRowX - TileWidth, lastRowY, imageIndex++);

			// upper right corner
			borders[borderIndex++] = CreateBorder(lastRowX, y, imageIndex++);
			// right border
			borders[borderIndex++] = CreateBorder(lastRowX, y + TileHeight, imageIndex++);
			for (int i = 0; i < heightInTiles - 4; i++)
				borders[borderIndex++] = CreateBorder(lastRowX, y + TileHeight + (i + 1) * TileHeight, imageIndex);
			imageIndex++;
			borders[borderIndex++] = CreateBorder(lastRowX, lastRowY - TileHeight, imageIndex++);
			// lower right corner
			borders[borderIndex++] = CreateBorder(lastRowX, lastRowY, imageIndex++);

			// fill area
			fill = layer.ColoredRectFactory!.Create();
			fill.Color = windowColor;
			fill.DisplayLayer = displayLayer;
			fill.Position = ClientArea.Position;
			fill.Size = ClientArea.Size;
			fill.Visible = true;

			ISprite CreateBorder(int x, int y, int relativeImageIndex)
			{
				var sprite = layer.SpriteFactory!.Create();
				sprite.TextureOffset = new(textureOffset.X + relativeImageIndex * TileWidth, textureOffset.Y);
				sprite.Position = new(x, y);
				sprite.DisplayLayer = displayLayer;
				sprite.PaletteIndex = paletteIndex.Value;
				sprite.Size = new(TileWidth, TileHeight);
				sprite.Visible = true;

				return sprite;
			}
		}

		public void Destroy()
		{
			foreach (var border in borders)
				border.Visible = false;

			fill.Visible = false;
		}
	}
}
