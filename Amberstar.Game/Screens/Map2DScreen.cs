using System.Reflection.Metadata.Ecma335;
using Amber.Common;
using Amber.Renderer.Common;
using Amberstar.Game.Events;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Events;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

// TODO: Shadows (see Do_shadow)
internal class Map2DScreen : ButtonGridScreen
{
	class WorldMap : IMap2D
	{
		const int WorldMapSong = 2;
		IMap2D[] maps = [];
		readonly Dictionary<int, IMap2D> mapCache = [];
		List<IEvent> events = [];
		Tile2D[] tiles = [];

		IMap IMapEventProvider.Map => this;

		public int Index => maps[0].Index;

        public int Width => WorldMapWidth * 2;

		public int Height => WorldMapHeight * 2;

		public MapType Type => MapType.Map2D;

		public MapFlags Flags => maps[0].Flags;

		public string Name => maps[0].Name;

        public int SongIndex => WorldMapSong;

        public MapCharacter[] Characters => [];

		public Position[][] CharacterPositions => [];

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

		public IEvent? GetEvent(Game Game, int x, int y, bool onlyActive = true)
		{
			return GetEventWithIndex(Game, x, y, onlyActive)?.Event;
        }

        public (int Index, IEvent Event)? GetEventWithIndex(Game Game, int x, int y, bool onlyActive = true)
        {
            int eventIndex;
            IMap2D map;

            static int AdjustEventIndex(int eventIndex, int offset)
            {
                if (eventIndex == 0)
                    return 0;

                return offset + eventIndex;
            }

            if (x < WorldMapWidth)
            {
                if (y < WorldMapHeight)
                {
                    map = maps[0];
                    eventIndex = maps[0].Tiles[x + y * WorldMapWidth].Event;
                }
                else
                {
                    map = maps[2];
                    eventIndex = AdjustEventIndex(maps[2].Tiles[x + (y - WorldMapHeight) * WorldMapWidth].Event, 2 * IMap.EventCount);
                }
            }
            else
            {
                if (y < WorldMapHeight)
                {
                    map = maps[1];
                    eventIndex = AdjustEventIndex(maps[1].Tiles[x - WorldMapWidth + y * WorldMapWidth].Event, 1 * IMap.EventCount);
                }
                else
                {
                    map = maps[3];
                    eventIndex = AdjustEventIndex(maps[3].Tiles[x - WorldMapWidth + (y - WorldMapHeight) * WorldMapWidth].Event, 3 * IMap.EventCount);
                }
            }

            if (eventIndex == 0)
                return null;

            if (onlyActive && !Game.IsEventActive(map, eventIndex, events[eventIndex - 1]))
                return null;

            return (eventIndex, events[eventIndex - 1]);
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
	const long OuchDisplayTime = Game.TicksPerSecond / 6;
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
	const int MapViewWidth = TilesPerRow * TileWidth;
	const int MapViewHeight = TileRows * TileHeight;
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
	IMap2D? map;
	WorldMap? worldMap;
	ITileset[]? tilesets;
	readonly Dictionary<int, IAnimatedSprite> underlay = [];
	readonly Dictionary<int, IAnimatedSprite> overlay = [];
    readonly List<Characters.MapCharacter> characters = [];
    readonly List<ISprite> mapCharacters = [];
    ISprite? player;
    ISprite? ouchBubble;
	long ouchBubbleDeleteActionIndex = -1;
    int lastScrollX = -1;
	int lastScrollY = -1;
	int tileGraphicOffset = 0;
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
	bool mouseDown = false;
    IRenderText? mapNameText;

    public override ScreenType Type { get; } = ScreenType.Map2D;
    public override ScreenFadeType FadeType { get; } = ScreenFadeType.None;
    public IMap2D Map => map!;

    internal override byte ButtonGridPaletteIndex => palette;

    internal void MapChanged()
	{
		LoadMap(Game.State.MapIndex);
        ShowMapName();
        AfterMove();
	}

	public override void Init()
	{
		base.Init();

		tilesets = [Game.AssetProvider.TilesetLoader.LoadTileset(1), Game.AssetProvider.TilesetLoader.LoadTileset(2)];

		timeText = Game.TextManager.Create($"{Game.State.Hour:00}:{Game.State.Minute:00}", 15, -1, palette);
		timeText.Show(220, 70, 100);

		Game.Time.MinuteChanged += () =>
		{
			timeText?.Delete();

			if (Game.ScreenHandler.ActiveScreen?.Type != ScreenType.Map2D)
				return;

			timeText = Game.TextManager.Create($"{Game.State.Hour:00}:{Game.State.Minute:00}", 15, -1, palette);
			timeText.Show(220, 70, 100);
		};
	}

	public override void ScreenPushed(Screen screen)
	{
		base.ScreenPushed(screen);

		// Don't move any further
		ResetMovement();

		if (!screen.Transparent)
		{
			underlay.Values.ToList().ForEach(tile => tile.Visible = false);
			overlay.Values.ToList().ForEach(tile => tile.Visible = false);
			screenPushPlayerWasVisible = player!.Visible;
			player!.Visible = false;
            mapNameText!.Visible = false;
        }

		timeText?.Delete();

		Game.Pause();
	}

	public override void ScreenPopped(Screen screen)
	{
		if (!screen.Transparent)
		{
			SetLayout();
            underlay.Values.ToList().ForEach(tile => tile.Visible = true);
			overlay.Values.ToList().ForEach(tile => tile.Visible = true);
			player!.Visible = screenPushPlayerWasVisible;
            mapNameText!.Visible = true;
			AfterMove(ignoreEvents: true);
        }

		timeText?.Delete();
		timeText = Game.TextManager.Create($"{Game.State.Hour:00}:{Game.State.Minute:00}", 15, -1, palette);
		timeText.Show(220, 70, 100);

		base.ScreenPopped(screen);

		Game.Resume();
	}

	public override void Open(Action? closeAction)
	{
        base.Open(closeAction);

        moveTickCounter = 0;
		lastMoveStartTicks = 0;
		currentTicks = 0;
		additionalMoveRequested = false;
		mouseDown = false;

        Game.Time.MinuteChanged += MinuteChanged;

		SetLayout();
		LoadMap(Game.State.MapIndex);
        ShowMapName();
        InitPlayer();
		AfterMove();
	}

    private void MinuteChanged()
    {
        if (Game.Paused)
            return;

        foreach (var character in characters)
        {
            character.Update(Game);
        }

		FillMap(lastScrollX, lastScrollY, true);
    }

    private void SetLayout()
	{
        Game.SetLayout(Layout.Map2D, palette);
    }

    private void ShowMapName()
    {
		mapNameText?.Delete();
        mapNameText = Game.TextManager.Create(map!.Name, 15, TextManager.TransparentPaper, palette);
        mapNameText.ShowInArea(OffsetX, OffsetY - mapNameText.LineHeight - 3, TilesPerRow * TileWidth, TileRows * TileHeight, 100, TextAlignment.Center);
    }

    protected override void ButtonClicked(int index)
	{
		if (Game.ButtonLayout == ButtonLayout.Movement)
		{
			if (index == 4)
			{
				Game.Time.Tick();
				return;
			}

			int moveX = index % 3 - 1;
			int moveY = index / 3 - 1;

			if (moveY < 0)
				Game.State.PartyDirection = Direction.Up;
			else if (moveY > 0)
				Game.State.PartyDirection = Direction.Down;
			else if (moveX < 0)
				Game.State.PartyDirection = Direction.Left;
			else if (moveX > 0)
				Game.State.PartyDirection = Direction.Right;

			if (currentTicks - lastMoveStartTicks >= GetTicksPerStep())
			{
				lastMoveStartTicks = currentTicks;
				if (MovePlayer(moveX, moveY))
					AfterMove();
			}
		}
		else // Actions
		{
			var buttonType = GetButtonType(index);

			Rect CreateActionCursorTrapArea(bool mouth)
			{
                var playerPosition = Game.State.PartyPosition;
                var playerRenderPosition = new Position(OffsetX + (playerPosition.X - lastScrollX) * TileWidth, OffsetY + (playerPosition.Y - lastScrollY) * TileHeight);

				int range = mouth ? 2 : 1;

				var startPosition = new Position
				(
					MathUtil.Limit(OffsetX + 7, playerRenderPosition.X - (range - 1) * TileWidth - TileWidth / 2, OffsetX + MapViewWidth - 1),
					MathUtil.Limit(OffsetY + 7, playerRenderPosition.Y - (range - 1) * TileHeight - TileHeight / 2, OffsetY + MapViewHeight - 1)
				);
				var size = new Size
				(
					Math.Min((range * 2) * TileWidth, OffsetX + MapViewWidth - startPosition.X - 8),
					Math.Min((range * 2) * TileHeight, OffsetY + MapViewHeight - startPosition.Y - 8)
				);

				return new Rect(startPosition, size);
            }

			switch (buttonType)
			{
				case ButtonType.Eye:
					Game.Cursor.CursorType = CursorType.Eye;
					Game.TrapMouse(CreateActionCursorTrapArea(mouth: false));
                    break;
                case ButtonType.Ear:
                    // TODO: check for something to hear
                    //Game.Cursor.CursorType = CursorType.Ear;
                    //Game.TrapMouse(CreateActionCursorTrapArea(mouth: false));
                    Game.ShowTextMessage(Message.ListenNothingHeard);
                    break;
                case ButtonType.Mouth:
                    Game.Cursor.CursorType = CursorType.Mouth;
                    Game.TrapMouse(CreateActionCursorTrapArea(mouth: true));
                    break;
				case ButtonType.Disk:
					// TODO: Show options
					Game.Quit();
					break;
                // TODO
            }
		}
	}

	protected override void SetupButtons(ButtonGrid buttonGrid)
	{
		if (Game.ButtonLayout == ButtonLayout.Movement)
		{
			// Upper row
			buttonGrid.SetButton(0, ButtonType.ArrowUpLeft);
			buttonGrid.SetButton(1, ButtonType.ArrowUp);
			buttonGrid.SetButton(2, ButtonType.ArrowUpRight);
			// Middle row
			buttonGrid.SetButton(3, ButtonType.ArrowLeft);
			buttonGrid.SetButton(4, ButtonType.Sleep);
			buttonGrid.SetButton(5, ButtonType.ArrowRight);
			// Lower row
			buttonGrid.SetButton(6, ButtonType.ArrowDownLeft);
			buttonGrid.SetButton(7, ButtonType.ArrowDown);
			buttonGrid.SetButton(8, ButtonType.ArrowDownRight);

			bool enableMoveButtons = Game.State.ActivePartyMember?.CanMove(inBattle: false) ?? false;

            for (int i = 0; i< 9; i++)
			{
				if (i != 4)
					buttonGrid.EnableButton(i, enableMoveButtons);
			}
		}
		else // Actions
		{
			// Upper row
			buttonGrid.SetButton(0, ButtonType.Eye);
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

	public override void Close()
	{
		// Don't move any further
		ResetMovement();

        Game.Time.MinuteChanged -= MinuteChanged;

        ClearMap();
		player!.Visible = false;
		player = null;
        characters.Clear();

		mapCharacters.ForEach(c => c.Visible = false);
		mapCharacters.Clear();

        if (ouchBubble != null)
		{
			ouchBubble.Visible = false;
			ouchBubble = null;
        }

		timeText?.Delete();
        mapNameText!.Delete();

        base.Close();
	}

	private void ResetMovement()
	{
		moveX = 0;
		moveY = 0;
		mouseDown = false;
		Game.DeleteDelayedActions(delayedMoveActionIndex);
	}

	public override void Update(long elapsedTicks)
	{
		if (Game.Paused || !Game.InputEnabled)
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
		var oldPosition = Game.State.PartyPosition;
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
                    @event.Type == EventType.Door ||
                    @event.Type == EventType.Place ||
                    @event.Type == EventType.TravelExit ||
					(@event.Type == EventType.WindGate && Game.State.HasWindChain))
					return false;
			}

			var targetTile = map!.Tiles[x + y * map.Width];
			
			if (targetTile.Overlay != 0)
			{
                var flags = GetTileInfo(targetTile.Overlay).Flags;

				if (!flags.HasFlag(TileFlags.UnderlayHasPriority))
					return BlocksMovement(flags);
            }

			if (targetTile.Underlay == 0)
				return false;

			return BlocksMovement(GetTileInfo(targetTile.Underlay).Flags);
		}

		bool BlocksMovement(TileFlags flags)
		{
			return flags.HasFlag(TileFlags.BlockAllMovement) || !flags.HasFlag((TileFlags)(1 << (8 + (int)Game.State.TravelType)));
		}

		if (TileBlocksMovement(newX, newY))
		{
			if (newY != oldPosition.Y && !TileBlocksMovement(oldPosition.X, newY))
			{
				// only move in y direction
				Game.State.SetPartyPosition(oldPosition.X, newY);
				return true;
			}
			else if (newX != oldPosition.X && !TileBlocksMovement(newX, oldPosition.Y))
			{
				// only move in x direction
				Game.State.SetPartyPosition(newX, oldPosition.Y);
				return true;
			}
			else
			{
				// stop as we hit an obstacle
				ShowOuchBubble();
				return false;
			}
		}

		// Can move
		Game.State.SetPartyPosition(newX, newY);
		return true;
	}

