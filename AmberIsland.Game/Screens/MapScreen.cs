using Amber.Common;
using Amber.Renderer.Common;
using AmberIsland.Game;
using AmberIsland.Game.UI;
using AmberIsland.GameData;

namespace AmberIsland.Game.Screens;

internal class MapScreen : Screen
{
	const int WalkTicksPerStep = 2;
    const int RunTicksPerStep = 1;
	internal const int TileWidth = 16;
    internal const int TileHeight = 16;
    const int TilesPerRow = Game.VirtualScreenWidth / TileWidth;
    const int TileRows = Game.VirtualScreenHeight / TileHeight;
    const int OffsetX = 0;
	const int OffsetY = 0;
	const int RenderOrderOffset = TileHeight / 4;
	const int MinScrollX = TilesPerRow / 2;
	const int MinScrollY = TileRows / 2 + 1;
	internal static readonly double DiagonalDistance = Math.Sqrt(2);
	Game? game;
	Map? map;
	Tileset? tileset;
	//WorldMap? worldMap;
	//ITileset[]? tilesets;
	readonly Dictionary<int, IAnimatedSprite> underlay = [];
    readonly Dictionary<int, IAnimatedSprite> objects = [];
    readonly Dictionary<int, IAnimatedSprite> overlay = [];
	readonly List<MapActor> mapActors = [];
	int lastScrollX = -1;
	int lastScrollY = -1;
	int tileGraphicOffset = 0;
	int ticksPerStep = WalkTicksPerStep;

	int moveX = 0;
	int moveY = 0;
	long moveTickCounter = 0;
	long lastMoveStartTicks = 0;
	long currentTicks = 0;
	bool additionalMoveRequested = false;
	bool screenPushPlayerWasVisible = false;
	byte palette = 0;
	long delayedMoveActionIndex = -1;
	bool mouseDown = false;
    //IRenderText? mapNameText;

    public override ScreenType Type { get; } = ScreenType.Map2D;
	public Map Map => map!;

    internal void MapChanged()
	{
		//LoadMap(game!.State.MapIndex);
        ShowMapName();
        AfterMove();
	}

	public override void Init(Game game)
	{
		this.game = game;
		//tilesets = [game.AssetProvider.TilesetLoader.LoadTileset(1), game.AssetProvider.TilesetLoader.LoadTileset(2)];

		// TODO
		map = game.GameData.GetMap(1);
		tileset = game.GameData.GetTileset(1);
		FillMap(0, 0, true);
		SpawnMonster(new Position(100, 100), Direction.Down, 1);
    }

	public override void ScreenPushed(Game game, Screen screen)
	{
		base.ScreenPushed(game, screen);

		// Don't move any further
		ResetMovement();

		if (!screen.Transparent)
		{
			underlay.Values.ToList().ForEach(tile => tile.Visible = false);
            objects.Values.ToList().ForEach(tile => tile.Visible = false);
            overlay.Values.ToList().ForEach(tile => tile.Visible = false);
            //mapNameText!.Visible = false;
        }

		game.Pause();
	}

	public override void ScreenPopped(Game game, Screen screen)
	{
		if (!screen.Transparent)
		{
			SetLayout();
            underlay.Values.ToList().ForEach(tile => tile.Visible = true);
            objects.Values.ToList().ForEach(tile => tile.Visible = true);
			overlay.Values.ToList().ForEach(tile => tile.Visible = true);
            //mapNameText!.Visible = true;
        }

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

		SetLayout();
		//LoadMap(game.State.MapIndex);
        ShowMapName();
        InitPlayer();
		AfterMove();
	}

	private void SetLayout()
	{
        //game!.SetLayout(Layout.Map2D, palette);
    }

    private void ShowMapName()
    {
        /*var name = game!.AssetProvider.TextLoader.FromString(map!.Name).GetTextBlock(0);

        mapNameText ??= game!.TextManager.Create(name, Game.VirtualScreenWidth, 15, TextManager.TransparentPaper, palette);
        mapNameText.ShowInArea(OffsetX, OffsetY - mapNameText.LineHeight - 3, TilesPerRow * TileWidth, TileRows * TileHeight, 100, TextAlignment.Center);*/
    }

	public override void Close(Game game)
	{
		// Don't move any further
		ResetMovement();

		ClearMap();

        //mapNameText!.Delete();

        base.Close(game);
	}

	private void ResetMovement()
	{
		moveX = 0;
		moveY = 0;
		mouseDown = false;
		game!.DeleteDelayedActions(delayedMoveActionIndex);
		game.Player.CurrentState = Player.State.Idle;
	}

