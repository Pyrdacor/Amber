using Amber.Assets.Common;
using Amber.Common;
using Amber.Renderer;
using Amber.Renderer.Common;
using Amberstar.Game.Events;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Events;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

internal class Map3DScreen : ButtonGridScreen
{
	private static readonly Dictionary<Direction, Dictionary<PerspectiveLocation, Position>> PerspectiveMappings =
		new()
		{
			{
				Direction.North, new Dictionary<PerspectiveLocation, Position>
				{
					{ PerspectiveLocation.Forward3Left1, new Position(-1, -3) },
					{ PerspectiveLocation.Forward3Right1, new Position(1, -3) },
					{ PerspectiveLocation.Forward3, new Position(0, -3) },
					{ PerspectiveLocation.Forward2Left1, new Position(-1, -2) },
					{ PerspectiveLocation.Forward2Right1, new Position(1, -2) },
					{ PerspectiveLocation.Forward2, new Position(0, -2) },
					{ PerspectiveLocation.Forward1Left1, new Position(-1, -1) },
					{ PerspectiveLocation.Forward1Right1, new Position(1, -1) },
					{ PerspectiveLocation.Forward1, new Position(0, -1) },
					{ PerspectiveLocation.Left1, new Position(-1, 0) },
					{ PerspectiveLocation.Right1, new Position(1, 0) },
					{ PerspectiveLocation.PlayerLocation, new Position(0, 0) },
					{ PerspectiveLocation.Forward3Left2, new Position(-2, -3) },
					{ PerspectiveLocation.Forward3Right2, new Position(2, -3) },
				}
			},
			{
				Direction.East, new Dictionary<PerspectiveLocation, Position>
				{
					{ PerspectiveLocation.Forward3Left1, new Position(3, -1) },
					{ PerspectiveLocation.Forward3Right1, new Position(3, 1) },
					{ PerspectiveLocation.Forward3, new Position(3, 0) },
					{ PerspectiveLocation.Forward2Left1, new Position(2, -1) },
					{ PerspectiveLocation.Forward2Right1, new Position(2, 1) },
					{ PerspectiveLocation.Forward2, new Position(2, 0) },
					{ PerspectiveLocation.Forward1Left1, new Position(1, -1) },
					{ PerspectiveLocation.Forward1Right1, new Position(1, 1) },
					{ PerspectiveLocation.Forward1, new Position(1, 0) },
					{ PerspectiveLocation.Left1, new Position(0, -1) },
					{ PerspectiveLocation.Right1, new Position(0, 1) },
					{ PerspectiveLocation.PlayerLocation, new Position(0, 0) },
					{ PerspectiveLocation.Forward3Left2, new Position(3, -2) },
					{ PerspectiveLocation.Forward3Right2, new Position(3, 2) },
				}
			},
			{
				Direction.South, new Dictionary<PerspectiveLocation, Position>
				{
					{ PerspectiveLocation.Forward3Left1, new Position(1, 3) },
					{ PerspectiveLocation.Forward3Right1, new Position(-1, 3) },
					{ PerspectiveLocation.Forward3, new Position(0, 3) },
					{ PerspectiveLocation.Forward2Left1, new Position(1, 2) },
					{ PerspectiveLocation.Forward2Right1, new Position(-1, 2) },
					{ PerspectiveLocation.Forward2, new Position(0, 2) },
					{ PerspectiveLocation.Forward1Left1, new Position(1, 1) },
					{ PerspectiveLocation.Forward1Right1, new Position(-1, 1) },
					{ PerspectiveLocation.Forward1, new Position(0, 1) },
					{ PerspectiveLocation.Left1, new Position(1, 0) },
					{ PerspectiveLocation.Right1, new Position(-1, 0) },
					{ PerspectiveLocation.PlayerLocation, new Position(0, 0) },
					{ PerspectiveLocation.Forward3Left2, new Position(2, 3) },
					{ PerspectiveLocation.Forward3Right2, new Position(-2, 3) },
				}
			},
			{
				Direction.West, new Dictionary<PerspectiveLocation, Position>
				{
					{ PerspectiveLocation.Forward3Left1, new Position(-3, 1) },
					{ PerspectiveLocation.Forward3Right1, new Position(-3, -1) },
					{ PerspectiveLocation.Forward3, new Position(-3, 0) },
					{ PerspectiveLocation.Forward2Left1, new Position(-2, 1) },
					{ PerspectiveLocation.Forward2Right1, new Position(-2, -1) },
					{ PerspectiveLocation.Forward2, new Position(-2, 0) },
					{ PerspectiveLocation.Forward1Left1, new Position(-1, 1) },
					{ PerspectiveLocation.Forward1Right1, new Position(-1, -1) },
					{ PerspectiveLocation.Forward1, new Position(-1, 0) },
					{ PerspectiveLocation.Left1, new Position(0, 1) },
					{ PerspectiveLocation.Right1, new Position(0, -1) },
					{ PerspectiveLocation.PlayerLocation, new Position(0, 0) },
					{ PerspectiveLocation.Forward3Left2, new Position(-3, 2) },
					{ PerspectiveLocation.Forward3Right2, new Position(-3, -2) },
				}
			}
		};