	private void AfterMove(bool ignoreEvents = false)
	{
		if (worldMap != null)
			UpdateWorldMap();

		var playerPosition = Game.State.PartyPosition;

		Game.Time.Moved2D();

        FillMap(playerPosition.X - TilesPerRow / 2, playerPosition.Y - TileRows / 2, true);

		if (Game.Cursor.CursorType != CursorType.Disk)
			Game.SimulateMouseMoveWithoutButton(); // Update cursor (user might move by keys)

        // Check for events
        if (!ignoreEvents)
			TryExecuteMapEvent(EventTrigger.Move);
    }

	private bool TryExecuteMapEvent(EventTrigger trigger, int x = 0, int y = 0)
	{
		if (x == 0)
		{
            (x, y) = Game.State.PartyPosition;
        }

        var eventWithIndex = GetEventWithIndex(x, y);

        if (eventWithIndex != null)
        {
			(int eventIndex, IEvent @event) = eventWithIndex.Value;
            var mapEvent = Event.CreateEvent(@event, eventIndex);

            if (mapEvent is IPlaceEvent ||
				mapEvent is IAltarEvent)
            {
				ResetPartyPosition();
            }

			Game.EventHandler.HandleEvent(trigger, mapEvent, map!);

			return true;
        }

		return false;
    }

