using Amber.Common;
using Amber.Renderer;
using Amberstar.Game.Events;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

internal class Map2DScreen : Screen
{
	enum ButtonLayout
	{
		Movement,
		Actions
	}

	class WorldMap : IMap2D
	{
		IMap2D[] maps = [];
		Dictionary<int, IMap2D> mapCache = [];
		List<IEvent> events = [];
		Tile2D[] tiles = [];

		public int Width => WorldMapWidth * 2;

		public int Height => WorldMapHeight * 2;

		public MapType Type => MapType.Map2D;

		public MapFlags Flags => maps[0].Flags;

		public MapNPC[] NPCs => [];

		public Position[][] NPCPositions => [];

		public List<IEvent> Events => events;

		public int TilesetIndex => maps[0].TilesetIndex;

		public Tile2D[] Tiles => tiles;

		public int UpperLeftMapIndex { get; private set; }

		public void SetMaps(IMap2D[] maps, int[] mapIndices)
		{
			this.maps = maps;
			UpperLeftMapIndex = mapIndices[0];

			for (int i = 0; i < 4; i++)
				this.mapCache.TryAdd(mapIndices[i], maps[i]);

			events = maps.SelectMany(map => map.Events).ToList();

			tiles = new Tile2D[Width * Height];

			for (int y = 0; y < WorldMapHeight; y++)
			{
				for (int x = 0; x < WorldMapWidth; x++)
				{
					tiles[x + y * Width] = maps[0].Tiles[x + y * WorldMapWidth];
				}

				for (int x = 0; x < WorldMapWidth; x++)
				{
					tiles[WorldMapWidth + x + y * Width] = maps[1].Tiles[x + y * WorldMapWidth];
				}
			}

			for (int y = 0; y < WorldMapHeight; y++)
			{
				for (int x = 0; x < WorldMapWidth; x++)
				{
					tiles[x + (y + WorldMapHeight) * Width] = maps[2].Tiles[x + y * WorldMapWidth];
				}

				for (int x = 0; x < WorldMapWidth; x++)
				{
					tiles[WorldMapWidth + x + (y + WorldMapHeight) * Width] = maps[3].Tiles[x + y * WorldMapWidth];
				}
			}
		}

		public IMap2D? GetMapByIndex(int index) => mapCache.GetValueOrDefault(index);

		public IEvent? GetEvent(int x, int y)
		{
			int eventIndex = 0;

			static int AdjustEventIndex(int eventIndex, int offset)
			{
				if (eventIndex == 0)
					return 0;

				return offset + eventIndex;
			}

			if (x < WorldMapWidth)
			{
				if (y < WorldMapHeight)
					eventIndex = maps[0].Tiles[x + y * WorldMapWidth].Event;
				else
					eventIndex = AdjustEventIndex(maps[2].Tiles[x + (y - WorldMapHeight) * WorldMapWidth].Event, 2 * IMap.EventCount);
			}
			else
			{
				if (y < WorldMapHeight)
					eventIndex = AdjustEventIndex(maps[1].Tiles[x - WorldMapWidth + y * WorldMapWidth].Event, 1 * IMap.EventCount);
				else
					eventIndex = AdjustEventIndex(maps[3].Tiles[x - WorldMapWidth + (y - WorldMapHeight) * WorldMapWidth].Event, 3 * IMap.EventCount);
			}

			if (eventIndex == 0)
				return null;

			return events[eventIndex - 1];
		}
	}

	// Note: In original you could only move by mouse or buttons on 2D maps.
	// In general travel types had a delay which was given in number
	// of vertical blanks. The delays were 5, 2, 3, 2, 4, 0, 0.
	// For movement in cities it was even 0 for waling.
	// However this is way too fast when using automatic movement
	// (e.g. holding the key down).
	const int CityTicksPerStep = 20;
	const int WorldMapBaseTicksPerStep = 2;
	static readonly Dictionary<TravelType, int> TicksPerStep = new()
	{
		{ TravelType.Walk, 6 * WorldMapBaseTicksPerStep },
		{ TravelType.Horse, 3 * WorldMapBaseTicksPerStep },
		{ TravelType.Raft, 4 * WorldMapBaseTicksPerStep },
		{ TravelType.Ship, 3 * WorldMapBaseTicksPerStep },
		{ TravelType.MagicDisc, 5 * WorldMapBaseTicksPerStep },
		{ TravelType.Eagle, 1 * WorldMapBaseTicksPerStep },
		{ TravelType.SuperChicken, 1 * WorldMapBaseTicksPerStep },
	};