	const int TicksPerStep = 16;
	const int TicksPerTurn = 16;
	const int AnimationTicksPerFrame = 25;

	const int ViewWidth = 144;
	const int ViewHeight = 144;
	const int OffsetX = 32;
	const int OffsetY = 49;
	const int SkyTransparentColorIndex = 11;
	IReadOnlyDictionary<int, IGraphic> backgrounds = new Dictionary<int, IGraphic>();
    IReadOnlyDictionary<int, IGraphic> clouds = new Dictionary<int, IGraphic>();
    IReadOnlyDictionary<DayTime, Color[]> skyGradients = new Dictionary<DayTime, Color[]>();
	IMap3D? map;
	ILabData? labData;
	readonly List<IColoredRect> skyGradient = [];
	readonly List<IAnimatedSprite> images = [];
	readonly List<Characters.MapCharacter> characters = [];
	long currentTicks = 0;
	long lastMoveTicks = 0;
	long lastTurnTicks = 0;
	long lastAnimationFrame = 0;
	byte palette = 0;
	bool mouseDown = false;
	IRenderText? mapNameText;

	public override ScreenType Type { get; } = ScreenType.Map3D;
    public override ScreenFadeType FadeType { get; } = ScreenFadeType.None;
    public IMap3D Map => map!;

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