	internal void ResetPartyPosition()
	{
        Game.State.ResetPartyPosition();

        if (worldMap != null)
            UpdateWorldMap();
    }

	private void UpdateMovement()
	{
		bool left = Game.IsKeyDown(Key.Left) || Game.IsKeyDown('A');
		bool right = Game.IsKeyDown(Key.Right) || Game.IsKeyDown('D');
		bool up = Game.IsKeyDown(Key.Up) || Game.IsKeyDown('W');
		bool down = Game.IsKeyDown(Key.Down) || Game.IsKeyDown('S');
		bool upLeft = Game.IsKeyDown('Q');
		bool upRight = Game.IsKeyDown('E');
		bool downLeft = Game.IsKeyDown('Y') || Game.IsKeyDown('Z');
		bool downRight = Game.IsKeyDown('C');

		if (mouseDown && Game.InputEnabled && !Game.Paused)
		{
			switch (Game.Cursor.CursorType)
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
				default:
					return;
			}
		}

		if (Game.ButtonLayout == ButtonLayout.Movement)
		{
			if (!left)
				left = Game.IsKeyDown(Key.Keypad4);
			if (!right)
				right = Game.IsKeyDown(Key.Keypad6);
			if (!up)
				up = Game.IsKeyDown(Key.Keypad8);
			if (!down)
				down = Game.IsKeyDown(Key.Keypad2);
			if (!upLeft)
				upLeft = Game.IsKeyDown(Key.Keypad7);
			if (!upRight)
				upRight = Game.IsKeyDown(Key.Keypad9);
			if (!downLeft)
				downLeft = Game.IsKeyDown(Key.Keypad1);
			if (!downRight)
				downRight = Game.IsKeyDown(Key.Keypad3);
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
			if (moveX != 0 || moveY != 0)
			{
				long timeTillNextMove = Math.Max(0, GetTicksPerStep() - (currentTicks - lastMoveStartTicks));
				int x = moveX;
				int y = moveY;
				Game.DeleteDelayedActions(delayedMoveActionIndex);
				delayedMoveActionIndex = Game.AddDelayedAction(timeTillNextMove, () =>
				{
					lastMoveStartTicks = currentTicks;
					if (MovePlayer(x, y))
						AfterMove();
					moveTickCounter = 0;
				});
			}

			additionalMoveRequested = false;
		}
		else if (!additionalMoveRequested)
		{
			additionalMoveRequested = (currentTicks - lastMoveStartTicks) < GetTicksPerStep();
		}

