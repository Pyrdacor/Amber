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

		// Note: In original you could only use by mouse or buttons on 2D.
		// In general travel types had a delay which was given in number
		// of vertical blanks. The delays were 5, 2, 3, 2, 4, 0, 0.
		// For movement in cities it was even 0 for waling.
		// However this is way too fast when using automatic movement
		// (e.g. holding the key down).
		const int CityTicksPerStep = 20;
		static readonly Dictionary<TravelType, int> TicksPerStep = new()
		{
			{ TravelType.Walk, 6 * CityTicksPerStep },
			{ TravelType.Horse, 3 * CityTicksPerStep },
			{ TravelType.Raft, 4 * CityTicksPerStep },
			{ TravelType.Ship, 3 * CityTicksPerStep },
			{ TravelType.MagicDisc, 5 * CityTicksPerStep },
			{ TravelType.Eagle, 1 * CityTicksPerStep },
			{ TravelType.SuperChicken, 1 * CityTicksPerStep },
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
		long lastMoveStartTicks = 0;
		long currentTicks = 0;
		bool additionalMoveRequested = false;
		bool screenPushPlayerWasVisible = false;

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

		public override void ScreenPushed(Game game, Screen screen)
		{
			base.ScreenPushed(game, screen);

			// TODO: check for transparent screens?
			underlay.Values.ToList().ForEach(tile => tile.Visible = false);
			overlay.Values.ToList().ForEach(tile => tile.Visible = false);
			screenPushPlayerWasVisible = player!.Visible;
			player!.Visible = false;
		}

		public override void ScreenPopped(Game game, Screen screen)
		{
			game.SetLayout(Layout.Map2D);
			underlay.Values.ToList().ForEach(tile => tile.Visible = true);
			overlay.Values.ToList().ForEach(tile => tile.Visible = true);
			player!.Visible = screenPushPlayerWasVisible;

			base.ScreenPopped(game, screen);
		}

		public override void Open(Game game, Action? closeAction)
		{
			base.Open(game, closeAction);

			moveTickCounter = 0;
			lastMoveStartTicks = 0;
			currentTicks = 0;
			additionalMoveRequested = false;

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

			base.Close(game);
		}

		public override void Update(Game game, long elapsedTicks)
		{
			if (elapsedTicks == 0)
				return;

			currentTicks += elapsedTicks;

			if (moveX != 0 || moveY != 0)
			{
				moveTickCounter += elapsedTicks;

				var ticksPerStep = GetTicksPerStep();

				if (ticksPerStep > 0 && moveTickCounter >= ticksPerStep)
				{
					bool moved = false;

					while (moveTickCounter >= ticksPerStep)
					{
						if (MovePlayer(moveX, moveY))
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

		private bool MovePlayer(int x, int y)
		{
			additionalMoveRequested = false;
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
					game.State.SetPartyPosition(oldPosition.X, newY);
					return true;
				}
				else if (newX != oldPosition.X && !TileBlocksMovement(newX, oldPosition.Y))
				{
					// only move in x direction
					game.State.SetPartyPosition(newX, oldPosition.Y);
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
			game.State.SetPartyPosition(newX, newY);
			return true;
		}

		private void AfterMove()
		{
			var playerPosition = game!.State.PartyPosition;
			FillMap(playerPosition.X - TilesPerRow / 2, playerPosition.Y - TileRows / 2, true);

			game.Time.Moved2D();

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

			if (additionalMoveRequested && !left && !right && !up && !down)
			{
				long timeTillNextMove = Math.Max(0, GetTicksPerStep() - (currentTicks - lastMoveStartTicks));
				int x = moveX;
				int y = moveY;
				game.AddDelayedAction(timeTillNextMove, () =>
				{
					lastMoveStartTicks = currentTicks;
					if (MovePlayer(x, y))
						AfterMove();
					moveTickCounter = 0;
				});
				additionalMoveRequested = false;
			}
			else if (!additionalMoveRequested)
			{
				additionalMoveRequested = (currentTicks - lastMoveStartTicks) < GetTicksPerStep();
			}

			bool wasMovingBefore = moveX != 0 || moveY != 0 || additionalMoveRequested;

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
				if (MovePlayer(moveX, moveY))
				{
					lastMoveStartTicks = currentTicks;
					moveTickCounter = -GetTicksPerStep();

					AfterMove();					
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

		private int GetTicksPerStep() => map!.Flags.HasFlag(MapFlags.Wilderness) ? TicksPerStep[game!.State.TravelType] : CityTicksPerStep;

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