        backgrounds = Game.AssetProvider.GraphicLoader.LoadAllBackgroundGraphics();
		clouds = Game.AssetProvider.GraphicLoader.LoadAllCloudGraphics();
		skyGradients = Game.AssetProvider.GraphicLoader.LoadSkyGradients();
    }

	public override void ScreenPushed(Screen screen)
	{
		base.ScreenPushed(screen);

		if (!screen.Transparent)
		{
			images.ForEach(image => image.Visible = false);
			skyGradient.ForEach(g => g.Visible = false);
			mapNameText!.Visible = false;
		}

		mouseDown = false;
        Game.Pause();
	}

	public override void ScreenPopped(Screen screen)
	{
		if (!screen.Transparent)
		{
			SetLayout();
            images.ForEach(image => image.Visible = true);
			skyGradient.ForEach(g => g.Visible = true);
            mapNameText!.Visible = true;
        }

		base.ScreenPopped(screen);

        Game.Resume();
	}

    private void SetLayout()
    {
        // TODO: For some reason the palette is not exactly the same as on the Atari ST. It is brighter there.
        Game.SetLayout(Layout.Map3D, palette);
    }

	private void ShowMapName()
	{
		mapNameText?.Delete();
        mapNameText = Game.TextManager.Create(map!.Name, 15, TextManager.TransparentPaper, palette);
        mapNameText.ShowInArea(OffsetX, OffsetY - mapNameText.LineHeight - 3, ViewWidth, ViewHeight, 100, TextAlignment.Center);
    }

    public override void Open(Action? closeAction)
	{
        base.Open(closeAction);

		currentTicks = 0;
		lastMoveTicks = 0;
		lastTurnTicks = 0;
		mouseDown = false;

		SetLayout();
		LoadMap(Game.State.MapIndex);
        ShowMapName();
        AfterMove();

        Game.Time.MinuteChanged += MinuteChanged;
        Game.CanSeeChanged += CanSeeChanged;
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

			void Move()
			{
				bool left = moveX < 0;
				bool right = moveX > 0;
				bool forward = moveY < 0;
				bool backward = moveY > 0;
				CheckMove(forward, backward, left, right, false, false);
			}

			void Turn(int dist)
			{
				bool turnLeft = dist < 0;
				bool turnRight = dist > 0;
				CheckMove(false, false, false, false, turnLeft, turnRight);
			}

			if ((moveX == 0) != (moveY == 0)) // move up, right, down or left
				Move();
			else if (moveY == -1) // turn left or right
				Turn(moveX);
			else // rotate left or right
				Rotate(moveX > 0);
		}
		else // Actions
		{
            var buttonType = GetButtonType(index);

            switch (buttonType)
			{
				case ButtonType.Eye:
				{
					// TODO: NPCs

					var playerPosition = Game.State.PartyPosition;
					var forwardPosition = playerPosition + Game.State.PartyDirection.Offset();

					if (forwardPosition.X >= 0 && forwardPosition.X < map!.Width &&
						forwardPosition.Y >= 0 && forwardPosition.Y < map.Height &&
						map.Tiles[forwardPosition.X + forwardPosition.Y * map.Width].Event != 0)
					{
						var tile = map.Tiles[forwardPosition.X + forwardPosition.Y * map.Width];
						int eventIndex = tile.Event;
						Game.EventHandler.HandleEvent(EventTrigger.Eye, Event.CreateEvent(map.Events[eventIndex - 1], eventIndex), map);
					}

                    break;
                }				
                case ButtonType.Ear:
                {
					// TODO
					break;
				}
				case ButtonType.Mouth:
                {
					var character = characters.FirstOrDefault(character => character.Position == Game.State.PartyPosition);

					if (character != null && character.Type == MapCharacterType.Person)
					{
						Game.State.CurrentConversationCharacter = new(character.CharacterIndex, character.Map, character.Index);
						Game.ScreenHandler.PushScreen(ScreenType.Conversation);
						return;
					}

					break;
				}
				case ButtonType.UseMagic:
				{
					// TODO
					break;
				}
				case ButtonType.Camp:
				{
					// TODO
					break;
				}
				case ButtonType.Map:
				{
					// TODO
					// Game.ScreenHandler.PushScreen(ScreenType.Map);
					break;
				}
				case ButtonType.PartyPositions:
				{
					// TODO
					break;
				}
				case ButtonType.Disk: // options
				{
					// TODO: Show options
					Game.Quit();
					break;
				}
			}
        }
	}

	protected override void SetupButtons(ButtonGrid buttonGrid)
	{
		if (Game.ButtonLayout == ButtonLayout.Movement)
		{
			// Upper row
			buttonGrid.SetButton(0, ButtonType.TurnLeft);
			buttonGrid.SetButton(1, ButtonType.MoveForward);
			buttonGrid.SetButton(2, ButtonType.TurnRight);
			// Middle row
			buttonGrid.SetButton(3, ButtonType.StrafeLeft);
			buttonGrid.SetButton(4, ButtonType.Sleep);
			buttonGrid.SetButton(5, ButtonType.StrafeRight);
			// Lower row
			buttonGrid.SetButton(6, ButtonType.RotateLeft);
			buttonGrid.SetButton(7, ButtonType.MoveBackward);
			buttonGrid.SetButton(8, ButtonType.RotateRight);

            bool enableMoveButtons = Game.State.ActivePartyMember?.CanMove(inBattle: false) ?? false;

            for (int i = 0; i < 9; i++)
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
			buttonGrid.EnableButton(3, false);
			buttonGrid.SetButton(4, ButtonType.UseMagic);
			buttonGrid.SetButton(5, ButtonType.Camp);
			// Lower row
			buttonGrid.SetButton(6, ButtonType.Map);
			buttonGrid.SetButton(7, ButtonType.PartyPositions);
			buttonGrid.SetButton(8, ButtonType.Disk);
		}
	}

	public override void Close()
	{
		mouseDown = false;
		Game.Time.MinuteChanged -= MinuteChanged;
		Game.CanSeeChanged -= CanSeeChanged;
		ClearView();
		skyGradient.ForEach(g => g.Visible = false);
		skyGradient.Clear();
        characters.Clear();
		mapNameText!.Delete();

		base.Close();
	}

	private void CanSeeChanged(bool canSee)
	{
		if (Game.Paused)
			return;

		UpdateLight();

		if (map!.Flags.HasFlag(MapFlags.City))
			UpdateSky(canSee);
	}

	private void MinuteChanged()
	{
		if (Game.Paused)
			return;

		var lightMode = map!.Flags.GetLightMode();

		if (lightMode != LightMode.Static)
			UpdateLight();

		if (map.Flags.HasFlag(MapFlags.City) && Game.CanSee())
			UpdateSky(true);

		if (characters.Count != 0) // TODO: check for active ones only (or maybe remove inactive ones)
		{
			foreach (var character in characters)
			{
				character.Update(Game);
			}

			var offsets = PerspectiveMappings[Game.State.PartyDirection];
			var playerPosition = Game.State.PartyPosition;

			for (int i = 0; i < 10; i++)
			{
				var offset = offsets[(PerspectiveLocation)i];
				int x = playerPosition.X + offset.X;
				int y = playerPosition.Y + offset.Y;

				if (characters.Any(character => character.Position.X == x && character.Position.Y == y))
				{
					UpdateView();
					break;
				}
			}
		}
	}

	public override void Update(long elapsedTicks)
	{
		if (elapsedTicks == 0)
			return;

		currentTicks += elapsedTicks;
		long animationFrame = currentTicks / AnimationTicksPerFrame;

		if (animationFrame != lastAnimationFrame)
		{
			lastAnimationFrame = animationFrame;

			foreach (var image in images)
				image.CurrentFrameIndex++;
		}

		if (Game.InputEnabled && !Game.Paused && currentTicks >= lastMoveTicks + TicksPerStep)
			CheckMove();
	}

	private void AfterMove()
	{
		// TODO

		var playerPosition = Game.State.PartyPosition;
		UpdateView();

		Game.Time.Moved3D();

		// Check for events
		var eventIndex = map!.Tiles[playerPosition.X + playerPosition.Y * map.Width].Event;

		if (eventIndex != 0 && Game.IsEventActive(map, eventIndex, map.Events[eventIndex - 1]))
		{
			var @event = map.Events[eventIndex - 1];
            var mapEvent = Event.CreateEvent(@event, eventIndex);

			if (@event is IPlaceEvent ||
                @event is IDoorEvent ||
                @event is IDoorExitEvent ||
                @event is IChestEvent ||
                @event is ITeleportEvent)
			{
				Game.State.ResetPartyPosition();
                UpdateView();
            }

			Game.EventHandler.HandleEvent(EventTrigger.Move, mapEvent, map);
		}
	}

    internal void ResetPartyPosition()
    {
		if (Game.State.ResetPartyPosition())
			UpdateView();
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

		if (tileFlags.HasFlag(LabTileFlags.BlockAllMovement))
			return false;

		return tileFlags.HasFlag((LabTileFlags)(1 << (8 + collisionClass)));
	}

	private void UpdateLight()
	{
		// TODO
	}

	private void UpdateSky(bool canSee)
	{
		if (!canSee)
		{
			skyGradient.ForEach(g => g.Visible = false);
			skyGradient.Clear();
			return;
		}

		var dayTime = Game.State.Hour.HourToDayTime();
		var gradient = skyGradients[dayTime];
		var skyColor = Game.AssetProvider.PaletteLoader.LoadPalette(palette).GetColorAt(SkyTransparentColorIndex, 0);

		if (skyGradient.Count == 0)
		{
			void CreateSkyLine(int y, Color color)
			{
				var skyLine = Game.GetRenderLayer(Layer.Map3D).ColoredRectFactory!.Create();
				skyLine.Color = color;
				skyLine.Position = new(OffsetX, OffsetY + y);
				skyLine.Size = new(ViewWidth, 1);
				skyLine.DisplayLayer = 0;
				skyLine.Visible = true;				
				skyGradient.Add(skyLine);
			}

			for (int y = 0; y < gradient.Length - 1; y++)
				CreateSkyLine(y, gradient[y]);
			
			CreateSkyLine(gradient.Length - 1, skyColor);
		}
	}

	private LabTileFlags GetTileFlags(int x, int y)
	{
		var tile = map!.Tiles[x + y * map.Width];

		if (tile.LabTileIndex == 0)
			return LabTileFlags.None;

		var labTile = map.LabTiles[tile.LabTileIndex - 1];

		return labTile.Flags;
	}

	private void CheckMove(bool forward, bool backward, bool left, bool right, bool turnLeft, bool turnRight)
	{
		switch (Game.State.PartyDirection)
		{
			case Direction.North:
				if (forward && !backward)
					Move(0, -1);
				else if (backward && !forward)
					Move(0, 1);
				else if (left && !right)
					Move(-1, 0);
				else if (right && !left)
					Move(1, 0);
				else if (turnLeft && !turnRight)
					TurnTo(Direction.West);
				else if (turnRight && !turnLeft)
					TurnTo(Direction.East);
				break;
			case Direction.East:
				if (forward && !backward)
					Move(1, 0);
				else if (backward && !forward)
					Move(-1, 0);
				else if (left && !right)
					Move(0, -1);
				else if (right && !left)
					Move(0, 1);
				else if (turnLeft && !turnRight)
					TurnTo(Direction.North);
				else if (turnRight && !turnLeft)
					TurnTo(Direction.South);
				break;
			case Direction.South:
				if (forward && !backward)
					Move(0, 1);
				else if (backward && !forward)
					Move(0, -1);
				else if (left && !right)
					Move(1, 0);
				else if (right && !left)
					Move(-1, 0);
				else if (turnLeft && !turnRight)
					TurnTo(Direction.East);
				else if (turnRight && !turnLeft)
					TurnTo(Direction.West);
				break;
			case Direction.West:
				if (forward && !backward)
					Move(-1, 0);
				else if (backward && !forward)
					Move(1, 0);
				else if (left && !right)
					Move(0, 1);
				else if (right && !left)
					Move(0, -1);
				else if (turnLeft && !turnRight)
					TurnTo(Direction.South);
				else if (turnRight && !turnLeft)
					TurnTo(Direction.North);
				break;
		}
	}

	private void CheckMove()
	{
		bool left = Game.IsKeyDown('A');
		bool right = Game.IsKeyDown('D');
		bool forward = Game.IsKeyDown(Key.Up) || Game.IsKeyDown('W');
		bool backward = Game.IsKeyDown(Key.Down) || Game.IsKeyDown('S');
		bool turnLeft = Game.IsKeyDown(Key.Left) || Game.IsKeyDown('Q');
		bool turnRight = Game.IsKeyDown(Key.Right) || Game.IsKeyDown('E');

		if (mouseDown && Game.InputEnabled && !Game.Paused)
		{
			switch (Game.Cursor.CursorType)
			{
				case CursorType.ArrowForward3D:
					forward = true;
					break;
				case CursorType.ArrowBackward3D:
					backward = true;
					break;
				case CursorType.ArrowLeft3D:
					left = true;
					break;
				case CursorType.ArrowRight3D:
					right = true;
					break;
				case CursorType.ArrowTurnLeft3D:
					turnLeft = true;
					break;
				case CursorType.ArrowTurnRight3D:
					turnRight = true;
					break;
				case CursorType.FullTurnLeft:
					// TODO
					break;
				case CursorType.FullTurnRight:
					// TODO
					break;
			}
		}

		if (Game.ButtonLayout == ButtonLayout.Movement)
		{
			if (!left)
				left = Game.IsKeyDown(Key.Keypad4);
			if (!right)
				right = Game.IsKeyDown(Key.Keypad6);
			if (!forward)
				forward = Game.IsKeyDown(Key.Keypad8);
			if (!backward)
				backward = Game.IsKeyDown(Key.Keypad2);
			if (!turnLeft)
				turnLeft = Game.IsKeyDown(Key.Keypad7);
			if (!turnRight)
				turnRight = Game.IsKeyDown(Key.Keypad9);
			// TODO
			/*if (!fullTurnLeft)
				fullTurnLeft = Game.IsKeyDown(Key.Keypad1);
			if (!fullTurnRight)
				fullTurnRight = Game.IsKeyDown(Key.Keypad3);*/
		}

		CheckMove(forward, backward, left, right, turnLeft, turnRight);
	}

	public override bool KeyDown(Key key, KeyModifiers keyModifiers)
	{
		if (base.KeyDown(key, keyModifiers))
			return true;

        CheckMove();

		return true;
	}

	public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		if (buttons == MouseButtons.Right)
		{
			if (ButtonGrid.Area.Contains(position))
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
			var mapArea = new Rect(OffsetX, OffsetY, ViewWidth, ViewHeight);

			if (mapArea.Contains(position))
			{
				if (Game.Cursor.CursorType == CursorType.Zzz)
				{
					Game.Time.Tick();
					return true;
				}
				else if (Game.Cursor.CursorType >= CursorType.ArrowUp2D && Game.Cursor.CursorType <= CursorType.ArrowDownLeft2D)
				{
					CheckMove();
					return true;
				}
            }
        }

		return base.MouseDown(position, buttons, keyModifiers);
    }

	public override bool MouseUp(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		mouseDown = false;

		return base.MouseUp(position, buttons, keyModifiers);
	}

	public override void MouseMove(Position position, MouseButtons buttons)
	{
		base.MouseMove(position, buttons);

		var mapArea = new Rect(OffsetX, OffsetY, ViewWidth, ViewHeight);

		if (mapArea.Contains(position))
		{
			int relativeX = position.X - mapArea.Left;
			int relativeY = position.Y - mapArea.Top;
			bool left = relativeX < mapArea.Size.Width / 4;
			bool right = relativeX >= mapArea.Size.Width * 3 / 4;
			bool up = relativeY < mapArea.Size.Height / 4;
			bool down = relativeY >= mapArea.Size.Height * 3 / 4;

			var lastCursor = Game.Cursor.CursorType;

			if (up)
			{
				if (left)
					Game.Cursor.CursorType = CursorType.ArrowTurnLeft3D;
				else if (right)
					Game.Cursor.CursorType = CursorType.ArrowTurnRight3D;
				else
					Game.Cursor.CursorType = CursorType.ArrowForward3D;
			}
			else if (down)
			{
				if (left)
					Game.Cursor.CursorType = CursorType.FullTurnLeft;
				else if (right)
					Game.Cursor.CursorType = CursorType.FullTurnRight;
				else
					Game.Cursor.CursorType = CursorType.ArrowBackward3D;
			}
			else
			{
				if (left)
					Game.Cursor.CursorType = CursorType.ArrowLeft3D;
				else if (right)
					Game.Cursor.CursorType = CursorType.ArrowRight3D;
				else
					Game.Cursor.CursorType = CursorType.Zzz;
			}

			if (mouseDown && lastCursor != Game.Cursor.CursorType)
				CheckMove();
		}
		else if (Game.Cursor.CursorType != CursorType.Sword)
		{
			Game.Cursor.CursorType = CursorType.Sword;
		}
	}

	private void Move(int x, int y)
	{
		if (currentTicks - lastMoveTicks < TicksPerStep)
			return;

		lastMoveTicks = currentTicks;
		int targetX = Game.State.PartyPosition.X + x;
		int targetY = Game.State.PartyPosition.Y + y;

		if (CanMoveTo(targetX, targetY, true, 0))
		{
			Game.State.SetPartyPosition(targetX, targetY);
			AfterMove();
		}
		else
		{
			// TODO: ouch
		}
	}

	private void TurnTo(Direction newDirection)
	{
		if (currentTicks - lastTurnTicks < TicksPerTurn)
			return;

		lastTurnTicks = currentTicks;
		Game.State.PartyDirection = newDirection;
		UpdateView();
	}

	private void Rotate(bool right)
	{
		// TODO
	}

	private BlockFacing FacingByRelativeOffset(Position offset)
	{
		switch (Game.State.PartyDirection)
		{
			case Direction.North:
				if (offset.X < 0)
					return BlockFacing.LeftOfPlayer;
				else if (offset.X > 0) 
					return BlockFacing.RightOfPlayer;
				return BlockFacing.FacingPlayer;
			case Direction.East:
				if (offset.Y < 0)
					return BlockFacing.LeftOfPlayer;
				else if (offset.Y > 0)
					return BlockFacing.RightOfPlayer;
				return BlockFacing.FacingPlayer;
			case Direction.South:
				if (offset.X < 0)
					return BlockFacing.RightOfPlayer;
				else if (offset.X > 0)
					return BlockFacing.LeftOfPlayer;
				return BlockFacing.FacingPlayer;
			case Direction.West:
				if (offset.Y < 0)
					return BlockFacing.RightOfPlayer;
				else if (offset.Y > 0)
					return BlockFacing.LeftOfPlayer;
				return BlockFacing.FacingPlayer;
			default:
				return BlockFacing.FacingPlayer;
		}
	}

	private void UpdateView()
	{
		var playerPosition = Game.State.PartyPosition;

		ClearView();

		var offsets = PerspectiveMappings[Game.State.PartyDirection];
		var layer = Game.GetRenderLayer(Layer.Map3D);
		var textureAtlas = layer.Config.Texture!;
		byte displayLayer = 40;

		var floor = backgrounds[labData!.FloorIndex];
		var floorSprite = layer.SpriteFactory!.CreateAnimated();
		floorSprite.Size = new(floor.Width, floor.Height);
		floorSprite.Position = new(OffsetX, OffsetY + ViewHeight - floor.Height);
		floorSprite.TextureOffset = textureAtlas.GetOffset(Game.GraphicIndexProvider.GetBackgroundGraphicIndex(labData.FloorIndex));
		floorSprite.Opaque = true;
		floorSprite.DisplayLayer = 5;
		floorSprite.PaletteIndex = palette;
		floorSprite.Visible = true;
		images.Add(floorSprite);

		var hasSky = map!.Flags.HasFlag(MapFlags.City);
		var dayTime = Game.State.Hour.HourToDayTime();

		if (hasSky && (dayTime == DayTime.Day || dayTime == DayTime.Dusk))
		{
			// Clouds
			var cloud = clouds[labData!.CeilingIndex];
			var cloudSprite = layer.SpriteFactory!.CreateAnimated();
			cloudSprite.Size = new(cloud.Width, cloud.Height);
			cloudSprite.Position = new(OffsetX, OffsetY);
			cloudSprite.TextureOffset = textureAtlas.GetOffset(Game.GraphicIndexProvider.GetCloudGraphicIndex(labData.CeilingIndex));
			cloudSprite.TransparentColorIndex = SkyTransparentColorIndex;
			cloudSprite.DisplayLayer = 12;
			cloudSprite.PaletteIndex = palette;
			cloudSprite.Visible = true;
			images.Add(cloudSprite);
		}
		else
		{
			// Normal sky or ceiling
			var ceiling = backgrounds[labData!.CeilingIndex];
			var ceilingSprite = layer.SpriteFactory!.CreateAnimated();
			ceilingSprite.Size = new(ceiling.Width, ceiling.Height);
			ceilingSprite.Position = new(OffsetX, OffsetY);
			ceilingSprite.TextureOffset = textureAtlas.GetOffset(Game.GraphicIndexProvider.GetBackgroundGraphicIndex(labData.CeilingIndex));
			ceilingSprite.Opaque = !hasSky;
			ceilingSprite.TransparentColorIndex = (byte)(hasSky ? SkyTransparentColorIndex : 0);
			ceilingSprite.DisplayLayer = 10;
			ceilingSprite.PaletteIndex = palette;
			ceilingSprite.Visible = true;
			images.Add(ceilingSprite);
		}

		for (int i = 0; i < 14; i++)
		{
			var perspectiveLocation = (PerspectiveLocation)i;
			var offset = offsets[perspectiveLocation];
			int x = playerPosition.X + offset.X;
			int y = playerPosition.Y + offset.Y;
			var tile = map!.Tiles[x + y * map.Width];

			if (tile.LabTileIndex == 0)
				continue;

			var labTile = map!.LabTiles[tile.LabTileIndex - 1];

			void DrawBlock(ILabBlock labBlock, int? customRenderX = null)
			{
				if (labBlock.Type != LabBlockType.Wall && i > 11)
					return;

				if (i > 11)
				{
					displayLayer = (byte)(20 + (i % 12) * 10);
					perspectiveLocation = (PerspectiveLocation)((int)perspectiveLocation % 12);
				}

				var facing = labBlock.Type == LabBlockType.Overlay ? FacingByRelativeOffset(offset) : BlockFacing.FacingPlayer;
				byte displayPlayerAdd = (byte)(facing == BlockFacing.FacingPlayer ? 10 : 5);

				if (facing != BlockFacing.FacingPlayer)
					AddBlockSprite(facing);

				AddBlockSprite(BlockFacing.FacingPlayer);

				void AddBlockSprite(BlockFacing facing)
				{
					var perspective = labBlock.Perspectives.FirstOrDefault(p => p.Location == perspectiveLocation && p.Facing == facing);

					if (perspective.Frames == null)
						return;

					if (customRenderX == OffsetX + ViewWidth)
						customRenderX -= perspective.Frames[0].Width / 2;

					if (perspective.SpecialRenderPosition != null)
					{
						int graphicIndex = Game.GraphicIndexProvider.GetLabBlockGraphicIndex(labBlock.Index, perspectiveLocation, facing);
						var textureOffset = textureAtlas.GetOffset(graphicIndex);
						var blockSprite = layer.SpriteFactory!.CreateAnimated();
						blockSprite.FrameCount = 1;
						blockSprite.Size = new Size(perspective.Frames[0].Width, perspective.Frames[0].Height);
						blockSprite.DisplayLayer = displayLayer;
						blockSprite.PaletteIndex = palette;
						blockSprite.TextureOffset = textureOffset;
						blockSprite.Position = new(OffsetX + perspective.RenderPosition.X, OffsetY + perspective.RenderPosition.Y);
						blockSprite.Visible = true;

						images.Add(blockSprite);

						displayLayer += (byte)(displayPlayerAdd / 2);

						blockSprite = layer.SpriteFactory!.CreateAnimated();
						blockSprite.FrameCount = perspective.Frames.Length - 1;
						blockSprite.Size = new Size(perspective.Frames[1].Width, perspective.Frames[1].Height);
						blockSprite.DisplayLayer = displayLayer;
						blockSprite.PaletteIndex = palette;
						blockSprite.TextureOffset = new(textureOffset.X + perspective.Frames[0].Width, textureOffset.Y);
						blockSprite.Position = new(OffsetX + perspective.SpecialRenderPosition.Value.X, OffsetY + perspective.SpecialRenderPosition.Value.Y);
						blockSprite.Visible = true;

						images.Add(blockSprite);
					}
					else
					{
						var blockSprite = layer.SpriteFactory!.CreateAnimated();
						blockSprite.FrameCount = perspective.Frames.Length;
						blockSprite.Size = new Size(perspective.Frames[0].Width, perspective.Frames[0].Height);
						blockSprite.DisplayLayer = displayLayer;
						blockSprite.PaletteIndex = palette;
						blockSprite.TextureOffset = textureAtlas.GetOffset(Game.GraphicIndexProvider.GetLabBlockGraphicIndex(labBlock.Index, perspectiveLocation, facing));
						blockSprite.Position = new(customRenderX ?? (OffsetX + perspective.RenderPosition.X), OffsetY + perspective.RenderPosition.Y);
						blockSprite.Visible = true;

						images.Add(blockSprite);
					}

					displayLayer += displayPlayerAdd;
				}
			}

			var primary = labData!.LabBlocks[labTile.PrimaryLabBlockIndex - 1];

			if (primary.Type == LabBlockType.Overlay && labTile.SecondaryLabBlockIndex != 0)
			{
				// Draw underlay for overlays
				DrawBlock(labData!.LabBlocks[labTile.SecondaryLabBlockIndex - 1]);
			}

			// Draw underlay or overlay
			if (labTile.PrimaryLabBlockIndex != 1) // 1 seems to be a marker for free tiles
				DrawBlock(primary);

			var character = characters.FirstOrDefault(character => character.Position == new Position(x, y));

			if (character != null)
			{
				var objectBlock = labData!.LabBlocks[character.Icon - 1];
				int? customX = null;

				if ((int)perspectiveLocation % 3 == 0) // left row
					customX = OffsetX;
				else if ((int)perspectiveLocation % 3 == 1) // right row
					customX = OffsetX + ViewWidth;

				DrawBlock(objectBlock, customX);
			}
		}
	}

	private void ClearView()
	{
		images.ForEach(image => image.Visible = false);
		images.Clear();
	}

	private void LoadMap(int index)
	{
		map = Game.AssetProvider.MapLoader.LoadMap(index) as IMap3D; // TODO: catch exceptions
		labData = Game.AssetProvider.LabDataLoader.LoadLabData(map!.LabDataIndex);
		palette = Game.PaletteIndexProvider.GetLabyrinthPaletteIndex(labData.PaletteIndex - 1);
		characters.Clear();


        for (int i = 0; i < map.Characters.Length; i++)
		{
			var characterData = map.Characters[i];

			if (characterData.Index != 0 && characterData.Icon != 0)
			{
				characters.Add(new Characters.MapCharacter(map, i, map.CharacterPositions[i], Game.State,
					(x, y, collisionClass) => CanMoveTo(x, y, false, collisionClass)));
			}
		}

		var lightMode = map.Flags.GetLightMode();

		if (lightMode != LightMode.Static)
			UpdateLight();

		if (map.Flags.HasFlag(MapFlags.City) && Game.CanSee())
			UpdateSky(true);

		if (Map.SongIndex != 0)
            Game.PlaySong(1 + Map.SongIndex);

        Game.State.MapIndex = index;
		Game.State.SetIsWorldMap(false);
		Game.State.TravelType = TravelType.Walk;
		Game.Cursor.PaletteIndex = palette;
		RequestButtonGridPaletteUpdate();

    }
}