		bool wasMovingBefore = moveX != 0 || moveY != 0 || additionalMoveRequested;

		if (left && !right)
		{
			Game.State.PartyDirection = Direction.Left;
			moveX = -1;
		}
		else if (right && !left)
		{
			Game.State.PartyDirection = Direction.Right;
			moveX = 1;
		}
		else
		{
			moveX = 0;
		}

		if (up && !down)
		{
			Game.State.PartyDirection = Direction.Up;
			moveY = -1;
		}
		else if (down && !up)
		{
			Game.State.PartyDirection = Direction.Down;
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

	public override bool KeyDown(Key key, KeyModifiers keyModifiers)
	{
		if (base.KeyDown(key, keyModifiers))
			return true;

		if (Game.Cursor.CursorType < CursorType.Eye || Game.Cursor.CursorType > CursorType.Ear)
			UpdateMovement();

		return true;
	}

	public override bool KeyUp(Key key, KeyModifiers keyModifiers)
	{
        if (base.KeyUp(key, keyModifiers))
            return true;

        UpdateMovement();

        return true;
    }

	private Position MousePositionToMapTilePosition(Position mousePosition)
	{
		if (map == null)
			return new();

		int x = lastScrollX + MathUtil.Limit(0, (mousePosition.X - OffsetX) / TileWidth, map.Width - 1);
		int y = lastScrollY + MathUtil.Limit(0, (mousePosition.Y - OffsetY) / TileHeight, map.Height - 1);

		return new(x, y);
    }

	public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		if (buttons == MouseButtons.Right)
		{
			if (Game.Cursor.CursorType.ForcesMouseTrap())
			{
                Game.Cursor.CursorType = CursorType.Sword;
                Game.UntrapMouse();
				return true;
            }
            else if (ButtonGrid.Area.Contains(position))
			{
				Game.ButtonLayout = (ButtonLayout)(1 - (int)Game.ButtonLayout); // toggle
				RequestButtonSetup();
                return true;
            }
			else
			{
				int? characterSlotIndex = Game.TestPartyPortraitHit(position);

				if (characterSlotIndex != null)
				{
					Game.OpenInventory(characterSlotIndex.Value);
					return true;
				}
            }
		}
		else
		{
			mouseDown = true;
			var mapArea = new Rect(OffsetX, OffsetY, TilesPerRow * TileWidth, TileRows * TileHeight);

			if (mapArea.Contains(position))
			{
				if (Game.Cursor.CursorType == CursorType.Zzz)
				{
					Game.Time.Tick();
					return true;
				}
				else if (Game.Cursor.CursorType >= CursorType.Eye && Game.Cursor.CursorType <= CursorType.Ear)
				{
					var eventTrigger = Game.Cursor.CursorType.ToEventTrigger();
					var (x, y) = MousePositionToMapTilePosition(position);

					Game.Cursor.CursorType = CursorType.Sword;
                    Game.UntrapMouse();

                    var character = characters.FirstOrDefault(character => character.Position.X == x && character.Position.Y == y);

                    if (character != null && character.Type == MapCharacterType.Person)
                    {
                        Game.State.CurrentConversationCharacter = new(character.CharacterIndex, character.Map, character.Index);
                        Game.ScreenHandler.PushScreen(ScreenType.Conversation);
                        return true;
                    }

                    TryExecuteMapEvent(eventTrigger, x, y);

					return true;
				}
				else if (Game.Cursor.CursorType >= CursorType.ArrowUp2D && Game.Cursor.CursorType <= CursorType.ArrowDownLeft2D)
				{
					UpdateMovement();
					return true;
				}
			}
        }

        return base.MouseDown(position, buttons, keyModifiers);
    }

