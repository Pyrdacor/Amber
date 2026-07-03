using Amber.Common;
using Amber.Renderer.Common;
using AmberIsland.Game.UI;
using AmberIsland.GameData;

namespace AmberIsland.Game.Map;

internal class MapScreen : Screen
{
	internal const int TileWidth = 32;
    internal const int TileHeight = 32;
    const int TilesPerRow = (Game.VirtualScreenWidth + TileWidth - 1) / TileWidth;
    const int TileRows = (Game.VirtualScreenHeight + TileHeight - 1) / TileHeight;
    const int OffsetX = 0;
	const int OffsetY = 0;
	const int RenderOrderOffset = TileHeight / 4;
	internal static readonly double DiagonalDistance = Math.Sqrt(2);
	DamageTextManager? damageTextManager;
	Game? game;
	GameData.Map? map;
	PathFinder? pathfinder;
	PathFollower? pathFollower;
	MapMonster? targetMonster;
	uint mapIndex = 0;
	Tileset? tileset;
	//WorldMap? worldMap;
	//ITileset[]? tilesets;
	readonly Dictionary<int, IAnimatedSprite> underlay = [];
    readonly Dictionary<int, IAnimatedSprite> objects = [];
    readonly Dictionary<int, IAnimatedSprite> overlay = [];
	readonly List<MapActor> mapActors = [];
	int lastScrollX = 0;
	int lastScrollY = 0;
	int tileGraphicOffset = 0;
	float terrainSpeedFactor = 1.0f;
    float buffSpeedFactor = 1.0f;

	long lastPlayerAttackTicks = 0;
	long currentTicks = 0;
	bool screenPushPlayerWasVisible = false;
	byte palette = 0;
	long delayedMoveActionIndex = -1;
	//IRenderText? mapNameText;
	Dictionary<ActorType, Dictionary<uint, byte[]>> actorPaletteIndices = [];

    public override ScreenType Type { get; } = ScreenType.Map2D;
	public GameData.Map Map => map!;

	private Position MapOffset => new(OffsetX + lastScrollX, OffsetX + lastScrollY);

	private float PlayerMoveSpeed => game!.Player.MoveSpeed * terrainSpeedFactor * buffSpeedFactor;

    internal void MapChanged()
	{
		//LoadMap(game!.State.MapIndex);
        //ShowMapName();
        //AfterMove();

        var monsterSprites = game!.GameData.GetMonsterAtlasSprites(mapIndex);
        var (monsterAtlas, monsterPalette) = game.CreateGraphicAtlasAndPalette(monsterSprites);

		var layer = game.GetRenderLayer(Layer.Monsters);
		layer.Config = layer.Config with
		{
			Texture = monsterAtlas,
			Palette = monsterPalette
        };

        var projectileSprites = game!.GameData.GetProjectileAtlasSprites(mapIndex);
        var (projectileAtlas, projectilePalette) = game.CreateGraphicAtlasAndPalette(projectileSprites);

        layer = game.GetRenderLayer(Layer.Projectiles);
        layer.Config = layer.Config with
        {
            Texture = projectileAtlas,
            Palette = projectilePalette
        };

        actorPaletteIndices[ActorType.Monster] = monsterSprites.Sprites.ToDictionary(sprite => sprite.Key, sprite => sprite.Value.PaletteIndices);
        actorPaletteIndices[ActorType.Projectile] = projectileSprites.Sprites.ToDictionary(sprite => sprite.Key, sprite => sprite.Value.PaletteIndices);
    }

	public override void Init(Game game)
	{
		this.game = game;
        damageTextManager = new(game);
        //tilesets = [game.AssetProvider.TilesetLoader.LoadTileset(1), game.AssetProvider.TilesetLoader.LoadTileset(2)];

        // TODO
        mapIndex = 1;
        map = game.GameData.GetMap(mapIndex)!;
        pathfinder = new(map.Width, map.Height, pos => !game.IsTileBlocking(map, pos.X, pos.Y, ActorType.Player, game.Player.TravelType));
        tileset = game.GameData.GetTileset(1);
		MapChanged();
		FillMap(0, 0, true);
		mapActors.Add(game.Player);
		SpawnMonster(new Position(100, 100), Direction.Down, 1);
        //SpawnMonster(new Position(120, 40), Direction.Left, 2);
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

		currentTicks = 0;

		SetLayout();
		//LoadMap(game.State.MapIndex);
        ShowMapName();
		EnterTile(game.Player.GetCurrentTile(), true);
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
		game!.Player.CurrentState = PlayerState.Idle;
		pathFollower = null;
    }

	private void StartAttacking()
	{
        ResetMovement();
		game!.Player.CurrentState = PlayerState.SwingingForth; // TODO
    }

