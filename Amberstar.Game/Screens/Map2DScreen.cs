using Amber.Common;
using Amber.Renderer;
using Amberstar.Game.Events;
using Amberstar.GameData;

namespace Amberstar.Game.Screens
{
	internal class Map2DScreen : Screen
	{
		enum ButtonLayout
		{
			Movement,
			Actions
		}

		static readonly Dictionary<TravelType, int> TicksPerStep = new()
		{
			// TODO
			{ TravelType.Walk, 20 },
			{ TravelType.Horse, 10 },
			{ TravelType.Raft, 10 },
			{ TravelType.Ship, 8 },
			{ TravelType.MagicDisc, 10 },
			{ TravelType.Eagle, 5 },
			{ TravelType.SuperChicken, 2 },
		};

		const int TilesPerRow = 11;
		const int TileRows = 9;
		const int TileWidth = 16;
		const int TileHeight = 16;
		const int OffsetX = 16;
		const int OffsetY = 49;
		const int RenderOrderOffset = TileHeight / 4;
		const int MinScrollX = TilesPerRow / 2;
		const int MinScrollY = TileRows / 2 + 1;
		Game? game;
		IMap2D? map;
		ITileset[]? tilesets;
		readonly Dictionary<int, IAnimatedSprite> underlay = [];
		readonly Dictionary<int, IAnimatedSprite> overlay = [];
		ISprite? player;
		int lastScrollX = -1;
		int lastScrollY = -1;
		int tileGraphicOffset = 0;
		ButtonLayout buttonLayout = ButtonLayout.Movement;
		int moveX = 0;
		int moveY = 0;
		long moveTickCounter = 0;

		public override ScreenType Type { get; } = ScreenType.Map2D;

		internal void MapChanged()
		{
			LoadMap(game!.State.MapIndex);
			AfterMove();
		}

		public override void Init(Game game)
		{
			this.game = game;
			tilesets = [game.AssetProvider.TilesetLoader.LoadTileset(1), game.AssetProvider.TilesetLoader.LoadTileset(2)];
		}

		public override void Open(Game game)
		{
			game.SetLayout(Layout.Map2D);
			buttonLayout = ButtonLayout.Movement;
			LoadMap(game.State.MapIndex);
			InitPlayer();
			AfterMove();
		}

		public override void Close(Game game)
		{
			ClearMap();
			player!.Visible = false;
			player = null;
		}

		public override void Render(Game game)
		{
			// TODO
		}

		public override void Update(Game game, long elapsedTicks)
		{
			if (elapsedTicks == 0)
				return;

			if (moveX != 0 || moveY != 0)
			{
				moveTickCounter += elapsedTicks;

				var ticksPerStep = TicksPerStep[game.State.TravelType];

				if (ticksPerStep > 0 && moveTickCounter >= ticksPerStep)
				{
					bool moved = false;

					while (moveTickCounter >= ticksPerStep)
					{
						if (MovePlayer(ref moveX, ref moveY))
						{
							moved = true;
							moveTickCounter -= ticksPerStep;
						}
						else
						{
							moveTickCounter = 0;
							break;
						}
					}

					if (moved)
						AfterMove();
				}
			}
			else
			{
				moveTickCounter = 0;
			}
		}

		private bool MovePlayer(ref int x, ref int y)
		{
			// TODO: collision detection
			var oldPosition = game!.State.PartyPosition;
			int newX = MathUtil.Limit(0, oldPosition.X + x, map!.Width - 1);
			int newY = MathUtil.Limit(0, oldPosition.Y + y, map.Height - 1);

			bool TileBlocksMovement(int x, int y)
			{
				// Some events like teleports seem to allow movement. (TODO: any other events?)
				var eventIndex = map!.Tiles[x + y * map.Width].Event;

				if (eventIndex != 0)
				{
					var @event = map.Events[eventIndex - 1];

					// TODO: we should check if the event is still active
					if (@event.Type == EventType.MapExit ||
						@event.Type == EventType.Teleporter ||
						@event.Type == EventType.TrapDoor ||
						@event.Type == EventType.TravelExit ||
						(@event.Type == EventType.WindGate && game.State.HasWindChain))
						return false;
				}

				var targetTile = map!.Tiles[x + y * map.Width];
				return BlocksMovement(targetTile.Underlay) || BlocksMovement(targetTile.Overlay);
			}

			bool BlocksMovement(int tileIndex)
			{
				if (tileIndex == 0)
					return false;

				var flags = GetTileInfo(tileIndex).Flags;

				return flags.HasFlag(TileFlags.BlockAllMovement) || !flags.HasFlag((TileFlags)(1 << (8 + (int)game!.State.TravelType)));
			}

			if (TileBlocksMovement(newX, newY))
			{
				if (newY != oldPosition.Y && !TileBlocksMovement(oldPosition.X, newY))
				{
					// only move in y direction
					game.State.PartyPosition = new(oldPosition.X, newY);
					return true;
				}
				else if (newX != oldPosition.X && !TileBlocksMovement(newX, oldPosition.Y))
				{
					// only move in x direction
					game.State.PartyPosition = new(newX, oldPosition.Y);
					return true;
				}
				else
				{
					// stop as we hit an obstacle
					x = 0;
					y = 0;
					return false;
				}
			}

			// Can move
			game.State.PartyPosition = new(newX, newY);
			return true;
		}