	public override bool MouseUp(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		mouseDown = false;
		UpdateMovement();

		return base.MouseUp(position, buttons, keyModifiers);
	}

	public override void MouseMove(Position position, MouseButtons buttons)
	{
		base.MouseMove(position, buttons);

		if (Game.Cursor.CursorType != CursorType.Eye &&
			Game.Cursor.CursorType != CursorType.Ear &&
			Game.Cursor.CursorType != CursorType.Mouth)
		{
			var mapArea = new Rect(OffsetX, OffsetY, TilesPerRow * TileWidth, TileRows * TileHeight);

			if (mapArea.Contains(position))
			{
				bool left = position.X < player!.Position.X;
				bool right = position.X >= player.Position.X + 16;
				bool up = position.Y < player!.Position.Y;
				bool down = position.Y >= player.Position.Y + 16;

				var lastCursor = Game.Cursor.CursorType;

				if (up)
				{
					if (left)
						Game.Cursor.CursorType = CursorType.ArrowUpLeft2D;
					else if (right)
						Game.Cursor.CursorType = CursorType.ArrowUpRight2D;
					else
						Game.Cursor.CursorType = CursorType.ArrowUp2D;
				}
				else if (down)
				{
					if (left)
						Game.Cursor.CursorType = CursorType.ArrowDownLeft2D;
					else if (right)
						Game.Cursor.CursorType = CursorType.ArrowDownRight2D;
					else
						Game.Cursor.CursorType = CursorType.ArrowDown2D;
				}
				else
				{
					if (left)
						Game.Cursor.CursorType = CursorType.ArrowLeft2D;
					else if (right)
						Game.Cursor.CursorType = CursorType.ArrowRight2D;
					else
						Game.Cursor.CursorType = CursorType.Zzz;
				}

				if (buttons == MouseButtons.Left && lastCursor != Game.Cursor.CursorType)
					UpdateMovement();
			}
			else if (Game.Cursor.CursorType.IsArrow())
			{
				Game.Cursor.CursorType = CursorType.Sword;
				UpdateMovement();
			}
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
        Rect displayedMapPortion = new(Math.Max(0, scrollOffsetX), Math.Max(0, scrollOffsetY), TilesPerRow, TileRows);

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

		var playerPosition = Game.State.PartyPosition;
		int playerTileIndex = playerPosition.X + playerPosition.Y * map!.Width;
		var playerTile = map.Tiles[playerTileIndex];

		int GetBaseLineOffset(Tile2D tile, out bool visible)
		{
			int baseLineOffset = 3 * RenderOrderOffset; // By default draw over overlay
			visible = true;

            if (tile.Underlay != 0)
            {
                var flags = GetTileInfo(tile.Underlay).Flags;

                if (flags.HasFlag(TileFlags.PartyInvisible))
                    visible = false;
            }

            if (tile.Overlay != 0)
            {
                var flags = GetTileInfo(tile.Overlay).Flags;

                if (flags.HasFlag(TileFlags.PartyInvisible))
                    visible = false;

                if (flags.HasFlag(TileFlags.Foreground))
                    baseLineOffset = RenderOrderOffset; // draw below overlay
            }

			return baseLineOffset;
        }

		int playerBaseLineOffset = GetBaseLineOffset(playerTile, out bool playerVisible);
        var renderLayer = Game.GetRenderLayer(Layer.Map2D);
        var tileset = tilesets![map!.TilesetIndex - 1];

        for (int i = 0; i < characters.Count; i++)
		{
			var character = characters[i];
			var mapCharacter = mapCharacters[i];
            var tile = map.Tiles[character.Position.X + character.Position.Y * map.Width];
			int baseLineOffset = GetBaseLineOffset(tile, out bool visible);

            mapCharacter.Visible = visible && displayedMapPortion.Contains(character.Position);

			if (mapCharacter.Visible)
			{
				var relativePosition = character.Position - displayedMapPortion.Position;
                mapCharacter.Position = new(OffsetX + relativePosition.X * TileWidth, OffsetY + relativePosition.Y * TileHeight);
				mapCharacter.BaseLineOffset = baseLineOffset;

                var tileInfo = tileset!.Tiles[character.Icon - 1];

                mapCharacter.TextureOffset = renderLayer.Config.Texture!.GetOffset(tileGraphicOffset + tileInfo.ImageIndex); // TODO: Animation
            }
		}

		// TODO: Transports

		if (playerVisible)
		{
			var tileInfo = tileset!.Tiles[tileset.PlayerSpriteIndex - 1];
			player!.BaseLineOffset = playerBaseLineOffset;
			player.TextureOffset = renderLayer.Config.Texture!.GetOffset(tileGraphicOffset + tileInfo.ImageIndex + (int)Game.State.TravelType * 4 + (int)Game.State.PartyDirection);
			player.Position = new(OffsetX + (playerPosition.X - scrollOffsetX) * TileWidth, OffsetY + (playerPosition.Y - scrollOffsetY) * TileHeight);
		}

		player!.Visible = playerVisible;
	}

	private void InitPlayer()
	{
		var renderLayer = Game.GetRenderLayer(Layer.Map2D);
		var tileset = tilesets![map!.TilesetIndex - 1];
		var tileInfo = tileset!.Tiles[tileset.PlayerSpriteIndex - 1];
		var playerPosition = Game.State.PartyPosition;
		player = renderLayer.SpriteFactory!.Create();

		player.TextureOffset = renderLayer.Config.Texture!.GetOffset(tileGraphicOffset + tileInfo.ImageIndex);
		player.Position = new(OffsetX + playerPosition.X * TileWidth, OffsetY + playerPosition.Y * TileHeight);
		player.PaletteIndex = Game.PaletteIndexProvider.GetTilesetPaletteIndex(map.TilesetIndex);
		player.Size = new(TileWidth, TileHeight);
		player.Visible = true;

		moveX = 0;
		moveY = 0;
	}

	private void ShowOuchBubble()
	{
        Game.DeleteDelayedActions(ouchBubbleDeleteActionIndex);

        var renderLayer = Game.GetRenderLayer(Layer.UI);
        var playerPosition = Game.State.PartyPosition;
        ouchBubble = renderLayer.SpriteFactory!.Create();

        ouchBubble.TextureOffset = renderLayer.Config.Texture!.GetOffset(Game.GraphicIndexProvider.GetUIGraphicIndex(UIGraphic.SmallOuch));
        ouchBubble.Position = new(OffsetX + (playerPosition.X - lastScrollX) * TileWidth + 14, OffsetY + (playerPosition.Y - lastScrollY) * TileHeight - 10);
        ouchBubble.PaletteIndex = Game.PaletteIndexProvider.GetTilesetPaletteIndex(map!.TilesetIndex);
        ouchBubble.Size = UIGraphic.SmallOuch.GetSize();
        ouchBubble.Visible = true;

        ouchBubbleDeleteActionIndex = Game.AddDelayedAction(OuchDisplayTime, () =>
		{
			if (ouchBubble != null)
			{
				ouchBubble.Visible = false;
				ouchBubble = null;
			}
		});
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
		var renderLayer = Game.GetRenderLayer(Layer.Map2D);

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
		tileSprite.PaletteIndex = Game.PaletteIndexProvider.GetTilesetPaletteIndex(map!.TilesetIndex);
		tileSprite.BaseLineOffset = baseLineOffset;
		tileSprite.Visible = true;

		return tileSprite;
	}

	private ITile GetTileInfo(int index)
	{
		var tileset = tilesets![map!.TilesetIndex - 1];
		return tileset!.Tiles[index - 1];
	}

	private int GetTicksPerStep() => map!.Flags.HasFlag(MapFlags.Wilderness) ? TicksPerStep[Game.State.TravelType] : CityTicksPerStep;

	public static int GetWorldMapIndex(int index, int offsetX, int offsetY)
	{
		int currentX = index % WorldMapWidthInMaps;
		int currentY = index / WorldMapWidthInMaps;

		currentX = (currentX + offsetX + WorldMapWidthInMaps) % WorldMapWidthInMaps;
		currentY = (currentY + offsetY + WorldMapHeightInMaps) % WorldMapHeightInMaps;

		return currentX + currentY * WorldMapWidthInMaps;
	}

	private IEvent? GetEvent(int x, int y, bool onlyActive = true)
	{
		if (map is WorldMap worldMap)
			return worldMap.GetEvent(Game, x, y, onlyActive);
		
		var eventIndex = map!.Tiles[x + y * map.Width].Event;

		if (eventIndex == 0)
			return null;

        if (onlyActive && !Game.IsEventActive(map, eventIndex, map.Events[eventIndex - 1]))
            return null;

        return map.Events[eventIndex - 1];
	}

    private (int Index, IEvent Event)? GetEventWithIndex(int x, int y, bool onlyActive = true)
    {
        if (map is WorldMap worldMap)
            return worldMap.GetEventWithIndex(Game, x, y, onlyActive);

        var eventIndex = map!.Tiles[x + y * map.Width].Event;

        if (eventIndex == 0)
            return null;

        if (onlyActive && !Game.IsEventActive(map, eventIndex, map.Events[eventIndex - 1]))
            return null;

        return (eventIndex, map.Events[eventIndex - 1]);
    }

    private void UpdateWorldMap(int? mapIndex = null)
	{
		// The first 64 maps (index 1 to 64) are the world maps.

		int newTopLeftMapIndex = mapIndex ?? worldMap?.UpperLeftMapIndex ??
			throw new AmberException(ExceptionScope.Application, "No world map active and no map index given.");
		
		int[] mapIndices;
		var playerPosition = Game.State.PartyPosition;
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

			Game.State.SetPartyPosition(playerPosition.X, playerPosition.Y);
			bool firstTime = worldMap == null;
			worldMap ??= new WorldMap();
			worldMap.SetMaps(mapIndices.Select(GetMap).ToArray(), mapIndices);
			map = worldMap;

			Game.State.MapIndex = newTopLeftMapIndex;

			IMap2D GetMap(int index)
			{
				if (index == mapIndex && firstTime)
					return this.map!;

				var map = worldMap?.GetMapByIndex(index);

				return map ?? (Game.AssetProvider.MapLoader.LoadMap(index) as IMap2D)!;
			}
		}
	}