    public override void Update(Game game, long elapsedTicks)
	{
		if (game.Paused || !game.InputEnabled)
			ResetMovement();

		if (elapsedTicks == 0) // This includes game.Paused already
			return;

		damageTextManager?.Update(elapsedTicks);

        int tilesPerRow = Math.Min(1 + TilesPerRow, (int)map!.Width);
        int tileRows = Math.Min(1 + TileRows, (int)map!.Height);
		var mapOffset = MapOffset;
        var mapArea = new Rect(mapOffset.X, mapOffset.Y, tilesPerRow * TileWidth, tileRows * TileHeight);

		foreach (var mapActor in mapActors.ToArray())
		{
			mapActor.Update(mapArea, elapsedTicks);
		}

        if (targetMonster != null)
        {
            var targetTile = targetMonster.GetCurrentTile();
            var distance = game.Player.GetCurrentTile().DistanceTo(targetTile);

            if (distance <= game.Player.AttackRange)
            {
				// In attack range, so start attacking.
				StartAttacking();
            }
        }

        currentTicks += elapsedTicks;

		if (pathFollower != null)
		{
			if (pathFollower.Finished)
			{
				ResetMovement();
				pathFollower = null;
			}
			else
			{
				var lastPlayerTile = game.Player.GetCurrentTile();
				var lastPosition = game.Player.Center.Round();
                pathFollower.Speed = PlayerMoveSpeed;
				Vector direction = game.Player.Direction;
				game.Player.Center = pathFollower.Update(game.Player.Center, (float)Game.TicksToSeconds(elapsedTicks), ref direction);
				game.Player.Direction = direction;

				var currentPlayerTile = game.Player.GetCurrentTile();

                if (lastPlayerTile != currentPlayerTile)
				{
					EnterTile(currentPlayerTile, false);
				}

				var currentPosition = game.Player.Center.Round();

				if (currentPosition != lastPosition)
				{
					FillMap(currentPosition.X - TilesPerRow / 2 * TileWidth, currentPosition.Y - TileRows / 2 * TileHeight);
                }
			}
		}
	}

	public override void KeyDown(Key key, KeyModifiers keyModifiers)
	{
		if (key == Key.Space && game?.Player.CurrentState == PlayerState.Walking)
			game.Player.CurrentState = PlayerState.Running;
	}

	public override void KeyUp(Key key, KeyModifiers keyModifiers)
	{
        if (key == Key.Space && game?.Player.CurrentState == PlayerState.Running)
            game.Player.CurrentState = PlayerState.Walking;
    }

	public override void MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		if (game == null)
			return;

		if (buttons == MouseButtons.Left)
		{
			var mapArea = new Rect(OffsetX, OffsetY, TilesPerRow * TileWidth, TileRows * TileHeight);

			if (mapArea.Contains(position))
			{
                // TODO: Check for item pickup		
                // TODO: Check for attack
				foreach (var monster in GetActorsOnScreen().OfType<MapMonster>())
				{
					if (monster.CollisionArea.Contains(position))
                    {
                        targetMonster = monster;
                        break;
                    }
                }

                // Move
				var targetTile = new Position((position.X - OffsetX + lastScrollX) / TileWidth, (position.Y - OffsetY + lastScrollY) / TileHeight);
                FindPathToTarget(targetTile, new Position(position.X - OffsetX + lastScrollX, position.Y - OffsetY + lastScrollY));
                return;
			}
        }
		else if (buttons == MouseButtons.Right)
		{
			ResetMovement();
		}