	const int TilesPerRow = 11;
	const int TileRows = 9;
	const int TileWidth = 16;
	const int TileHeight = 16;
	const int WorldMapWidthInMaps = 8;
	const int WorldMapHeightInMaps = 8;
	const int WorldMapCount = WorldMapWidthInMaps * WorldMapHeightInMaps;
	public const int WorldMapWidth = 50;
	public const int WorldMapHeight = 50;
	const int WorldMapPreloadOffset = 12;
	const int OffsetX = 16;
	const int OffsetY = 49;
	const int RenderOrderOffset = TileHeight / 4;
	const int MinScrollX = TilesPerRow / 2;
	const int MinScrollY = TileRows / 2 + 1;
	Game? game;
	IMap2D? map;
	WorldMap? worldMap;
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
	IRenderText? timeText; // for debugging, TODO: REMOVE
	byte palette = 0;
	long delayedMoveActionIndex = -1;
	ButtonGrid? buttonGrid;
	bool mouseDown = false;

	public override ScreenType Type { get; } = ScreenType.Map2D;
	public IMap2D Map => map!;

	internal void MapChanged()
	{
		LoadMap(game!.State.MapIndex);
		AfterMove();
	}

	public override void Init(Game game)
	{
		this.game = game;
		tilesets = [game.AssetProvider.TilesetLoader.LoadTileset(1), game.AssetProvider.TilesetLoader.LoadTileset(2)];

		timeText = game.TextManager.Create($"{game.State.Hour:00}:{game.State.Minute:00}", 15, -1, palette);
		timeText.Show(220, 70, 100);

		game.Time.MinuteChanged += () =>
		{
			timeText?.Delete();

			if (game.ScreenHandler.ActiveScreen?.Type != ScreenType.Map2D)
				return;

			timeText = game.TextManager.Create($"{game.State.Hour:00}:{game.State.Minute:00}", 15, -1, palette);
			timeText.Show(220, 70, 100);
		};
	}

	public override void ScreenPushed(Game game, Screen screen)
	{
		base.ScreenPushed(game, screen);

		// Don't move any further
		ResetMovement();

		if (!screen.Transparent)
		{
			underlay.Values.ToList().ForEach(tile => tile.Visible = false);
			overlay.Values.ToList().ForEach(tile => tile.Visible = false);
			screenPushPlayerWasVisible = player!.Visible;
			player!.Visible = false;
		}

		timeText?.Delete();

		game.Pause();
	}

	public override void ScreenPopped(Game game, Screen screen)
	{
		if (!screen.Transparent)
		{
			game.SetLayout(Layout.Map2D);
			underlay.Values.ToList().ForEach(tile => tile.Visible = true);
			overlay.Values.ToList().ForEach(tile => tile.Visible = true);
			player!.Visible = screenPushPlayerWasVisible;
		}

		timeText?.Delete();
		timeText = game.TextManager.Create($"{game.State.Hour:00}:{game.State.Minute:00}", 15, -1, palette);
		timeText.Show(220, 70, 100);

		base.ScreenPopped(game, screen);

		game.Resume();
	}

	public override void Open(Game game, Action? closeAction)
	{
		base.Open(game, closeAction);

		moveTickCounter = 0;
		lastMoveStartTicks = 0;
		currentTicks = 0;
		additionalMoveRequested = false;
		mouseDown = false;

		game.SetLayout(Layout.Map2D);
		buttonGrid = new(game);
		buttonGrid.ClickButtonAction += ButtonClicked;
		buttonLayout = ButtonLayout.Movement;
		SetupButtons();		
		LoadMap(game.State.MapIndex);
		InitPlayer();
		AfterMove();
	}

