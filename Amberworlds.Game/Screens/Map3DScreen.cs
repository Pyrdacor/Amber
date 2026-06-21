using Amber.Common;
using Amberworlds.GameData;

namespace Amberworlds.Game.Screens;

internal class Map3DScreen : Screen
{
	const int TicksPerStep = 16;
	const int TicksPerTurn = 16;
	const int AnimationTicksPerFrame = 25;

	const int ViewWidth = 144;
	const int ViewHeight = 144;
	const int OffsetX = 32;
	const int OffsetY = 49;
	MapData3D? map;
	long currentTicks = 0;
	long lastMoveTicks = 0;
	long lastTurnTicks = 0;
	long lastAnimationFrame = 0;
	byte palette = 0;
	bool mouseDown = false;

	public override ScreenType Type { get; } = ScreenType.Map3D;
    public override ScreenFadeType FadeType { get; } = ScreenFadeType.None;

    internal void MapChanged()
	{
		//LoadMap(Game.State.MapIndex);
        ShowMapName();
		AfterMove();
	}

	public override void ScreenPushed(Screen screen)
	{
		base.ScreenPushed(screen);

		if (!screen.Transparent)
		{

		}

		mouseDown = false;
        Game.Pause();
	}

	public override void ScreenPopped(Screen screen)
	{
		if (!screen.Transparent)
		{
			SetLayout();
        }

		base.ScreenPopped(screen);

        Game.Resume();
	}

    private void SetLayout()
    {
        //Game.SetLayout(Layout.Map3D, palette);
    }

	private void ShowMapName()
	{
		/*mapNameText?.Delete();
        mapNameText = Game.TextManager.Create(map!.Name, 15, TextManager.TransparentPaper, palette);
        mapNameText.ShowInArea(OffsetX, OffsetY - mapNameText.LineHeight - 3, ViewWidth, ViewHeight, 100, TextAlignment.Center);*/
    }

    public override void Open(Action? closeAction)
	{
        base.Open(closeAction);

		currentTicks = 0;
		lastMoveTicks = 0;
		lastTurnTicks = 0;
		mouseDown = false;

		SetLayout();
		//LoadMap(Game.State.MapIndex);
        ShowMapName();
        AfterMove();
	}

	public override void Close()
	{
		mouseDown = false;
		ClearView();

		base.Close();
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

			/*foreach (var image in images)
				image.CurrentFrameIndex++;*/
		}

		if (Game.InputEnabled && !Game.Paused && currentTicks >= lastMoveTicks + TicksPerStep)
			CheckMove();
	}

	private void AfterMove()
	{
		// TODO

		//var playerPosition = Game.State.PartyPosition;
		UpdateView();

		//Game.Time.Moved3D();

		// TODO: Check for events
	}

    internal void ResetPartyPosition()
    {
		//if (Game.State.ResetPartyPosition())
		//	UpdateView();
    }

    private bool CanMoveTo(int x, int y, bool player, int collisionClass)
	{
		/*if (x < 0 || y < 0 || x >= map!.Width || y >= map.Height)
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

		return tileFlags.HasFlag((LabTileFlags)(1 << (8 + collisionClass)));*/
		return false;
	}

	private void UpdateLight()
	{
		// TODO
	}

	private void UpdateSky(bool canSee)
	{
		
	}

	private void CheckMove(bool forward, bool backward, bool left, bool right, bool turnLeft, bool turnRight)
	{
		/*switch (Game.State.PartyDirection)
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
		}*/
	}

	private void CheckMove()
	{
		/*bool left = Game.IsKeyDown('A');
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
			if (!fullTurnLeft)
				fullTurnLeft = Game.IsKeyDown(Key.Keypad1);
			if (!fullTurnRight)
				fullTurnRight = Game.IsKeyDown(Key.Keypad3);
		}

		CheckMove(forward, backward, left, right, turnLeft, turnRight);*/
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
		/*if (buttons == MouseButtons.Right)
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
        }*/

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

		/*if (mapArea.Contains(position))
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
		}*/
	}

	private void Move(int x, int y)
	{
		if (currentTicks - lastMoveTicks < TicksPerStep)
			return;

		/*lastMoveTicks = currentTicks;
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
		}*/
	}

	private void UpdateView()
	{
		ClearView();

		// TODO
	}

	private void ClearView()
	{
		//images.ForEach(image => image.Visible = false);
		//images.Clear();
	}

	private void LoadMap(int index)
	{
		/*map = Game.AssetProvider.MapLoader.LoadMap(index) as IMap3D; // TODO: catch exceptions
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
		RequestButtonGridPaletteUpdate();*/
    }
}