        base.MouseDown(position, buttons, keyModifiers);
    }

	private void FindPathToTarget(Position targetTile, Position exactPosition)
	{
        var startTile = game!.Player.GetCurrentTile();
        var tilePath = pathfinder?.FindPath(startTile, targetTile);

        if (tilePath?.Count is > 0)
        {
            ResetMovement();

            game.Player.CurrentState = game.IsKeyDown(Key.Space) ? PlayerState.Running : PlayerState.Walking;

            pathFollower = new(tilePath, TileWidth, TileHeight, PlayerMoveSpeed, exactPosition);
        }
    }

	public override void MouseUp(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		base.MouseUp(position, buttons, keyModifiers);		
	}

	public override void MouseMove(Position position, MouseButtons buttons)
	{
		base.MouseMove(position, buttons);
	}

	private void FillMap(int scrollOffsetX, int scrollOffsetY, bool force = false)
	{
        int tilesPerRow = Math.Min(1 + TilesPerRow, (int)map!.Width);
		int tileRows = Math.Min(1 + TileRows, (int)map!.Height);

        scrollOffsetX = Math.Clamp(scrollOffsetX, 0, (map.Width - tilesPerRow) * TileWidth);
        scrollOffsetY = Math.Clamp(scrollOffsetY, 0, (map.Height - tileRows) * TileHeight);

		if (!force && scrollOffsetX == lastScrollX && scrollOffsetY == lastScrollY)
			return; // nothing to do

		lastScrollX = scrollOffsetX;
		lastScrollY = scrollOffsetY;

        for (int y = 0; y < tileRows; y++)
        {
            for (int x = 0; x < tilesPerRow; x++)
            {
                int gridIndex = x + y * tilesPerRow;
                int index = (x + scrollOffsetX / TileWidth) + (y + scrollOffsetY / TileHeight) * map!.Width;
                var backgroundTileIndex = map.BackgroundLayer[index];
                var objectTileIndex = map.ObjectLayer[index];
                var foregroundTileIndex = map.ForegroundLayer[index];

                if (backgroundTileIndex != 0)
                {
                    CreateTileSprite(Layer.MapBackground, underlay, gridIndex, OffsetX + x * TileWidth - scrollOffsetX % TileWidth, OffsetY + y * TileHeight - scrollOffsetY % TileHeight, backgroundTileIndex);
                }
                else if (underlay.TryGetValue(gridIndex, out var underlaySprite))
                {
                    underlaySprite.Visible = false;
                }

                if (objectTileIndex != 0)
                {
                    CreateTileSprite(Layer.Objects, objects, gridIndex, OffsetX + x * TileWidth - scrollOffsetX % TileWidth, OffsetY + y * TileHeight - scrollOffsetY % TileHeight, objectTileIndex, 2 * RenderOrderOffset);
                }
                else if (objects.TryGetValue(gridIndex, out var objectSprite))
                {
                    objectSprite.Visible = false;
                }

                if (foregroundTileIndex != 0)
                {
                    CreateTileSprite(Layer.MapForeground, overlay, gridIndex, OffsetX + x * TileWidth - scrollOffsetX % TileWidth, OffsetY + y * TileHeight - scrollOffsetY % TileHeight, foregroundTileIndex);
                }
                else if (overlay.TryGetValue(gridIndex, out var overlaySprite))
                {
                    overlaySprite.Visible = false;
                }
            }
        }
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
			tileSprite.Size = new(TileWidth, TileHeight);
			tileSprite.TextureSize = new(16, 16);
			tileSprite.Opaque = mapLayer == underlay;
			mapLayer.Add(gridIndex, tileSprite);
		}

		var tileInfo = GetTile(index);

        tileSprite.Position = new(x, y);
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

	internal void SpawnMonster(Position position, Direction direction, uint monsterIndex)
	{
		mapActors.Add(new MapMonster(game!, this, monsterIndex, position, direction));
	}

    internal void SpawnProjectile(MapActor source, Position position, Vector direction, uint projectileIndex)
    {
        mapActors.Add(new MapProjectile(game!, this, source, projectileIndex, position, direction));
    }

	internal void RemoveActor(MapActor actor)
	{
		mapActors.Remove(actor);
	}

    private IEnumerable<MapActor> GetActorsOnScreen() => mapActors.Where(actor => actor.VisibleOnMap);

    internal byte[] GetMonsterPaletteIndices(uint monsterIndex) => actorPaletteIndices[ActorType.Monster][monsterIndex];
    internal byte[] GetProjectilePaletteIndices(uint projectileIndex) => actorPaletteIndices[ActorType.Projectile][projectileIndex];

	internal void ShowDamageText(MapActor source, string text, TextColor color)
	{
		var area = source.Area;
		var position = new Position(area.Center.X, area.Top) - MapOffset;

		damageTextManager?.Spawn(position, text, color);
	}

	private void EnterTile(Position position, bool scrollMapTo)
	{
		var tileInfo = game!.GetTileInfo(map!, position.X, position.Y);

		terrainSpeedFactor = tileInfo.Type.SpeedFactor(game.Player.VisualDirection);

		if (scrollMapTo)
			FillMap((position.X - TilesPerRow / 2) * TileWidth, (position.Y - TileRows / 2) * TileHeight, true);

		// TODO: map events

		if (targetMonster != null)
		{
			var targetTile = targetMonster.GetCurrentTile();
			var distance = game.Player.GetCurrentTile().DistanceTo(targetTile);

			if (distance > game.Player.VisionRange)
			{
                // Lost sight of the monster, so stop chasing it.
                targetMonster = null;
			}
            else if (distance > game.Player.AttackRange)
			{
				// Not in attack range, so chase the monster.
				FindPathToTarget(targetTile, targetMonster.Center.Round());
			}
		}
    }
}