	private void ButtonClicked(int index)
	{
		if (buttonLayout == ButtonLayout.Movement)
		{
			if (index == 4)
			{
				game!.Time.Tick();
				return;
			}

			int moveX = index % 3 - 1;
			int moveY = index / 3 - 1;

			if (moveY < 0)
				game!.State.PartyDirection = Direction.Up;
			else if (moveY > 0)
				game!.State.PartyDirection = Direction.Down;
			else if (moveX < 0)
				game!.State.PartyDirection = Direction.Left;
			else if (moveX > 0)
				game!.State.PartyDirection = Direction.Right;

			if (currentTicks - lastMoveStartTicks >= GetTicksPerStep())
			{
				lastMoveStartTicks = currentTicks;
				if (MovePlayer(moveX, moveY))
					AfterMove();
			}
		}
		else // Actions
		{
			// TODO
		}
	}

	private void SetupButtons()
	{
		if (buttonLayout == ButtonLayout.Movement)
		{
			// Upper row
			buttonGrid!.SetButton(0, ButtonType.ArrowUpLeft);
			buttonGrid.SetButton(1, ButtonType.ArrowUp);
			buttonGrid.SetButton(2, ButtonType.ArrowUpRight);
			// Middle row
			buttonGrid.SetButton(3, ButtonType.ArrowLeft);
			buttonGrid.SetButton(4, ButtonType.Sleep);
			buttonGrid.SetButton(5, ButtonType.ArrowRight);
			// Lower row
			buttonGrid.SetButton(6, ButtonType.ArrowDownLeft);
			buttonGrid.EnableButton(6, true);
			buttonGrid.SetButton(7, ButtonType.ArrowDown);
			buttonGrid.SetButton(8, ButtonType.ArrowDownRight);
		}
		else // Actions
		{
			// Upper row
			buttonGrid!.SetButton(0, ButtonType.Eye);
			buttonGrid.SetButton(1, ButtonType.Ear);
			buttonGrid.SetButton(2, ButtonType.Mouth);
			// Middle row
			buttonGrid.SetButton(3, ButtonType.UseTransport);
			buttonGrid.SetButton(4, ButtonType.UseMagic);
			buttonGrid.SetButton(5, ButtonType.Camp);
			// Lower row
			buttonGrid.SetButton(6, ButtonType.Map);
			buttonGrid.EnableButton(6, false);
			buttonGrid.SetButton(7, ButtonType.PartyPositions);
			buttonGrid.SetButton(8, ButtonType.Disk);
		}
	}

	public override void Close(Game game)
	{
		// Don't move any further
		ResetMovement();

		ClearMap();
		player!.Visible = false;
		player = null;
		buttonGrid!.Destroy();

		timeText?.Delete();

		base.Close(game);
	}

	private void ResetMovement()
	{
		moveX = 0;
		moveY = 0;
		mouseDown = false;
		game!.DeleteDelayedActions(delayedMoveActionIndex);
	}