		private void AfterMove()
		{
			var playerPosition = game!.State.PartyPosition;
			FillMap(playerPosition.X - TilesPerRow / 2, playerPosition.Y - TileRows / 2, true);

			// Check for events
			var eventIndex = map!.Tiles[playerPosition.X + playerPosition.Y * map.Width].Event;

			if (eventIndex != 0)
				game.EventHandler.HandleEvent(EventTrigger.Move, Event.CreateEvent(map.Events[eventIndex - 1]), map);
		}

		private void UpdateMovement()
		{
			bool left = game!.IsKeyDown(Key.Left) || game.IsKeyDown('A');
			bool right = game!.IsKeyDown(Key.Right) || game.IsKeyDown('D');
			bool up = game!.IsKeyDown(Key.Up) || game.IsKeyDown('W');
			bool down = game!.IsKeyDown(Key.Down) || game.IsKeyDown('S');
			bool upLeft = game.IsKeyDown('Q');
			bool upRight = game.IsKeyDown('E');
			bool downLeft = game.IsKeyDown('Y') || game.IsKeyDown('Z');
			bool downRight = game.IsKeyDown('C');

			if (buttonLayout == ButtonLayout.Movement)
			{
				if (!left)
					left = game.IsKeyDown(Key.Keypad4);
				if (!right)
					right = game.IsKeyDown(Key.Keypad6);
				if (!up)
					up = game.IsKeyDown(Key.Keypad8);
				if (!down)
					down = game.IsKeyDown(Key.Keypad2);
				if (!upLeft)
					upLeft = game.IsKeyDown(Key.Keypad7);
				if (!upRight)
					upRight = game.IsKeyDown(Key.Keypad9);
				if (!downLeft)
					downLeft = game.IsKeyDown(Key.Keypad1);
				if (!downRight)
					downRight = game.IsKeyDown(Key.Keypad3);
			}

			if (upLeft || downLeft)
				left = true;
			if (upRight || downRight)
				right = true;
			if (upLeft || upRight)
				up = true;
			if (downLeft || downRight)
				down = true;

			bool wasMovingBefore = moveX != 0 || moveY != 0;

			if (left && !right)
			{
				game.State.PartyDirection = Direction.Left;
				moveX = -1;
			}
			else if (right && !left)
			{
				game.State.PartyDirection = Direction.Right;
				moveX = 1;
			}
			else
			{
				moveX = 0;
			}

			if (up && !down)
			{
				game.State.PartyDirection = Direction.Up;
				moveY = -1;
			}
			else if (down && !up)
			{
				game.State.PartyDirection = Direction.Down;
				moveY = 1;
			}
			else
			{
				moveY = 0;
			}

			if (!wasMovingBefore && (moveX != 0 || moveY != 0))
			{
				if (MovePlayer(ref moveX, ref moveY))
				{
					AfterMove();

					var ticksPerStep = TicksPerStep[game.State.TravelType];
					moveTickCounter = -ticksPerStep;
				}
			}
		}

		public override void KeyDown(Key key, KeyModifiers keyModifiers)
		{
			UpdateMovement();
		}

		public override void KeyUp(Key key, KeyModifiers keyModifiers)
		{
			UpdateMovement();
		}