	public override void Update(Game game, long elapsedTicks)
	{
		if (game.Paused || !game.InputEnabled)
			ResetMovement();

		if (elapsedTicks == 0)
			return;

		game.Player.Update(game.GameTicks);

        int tilesPerRow = Math.Min(TilesPerRow, (int)map!.Width);
        int tileRows = Math.Min(TileRows, (int)map!.Height);
        var mapArea = new Rect(OffsetX + lastScrollX * TileWidth, OffsetX + lastScrollY * TileHeight, tilesPerRow * TileWidth, tileRows * TileHeight);

		foreach (var mapActor in mapActors)
		{
			mapActor.Update(mapArea, elapsedTicks);
		}

        currentTicks += elapsedTicks;

		if (moveX != 0 || moveY != 0)
		{
			moveTickCounter += elapsedTicks;

			if (ticksPerStep > 0 && moveTickCounter >= ticksPerStep)
			{
				bool moved = false;

				while (moveTickCounter >= ticksPerStep)
				{
					if (MovePlayer(moveX, moveY))
					{
						if (game.Player.CurrentState != Player.State.Running)
							game.Player.CurrentState = Player.State.Walking;
                        game.Player.Position = game.State.PlayerPosition;
                        game.Player.CurrentDirection = game.State.PlayerDirection;
                        moved = true;
						moveTickCounter -= ticksPerStep;
					}
					else
					{
                        game.Player.CurrentState = Player.State.Idle;
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
		var oldPosition = game!.State.PlayerPosition;
		int newX = MathUtil.Limit(0, oldPosition.X + x, /*map!.Width - 1*/int.MaxValue);
		int newY = MathUtil.Limit(0, oldPosition.Y + y, /*map.Height - 1*/int.MaxValue);

		bool TileBlocksMovement(int x, int y)
		{
			/*var targetTile = map!.Tiles[x + y * map.Width];

			// TODO: use priority bit in flags
			return BlocksMovement(targetTile.Underlay) || BlocksMovement(targetTile.Overlay);*/
			return false;
		}

		bool BlocksMovement(int tileIndex)
		{
			/*if (tileIndex == 0)
				return false;

			var flags = GetTileInfo(tileIndex).Flags;

			return flags.HasFlag(TileFlags.BlockAllMovement) || !flags.HasFlag((TileFlags)(1 << (8 + (int)game!.State.TravelType)));*/
			return false;
		}

		if (TileBlocksMovement(newX, newY))
		{
			if (newY != oldPosition.Y && !TileBlocksMovement(oldPosition.X, newY))
			{
				// only move in y direction
				game.State.PlayerPosition = new(oldPosition.X, newY);
				return true;
			}
			else if (newX != oldPosition.X && !TileBlocksMovement(newX, oldPosition.Y))
			{
				// only move in x direction
				game.State.PlayerPosition = new(newX, oldPosition.Y);
				return true;
			}
			else
			{
				// stop as we hit an obstacle
				return false;
			}
		}

		// Can move
		game.State.PlayerPosition = new(newX, newY);

		return true;
	}

	private void AfterMove()
	{
		/*if (worldMap != null)
			UpdateWorldMap();

		var playerPosition = game!.State.PlayerPosition;

		game.Time.Moved2D();

        FillMap(playerPosition.X - TilesPerRow / 2, playerPosition.Y - TileRows / 2, true);*/
	}

	private void UpdateMovement()
	{
		bool left = game!.IsKeyDown(Key.Left) || game.IsKeyDown('A');
		bool right = game.IsKeyDown(Key.Right) || game.IsKeyDown('D');
		bool up = game.IsKeyDown(Key.Up) || game.IsKeyDown('W');
		bool down = game.IsKeyDown(Key.Down) || game.IsKeyDown('S');
		bool upLeft = game.IsKeyDown('Q');
		bool upRight = game.IsKeyDown('E');
		bool downLeft = game.IsKeyDown('Y') || game.IsKeyDown('Z');
		bool downRight = game.IsKeyDown('C');

		/*if (mouseDown && game.InputEnabled && !game.Paused)
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
		}*/

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
			long timeTillNextMove = Math.Max(0, ticksPerStep - (currentTicks - lastMoveStartTicks));
			int x = moveX;
			int y = moveY;
			game.DeleteDelayedActions(delayedMoveActionIndex);
			delayedMoveActionIndex = game.AddDelayedAction(timeTillNextMove, () =>
			{
				lastMoveStartTicks = currentTicks;
				if (MovePlayer(x, y))
				{
                    if (game.Player.CurrentState != Player.State.Running)
                        game.Player.CurrentState = Player.State.Walking;
					game.Player.Position = game.State.PlayerPosition;
					game.Player.CurrentDirection = game.State.PlayerDirection;
					AfterMove();
				}
				moveTickCounter = 0;
			});
			additionalMoveRequested = false;
		}
		else if (!additionalMoveRequested)
		{
			additionalMoveRequested = (currentTicks - lastMoveStartTicks) < ticksPerStep;
		}

		bool wasMovingBefore = moveX != 0 || moveY != 0 || additionalMoveRequested;

		if (left && !right)
		{
			game.State.PlayerDirection = Direction.Left;
			moveX = -1;
		}
		else if (right && !left)
		{
			game.State.PlayerDirection = Direction.Right;
			moveX = 1;
		}
		else
		{
			moveX = 0;
		}

		if (up && !down)
		{
			game.State.PlayerDirection = Direction.Up;
			moveY = -1;
		}
		else if (down && !up)
		{
			game.State.PlayerDirection = Direction.Down;
			moveY = 1;
		}
		else
		{
			moveY = 0;
		}

		if (moveX != 0 || moveY != 0)
		{
			ticksPerStep = game.IsKeyDown(Key.Space) ? RunTicksPerStep : WalkTicksPerStep;
		}

        if (!wasMovingBefore && (moveX != 0 || moveY != 0))
		{
			if (MovePlayer(moveX, moveY))
			{
                game.Player.CurrentState = game.IsKeyDown(Key.Space) ? Player.State.Running : Player.State.Walking;
                game.Player.Position = game.State.PlayerPosition;
				game.Player.CurrentDirection = game.State.PlayerDirection;
				lastMoveStartTicks = currentTicks;
				moveTickCounter = -ticksPerStep;

				AfterMove();					
			}
		}

		if (moveX == 0 && moveY == 0)
            game.Player.CurrentState = Player.State.Idle;
    }

	public override void KeyDown(Key key, KeyModifiers keyModifiers)
	{
		UpdateMovement();

		if (key == Key.Space && game?.Player.CurrentState == Player.State.Walking)
			game.Player.CurrentState = Player.State.Running;
	}

	public override void KeyUp(Key key, KeyModifiers keyModifiers)
	{
		UpdateMovement();

        if (key == Key.Space && game?.Player.CurrentState == Player.State.Running)
            game.Player.CurrentState = Player.State.Walking;
    }

	public override void MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		if (buttons == MouseButtons.Left)
		{
			mouseDown = true;
			var mapArea = new Rect(OffsetX, OffsetY, TilesPerRow * TileWidth, TileRows * TileHeight);

			if (mapArea.Contains(position))
			{
				/*if (game!.Cursor.CursorType == CursorType.Zzz)
					game.Time.Tick();*/				

				return;
			}

			base.MouseDown(position, buttons, keyModifiers);
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
	}

	private void FillMap(int scrollOffsetX, int scrollOffsetY, bool force = false)
	{
        int tilesPerRow = Math.Min(TilesPerRow, (int)map!.Width);
		int tileRows = Math.Min(TileRows, (int)map!.Height);

        for (int y = 0; y < tileRows; y++)
        {
            for (int x = 0; x < tilesPerRow; x++)
            {
                int gridIndex = x + y * tilesPerRow;
                int index = (x + scrollOffsetX) + (y + scrollOffsetY) * map!.Width;
                var backgroundTileIndex = map.BackgroundLayer[index];
                var objectTileIndex = map.ObjectLayer[index];
                var foregroundTileIndex = map.ForegroundLayer[index];

                if (backgroundTileIndex != 0)
                {
                    CreateTileSprite(Layer.MapBackground, underlay, gridIndex, OffsetX + x * TileWidth, OffsetY + y * TileHeight, backgroundTileIndex);
                }
                else if (underlay.TryGetValue(gridIndex, out var underlaySprite))
                {
                    underlaySprite.Visible = false;
                }

                if (foregroundTileIndex != 0)
                {
                    CreateTileSprite(Layer.Objects, objects, gridIndex, OffsetX + x * TileWidth, OffsetY + y * TileHeight, objectTileIndex, 2 * RenderOrderOffset);
                }
                else if (objects.TryGetValue(gridIndex, out var objectSprite))
                {
                    objectSprite.Visible = false;
                }

                if (foregroundTileIndex != 0)
                {
                    CreateTileSprite(Layer.MapForeground, overlay, gridIndex, OffsetX + x * TileWidth, OffsetY + y * TileHeight, foregroundTileIndex);
                }
                else if (overlay.TryGetValue(gridIndex, out var overlaySprite))
                {
                    overlaySprite.Visible = false;
                }
            }
        }

        /*if (scrollOffsetX < MinScrollX)
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

		var playerPosition = game!.State.PlayerPosition;
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
			player.TextureOffset = renderLayer.Config.Texture!.GetOffset(tileGraphicOffset + tileInfo.ImageIndex + (int)game.State.TravelType * 4 + (int)game.State.PlayerDirection);
			player.Position = new(OffsetX + (playerPosition.X - scrollOffsetX) * TileWidth, OffsetY + (playerPosition.Y - scrollOffsetY) * TileHeight);
		}

		player!.Visible = playerVisible;*/
    }

	private void InitPlayer()
	{
		/*var renderLayer = game!.GetRenderLayer(Layer.Map2D);
		var tileset = tilesets![map!.TilesetIndex - 1];
		var tileInfo = tileset!.Tiles[tileset.PlayerSpriteIndex - 1];
		var playerPositon = game.State.PlayerPosition;
		player = renderLayer.SpriteFactory!.Create();

		player.TextureOffset = renderLayer.Config.Texture!.GetOffset(tileGraphicOffset + tileInfo.ImageIndex);
		player.Position = new(OffsetX + playerPositon.X * TileWidth, OffsetY + playerPositon.Y * TileHeight);
		player.PaletteIndex = game.PaletteIndexProvider.GetTilesetPaletteIndex(map.TilesetIndex);
		player.Size = new(TileWidth, TileHeight);
		player!.Visible = true;

		moveX = 0;
		moveY = 0;*/
	}

	private void ClearMap()
	{
		underlay.Values.ToList().ForEach(tile => tile.Visible = false);
		underlay.Clear();
		overlay.Values.ToList().ForEach(tile => tile.Visible = false);
		overlay.Clear();
	}

	private IAnimatedSprite CreateTileSprite(Layer layer, Dictionary<int, IAnimatedSprite> mapLayer, int gridIndex, int x, int y, int index, int baseLineOffset = 0)
	{
		var renderLayer = game!.GetRenderLayer(layer);

		if (!mapLayer.TryGetValue(gridIndex, out var tileSprite))
		{
			tileSprite = renderLayer.SpriteFactory!.CreateAnimated();
			tileSprite.Position = new(x, y);
			tileSprite.Size = new(TileWidth, TileHeight);
			tileSprite.Opaque = mapLayer == underlay;
			mapLayer.Add(gridIndex, tileSprite);
		}

		var tileInfo = GetTile(index);

		tileSprite.FrameCount = Math.Max(1, (int)tileInfo.FrameCount);
		tileSprite.TextureOffset = renderLayer.Config.Texture!.GetOffset(tileInfo.ImageIndex);
		tileSprite.PaletteIndex = 0; // TODO
		tileSprite.BaseLineOffset = baseLineOffset;
		tileSprite.Visible = true;

		return tileSprite;
	}

	private Tile GetTile(int index)
	{
		return tileset!.Tiles[index - 1];
	}

	/*private int GetTicksPerStep() => map!.Flags.HasFlag(MapFlags.Wilderness) ? TicksPerStep[game!.State.TravelType] : CityTicksPerStep;

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
		var playerPosition = game!.State.PlayerPosition;
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

            game.PlaySong(worldMap!.SongIndex);
        }
		else
		{
            if (Map.SongIndex != 0)
                game.PlaySong(1 + Map.SongIndex);

            game.State.MapIndex = index;
			worldMap = null;
		}

		tileGraphicOffset = map.TilesetIndex == 1 ? 0 : tilesets![0].Graphics.Count + 1;
		palette = game.PaletteIndexProvider.GetTilesetPaletteIndex(map.TilesetIndex);
		
		game.State.SetIsWorldMap(isWorldMap);
		game.State.TravelType = TravelType.Walk; // TODO: is it possible to change map with travel type (always reset to walk for non-world maps though!)
		game.Cursor.PaletteIndex = palette;
		RequestButtonGridPaletteUpdate();
	}*/

	private void SpawnMonster(Position position, Direction direction, uint monsterIndex)
	{
		mapActors.Add(new MapMonster(game!, this, monsterIndex, position, direction));
	}
}