	public override void Update(Game game, long elapsedTicks)
	{
		if (game.Paused || !game.InputEnabled)
			ResetMovement();

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
			var @event = GetEvent(x, y);

			if (@event != null)
			{
				// TODO: we should check if the event is still active
				if (@event.Type == EventType.MapExit ||
					@event.Type == EventType.Teleporter ||
					@event.Type == EventType.TrapDoor ||
					@event.Type == EventType.TravelExit ||
					(@event.Type == EventType.WindGate && game.State.HasWindChain))
					return false;
			}

			var targetTile = map!.Tiles[x + y * map.Width];

			// TODO: use priority bit in flags
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
		if (worldMap != null)
			UpdateWorldMap();

		var playerPosition = game!.State.PartyPosition;

		game.Time.Moved2D();

		// Check for events
		var @event = GetEvent(playerPosition.X, playerPosition.Y);

		if (@event != null)
		{
			var mapEvent = Event.CreateEvent(@event);

			if (game.EventHandler.HandleEvent(EventTrigger.Move, mapEvent, map!) && mapEvent is ITeleportEvent)
			{
				// Don't update the map if we are teleporting away.
				return;
			}
		}

		FillMap(playerPosition.X - TilesPerRow / 2, playerPosition.Y - TileRows / 2, true);
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

		if (mouseDown && game.InputEnabled && !game.Paused)
		{
			switch (game!.Cursor.CursorType)
			{
				case CursorType.ArrowUp2D:
					up = true;
					break;
				case CursorType.ArrowDown2D:
					down = true;
					break;
				case CursorType.ArrowLeft2D:
					left = true;
					break;
				case CursorType.ArrowRight2D:
					right = true;
					break;
				case CursorType.ArrowUpLeft2D:
					up = true;
					left = true;
					break;
				case CursorType.ArrowUpRight2D:
					up = true;
					right = true;
					break;
				case CursorType.ArrowDownLeft2D:
					down = true;
					left = true;
					break;
				case CursorType.ArrowDownRight2D:
					down = true;
					right = true;
					break;
			}
		}

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
			game.DeleteDelayedActions(delayedMoveActionIndex);
			delayedMoveActionIndex = game.AddDelayedAction(timeTillNextMove, () =>
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

	public override void MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		if (buttons == MouseButtons.Right)
		{
			if (ButtonGrid.Area.Contains(position))
			{
				buttonLayout = (ButtonLayout)(1 - (int)buttonLayout); // toggle
				SetupButtons();
			}
		}
		else
		{
			mouseDown = true;
			var mapArea = new Rect(OffsetX, OffsetY, TilesPerRow * TileWidth, TileRows * TileHeight);

			if (mapArea.Contains(position))
			{
				if (game!.Cursor.CursorType == CursorType.Zzz)
					game.Time.Tick();
				else if (game.Cursor.CursorType >= CursorType.ArrowUp2D && game.Cursor.CursorType <= CursorType.ArrowDownLeft2D)
					UpdateMovement();				

				return;
			}

			buttonGrid!.MouseClick(position);
		}
	}

	public override void MouseUp(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		mouseDown = false;
		UpdateMovement();

		base.MouseUp(position, buttons, keyModifiers);		
	}

	public override void MouseMove(Position position, MouseButtons buttons)
	{
		base.MouseMove(position, buttons);

		var mapArea = new Rect(OffsetX, OffsetY, TilesPerRow * TileWidth, TileRows * TileHeight);

		if (mapArea.Contains(position))
		{
			/*int relativeX = position.X - mapArea.Left;
			int relativeY = position.Y - mapArea.Top;
			bool left = relativeX < mapArea.Size.Width / 4;
			bool right = relativeX >= mapArea.Size.Width * 3 / 4;
			bool up = relativeY < mapArea.Size.Height / 4;
			bool down = relativeY >= mapArea.Size.Height * 3 / 4;*/
			bool left = position.X < player!.Position.X;
			bool right = position.X >= player.Position.X + 16;
			bool up = position.Y < player!.Position.Y;
			bool down = position.Y >= player.Position.Y + 16;

			var lastCursor = game!.Cursor.CursorType;

			if (up)
			{
				if (left)
					game.Cursor.CursorType = CursorType.ArrowUpLeft2D;
				else if (right)
					game.Cursor.CursorType = CursorType.ArrowUpRight2D;
				else
					game.Cursor.CursorType = CursorType.ArrowUp2D;
			}
			else if (down)
			{
				if (left)
					game.Cursor.CursorType = CursorType.ArrowDownLeft2D;
				else if (right)
					game.Cursor.CursorType = CursorType.ArrowDownRight2D;
				else
					game.Cursor.CursorType = CursorType.ArrowDown2D;
			}
			else
			{
				if (left)
					game.Cursor.CursorType = CursorType.ArrowLeft2D;
				else if (right)
					game.Cursor.CursorType = CursorType.ArrowRight2D;
				else
					game.Cursor.CursorType = CursorType.Zzz;
			}

			if (mouseDown && lastCursor != game.Cursor.CursorType)
				UpdateMovement();
		}
		else if (game!.Cursor.CursorType != CursorType.Sword)
		{
			game!.Cursor.CursorType = CursorType.Sword;
			UpdateMovement();
		}
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
			var renderLayer = game!.GetRenderLayer(Layer.Map2D);
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
		var renderLayer = game!.GetRenderLayer(Layer.Map2D);
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
		var renderLayer = game!.GetRenderLayer(Layer.Map2D);

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

	public static int GetWorldMapIndex(int index, int offsetX, int offsetY)
	{
		int currentX = index % WorldMapWidthInMaps;
		int currentY = index / WorldMapWidthInMaps;

		currentX = (currentX + offsetX + WorldMapWidthInMaps) % WorldMapWidthInMaps;
		currentY = (currentY + offsetY + WorldMapHeightInMaps) % WorldMapHeightInMaps;

		return currentX + currentY * WorldMapWidthInMaps;
	}

	private IEvent? GetEvent(int x, int y)
	{
		if (map is WorldMap worldMap)
			return worldMap.GetEvent(x, y);
		
		var eventIndex = map!.Tiles[x + y * map.Width].Event;

		if (eventIndex == 0)
			return null;

		return map.Events[eventIndex - 1];
	}

	private void UpdateWorldMap(int? mapIndex = null)
	{
		// The first 64 maps (index 1 to 64) are the world maps.

		int newTopLeftMapIndex = mapIndex ?? worldMap?.UpperLeftMapIndex ??
			throw new AmberException(ExceptionScope.Application, "No world map active and no map index given.");
		
		int[] mapIndices;
		var playerPosition = game!.State.PartyPosition;
		bool changed = mapIndex != null;

		if (playerPosition.X < WorldMapPreloadOffset)
		{
			newTopLeftMapIndex = GetWorldMapIndex(newTopLeftMapIndex, -1, 0);
			playerPosition = new(playerPosition.X + WorldMapWidth, playerPosition.Y);
			changed = true;
		}
		else if (playerPosition.X >= 2 * WorldMapWidth - WorldMapPreloadOffset)
		{
			newTopLeftMapIndex = GetWorldMapIndex(newTopLeftMapIndex, 1, 0);
			playerPosition = new(playerPosition.X - WorldMapWidth, playerPosition.Y);
			changed = true;
		}

		if (playerPosition.Y < WorldMapPreloadOffset)
		{
			newTopLeftMapIndex = GetWorldMapIndex(newTopLeftMapIndex, 0, -1);
			playerPosition = new(playerPosition.X, playerPosition.Y + WorldMapHeight);
			changed = true;
		}
		else if (playerPosition.Y >= 2 * WorldMapHeight - WorldMapPreloadOffset)
		{
			newTopLeftMapIndex = GetWorldMapIndex(newTopLeftMapIndex, 0, 1);
			playerPosition = new(playerPosition.X, playerPosition.Y - WorldMapHeight);
			changed = true;
		}

		if (changed)
		{
			mapIndices =
			[
				newTopLeftMapIndex,
				GetWorldMapIndex(newTopLeftMapIndex, 1, 0),
				GetWorldMapIndex(newTopLeftMapIndex, 0, 1),
				GetWorldMapIndex(newTopLeftMapIndex, 1, 1),
			];

			game.State.SetPartyPosition(playerPosition.X, playerPosition.Y);
			bool firstTime = worldMap == null;
			worldMap ??= new WorldMap();
			worldMap.SetMaps(mapIndices.Select(GetMap).ToArray(), mapIndices);
			map = worldMap;

			game.State.MapIndex = newTopLeftMapIndex;

			IMap2D GetMap(int index)
			{
				if (index == mapIndex && firstTime)
					return this.map!;

				var map = worldMap?.GetMapByIndex(index);

				return map ?? (game!.AssetProvider.MapLoader.LoadMap(index) as IMap2D)!;
			}
		}
	}

	private void LoadMap(int index)
	{
		lastScrollX = -1;
		lastScrollY = -1;
		map = game!.AssetProvider.MapLoader.LoadMap(index) as IMap2D; // TODO: catch exceptions
		bool isWorldMap = map!.Flags.HasFlag(MapFlags.Wilderness);

		if (isWorldMap)
		{
			UpdateWorldMap(index);
		}
		else
		{
			worldMap = null;
		}

		tileGraphicOffset = map.TilesetIndex == 1 ? 0 : tilesets![0].Graphics.Count + 1;
		palette = game.PaletteIndexProvider.GetTilesetPaletteIndex(map.TilesetIndex);
		
		game.State.MapIndex = index;
		game.State.SetIsWorldMap(isWorldMap);
		game.State.TravelType = TravelType.Walk; // TODO: is it possible to change map with travel type (always reset to walk for non-world maps though!)
		game.Cursor.PaletteIndex = palette;
	}
}
