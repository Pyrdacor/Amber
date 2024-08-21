using Amber.Renderer;
using Amberstar.GameData;

namespace Amberstar.Game.Screens
{
	internal class Map2DScreen : Screen
	{
		const int TilesPerRow = 11;
		const int TileRows = 9;
		const int TileWidth = 16;
		const int TileHeight = 16;
		const int OffsetX = 16;
		const int OffsetY = 49;
		Game? game;
		IMap2D? map;
		ITileset[]? tilesets;
		readonly List<IAnimatedSprite> underlay = [];
		readonly List<IAnimatedSprite> overlay = [];
		ISprite? player;
		int lastScrollX = -1;
		int lastScrollY = -1;
		int tileGraphicOffset = 0;

		public override ScreenType Type { get; } = ScreenType.Map2D;

		public override void Init(Game game)
		{
			this.game = game;
			tilesets = [game.AssetProvider.TilesetLoader.LoadTileset(1), game.AssetProvider.TilesetLoader.LoadTileset(2)];
		}

		public override void Open(Game game)
		{
			game.SetLayout(Layout.Map2D);
			LoadMap(65); // TODO
			InitPlayer(3, 3); // TODO
		}

		public override void Close(Game game)
		{
			ClearMap();
			player!.Visible = false;
			player = null;
		}

		public override void Render(Game game, double delta)
		{
			// TODO
		}

		public override void Update(Game game, double delta)
		{
			// TODO
		}

		private void FillMap(int scrollOffsetX, int scrollOffsetY)
		{
			if (scrollOffsetX < 0)
				scrollOffsetX = 0;
			else if (scrollOffsetX + TilesPerRow > map!.Width)
				scrollOffsetX = map!.Width - TilesPerRow;
			if (scrollOffsetY < 0)
				scrollOffsetY = 0;
			else if (scrollOffsetY + TileRows > map!.Height)
				scrollOffsetY = map.Height - TileRows;

			if (scrollOffsetX == lastScrollX && scrollOffsetY == lastScrollY)
				return; // nothing to do

			lastScrollX = scrollOffsetX;
			lastScrollY = scrollOffsetY;
			int tilesetIndex = map!.TilesetIndex;

			ClearMap();

			for (int y = 0; y < TileRows; y++)
			{
				for (int x = 0; x < TilesPerRow; x++)
				{
					int index = x + y * map!.Width;
					var tile = map.Tiles[index];

					if (tile.Underlay != 0)
						underlay.Add(CreateTileSprite(OffsetX + x * TileWidth, OffsetY + y * TileHeight, tile.Underlay, tilesetIndex, Layer.MapUnderlay));

					if (tile.Overlay != 0)
						overlay.Add(CreateTileSprite(OffsetX + x * TileWidth, OffsetY + y * TileHeight, tile.Overlay, tilesetIndex, Layer.MapOverlay));
				}
			}
		}

		private void InitPlayer(int x, int y)
		{

		}

		private void ClearMap()
		{
			underlay.ForEach(tile => tile.Visible = false);
			underlay.Clear();
			overlay.ForEach(tile => tile.Visible = false);
			overlay.Clear();
		}

		private IAnimatedSprite CreateTileSprite(int x, int y, int index, int tilesetIndex, Layer layer)
		{
			var renderLayer = game!.Renderer.Layers[(int)layer];
			var tileset = tilesets![tilesetIndex - 1];
			var tileInfo = tileset!.Tiles[index - 1];
			var tileSprite = renderLayer.SpriteFactory!.CreateAnimated();

			tileSprite.FrameCount = Math.Max(1, tileInfo.FrameCount);
			tileSprite.Position = new(x, y);
			tileSprite.Size = new(TileWidth, TileHeight);
			tileSprite.TextureOffset = renderLayer.Config.Texture!.GetOffset(tileGraphicOffset + tileInfo.ImageIndex);
			tileSprite.PaletteIndex = game.PaletteIndexProvider.GetTilesetPaletteIndex(tilesetIndex);
			tileSprite.Visible = true;

			return tileSprite;
		}

		private void LoadMap(int index)
		{
			lastScrollX = -1;
			lastScrollY = -1;
			map = game!.AssetProvider.MapLoader.LoadMap(index) as IMap2D; // TODO: catch exceptions
			tileGraphicOffset = map!.TilesetIndex == 1 ? 0 : tilesets![0].Graphics.Count + 1;
			FillMap(0, 0); // TODO
		}
	}
}