	private void LoadMap(int index)
	{
		lastScrollX = -1;
		lastScrollY = -1;
		map = Game.AssetProvider.MapLoader.LoadMap(index) as IMap2D; // TODO: catch exceptions
		bool isWorldMap = map!.Flags.HasFlag(MapFlags.Wilderness);

		if (isWorldMap)
		{
			UpdateWorldMap(index);

            Game.PlaySong(worldMap!.SongIndex);
        }
		else
		{
            if (Map.SongIndex != 0)
                Game.PlaySong(1 + Map.SongIndex);

            Game.State.MapIndex = index;
			worldMap = null;
        }

		tileGraphicOffset = map.TilesetIndex == 1 ? 0 : tilesets![0].Graphics.Count + 1;
		palette = Game.PaletteIndexProvider.GetTilesetPaletteIndex(map.TilesetIndex);

        for (int i = 0; i < map.Characters.Length; i++)
        {
            var characterData = map.Characters[i];

            if (characterData.Index != 0 && characterData.Icon != 0)
            {
                characters.Add(new Characters.MapCharacter(map, i, map.CharacterPositions[i], Game.State,
                    (x, y, collisionClass) => CanMoveTo(x, y, false, collisionClass)));
                var sprite = Game.CreateSprite(Layer.Map2D, new(), new(16, 16), 1, palette, false)!;
                sprite.Visible = false;
                mapCharacters.Add(sprite);
            }
        }

        Game.State.SetIsWorldMap(isWorldMap);
		Game.State.TravelType = TravelType.Walk; // TODO: is it possible to change map with travel type (always reset to walk for non-world maps though!)
		Game.Cursor.PaletteIndex = palette;
		RequestButtonGridPaletteUpdate();
	}

    private TileFlags GetTileFlags(int x, int y)
    {
		var tile = map!.Tiles[x + y * map.Width];
		var underlayFlags = tile.Underlay == 0 ? TileFlags.None : GetTileInfo(tile.Underlay).Flags;

		if (tile.Overlay == 0)
			return underlayFlags;

		var overlayFlags = GetTileInfo(tile.Overlay).Flags;

        if (overlayFlags.HasFlag(TileFlags.UnderlayHasPriority))
            return underlayFlags;

        return overlayFlags;
    }

    private bool CanMoveTo(int x, int y, bool player, int collisionClass)
    {
        if (x < 0 || y < 0 || x >= map!.Width || y >= map.Height)
            return false;

        if (!player)
        {
            var testPosition = new Position(x, y);

            if (characters.Any(character => character.Position == testPosition))
                return false;
        }

        var tileFlags = GetTileFlags(x, y);

        if (tileFlags.HasFlag(TileFlags.BlockAllMovement))
            return false;

		return tileFlags.HasFlag((TileFlags)(1 << (8 + collisionClass)));
    }
}