		private void FillMap(int scrollOffsetX, int scrollOffsetY, bool force = false)
		{
			if (scrollOffsetX < MinScrollX)
				scrollOffsetX = MinScrollX;
			else if (scrollOffsetX + TilesPerRow > map!.Width - MinScrollX)
				scrollOffsetX = map!.Width - TilesPerRow - MinScrollX;
			if (scrollOffsetY < MinScrollY)
				scrollOffsetY = MinScrollY;
			else if (scrollOffsetY + TileRows > map!.Height - MinScrollY)
				scrollOffsetY = map.Height - TileRows - MinScrollY;

			if (!force && scrollOffsetX == lastScrollX && scrollOffsetY == lastScrollY)
				return; // nothing to do

			lastScrollX = scrollOffsetX;
			lastScrollY = scrollOffsetY;

			// Regarding render order. There are only 2 supported scenarios:
			// - Underlay <- Player <- Overlay (default)
			// - Underlay <- Overlay <- Player

			for (int y = 0; y < TileRows; y++)
			{
				for (int x = 0; x < TilesPerRow; x++)
				{
					int gridIndex = x + y * TilesPerRow;
					int index = (x + scrollOffsetX) + (y + scrollOffsetY) * map!.Width;
					var tile = map.Tiles[index];

					if (tile.Underlay != 0)
					{
						CreateTileSprite(underlay, gridIndex, OffsetX + x * TileWidth, OffsetY + y * TileHeight, tile.Underlay);
					}
					else if (underlay.TryGetValue(gridIndex, out var underlaySprite))
					{
						underlaySprite.Visible = false;
					}

					if (tile.Overlay != 0)
					{
						CreateTileSprite(overlay, gridIndex, OffsetX + x * TileWidth, OffsetY + y * TileHeight, tile.Overlay, 2 * RenderOrderOffset);
					}
					else if (overlay.TryGetValue(gridIndex, out var overlaySprite))
					{
						overlaySprite.Visible = false;
					}
				}
			}

			var playerPosition = game!.State.PartyPosition;
			int playerTileIndex = playerPosition.X + playerPosition.Y * map!.Width;
			var playerTile = map.Tiles[playerTileIndex];
			bool playerVisible = true;
			int playerBaseLineOffset = 3 * RenderOrderOffset; // ensure drawing player over overlay by default

			if (playerTile.Underlay != 0)
			{
				var flags = GetTileInfo(playerTile.Underlay).Flags;

				if (flags.HasFlag(TileFlags.PartyInvisible))
					playerVisible = false;
			}

			if (playerTile.Overlay != 0)
			{
				var flags = GetTileInfo(playerTile.Overlay).Flags;

				if (flags.HasFlag(TileFlags.PartyInvisible))
					playerVisible = false;

				if (flags.HasFlag(TileFlags.Foreground))
					playerBaseLineOffset = RenderOrderOffset; // draw player below overlay
			}

			if (playerVisible)
			{
				var renderLayer = game!.Renderer.Layers[(int)Layer.Map2D];
				var tileset = tilesets![map!.TilesetIndex - 1];
				var tileInfo = tileset!.Tiles[tileset.PlayerSpriteIndex - 1];
				player!.BaseLineOffset = playerBaseLineOffset;
				player.TextureOffset = renderLayer.Config.Texture!.GetOffset(tileGraphicOffset + tileInfo.ImageIndex + (int)game.State.TravelType * 4 + (int)game.State.PartyDirection);
				player.Position = new(OffsetX + (playerPosition.X - scrollOffsetX) * TileWidth, OffsetY + (playerPosition.Y - scrollOffsetY) * TileHeight);
			}

			player!.Visible = playerVisible;
		}

		private void InitPlayer()
		{
			var renderLayer = game!.Renderer.Layers[(int)Layer.Map2D];
			var tileset = tilesets![map!.TilesetIndex - 1];
			var tileInfo = tileset!.Tiles[tileset.PlayerSpriteIndex - 1];
			var playerPositon = game.State.PartyPosition;
			player = renderLayer.SpriteFactory!.Create();

			player.TextureOffset = renderLayer.Config.Texture!.GetOffset(tileGraphicOffset + tileInfo.ImageIndex);
			player.Position = new(OffsetX + playerPositon.X * TileWidth, OffsetY + playerPositon.Y * TileHeight);
			player.PaletteIndex = game.PaletteIndexProvider.GetTilesetPaletteIndex(map.TilesetIndex);
			player.Size = new(TileWidth, TileHeight);
			player!.Visible = true;

			moveX = 0;
			moveY = 0;
		}

		private void ClearMap()
		{
			underlay.Values.ToList().ForEach(tile => tile.Visible = false);
			underlay.Clear();
			overlay.Values.ToList().ForEach(tile => tile.Visible = false);
			overlay.Clear();
		}

		private IAnimatedSprite CreateTileSprite(Dictionary<int, IAnimatedSprite> mapLayer, int gridIndex, int x, int y, int index, int baseLineOffset = 0)
		{
			var renderLayer = game!.Renderer.Layers[(int)Layer.Map2D];

			if (!mapLayer.TryGetValue(gridIndex, out var tileSprite))
			{
				tileSprite = renderLayer.SpriteFactory!.CreateAnimated();
				tileSprite.Position = new(x, y);
				tileSprite.Size = new(TileWidth, TileHeight);
				tileSprite.Opaque = mapLayer == underlay;
				mapLayer.Add(gridIndex, tileSprite);
			}

			var tileInfo = GetTileInfo(index);

			tileSprite.FrameCount = Math.Max(1, tileInfo.FrameCount);			
			tileSprite.TextureOffset = renderLayer.Config.Texture!.GetOffset(tileGraphicOffset + tileInfo.ImageIndex);
			tileSprite.PaletteIndex = game.PaletteIndexProvider.GetTilesetPaletteIndex(map!.TilesetIndex);
			tileSprite.BaseLineOffset = baseLineOffset;
			tileSprite.Visible = true;

			return tileSprite;
		}

		private ITile GetTileInfo(int index)
		{
			var tileset = tilesets![map!.TilesetIndex - 1];
			return tileset!.Tiles[index - 1];
		}

		private void LoadMap(int index)
		{
			lastScrollX = -1;
			lastScrollY = -1;
			map = game!.AssetProvider.MapLoader.LoadMap(index) as IMap2D; // TODO: catch exceptions
			tileGraphicOffset = map!.TilesetIndex == 1 ? 0 : tilesets![0].Graphics.Count + 1;

			game.State.MapIndex = index;
			game.State.TravelType = TravelType.Walk; // TODO: is it possible to change map with travel type (always reset to walk for non-world maps though!)
		}
	}
}
