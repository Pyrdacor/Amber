using Amber.Common;

namespace Amberstar.Game;

partial class Game
{
    readonly Action<Position> setMousePosition;
    readonly Func<List<Key>> pressedKeyProvider;	
    List<Key>? pressedKeys = null;
    Rect? mouseTrapArea = null;
	Position lastMousePosition = new();

    internal bool InputEnabled { get; private set; } = true;		
	internal bool Paused { get; private set; } = false;

	public void KeyDown(Key key, KeyModifiers keyModifiers)
	{
		if (key == Key.F10)
			PlaySong(1);
		else if (key == Key.F5)
		{
			SaveGame();
			ShowTextMessage($"Game was saved at '{TempSaveFile}'.");
			return;
		}
        else if (key == Key.F7)
        {
            LoadGame();
			ShowTextMessage($"Game was loaded from '{Path.GetFileName(TempSaveFile)}'.", () =>
			{
				Teleport(1 + State.PartyPosition.X, 1 + State.PartyPosition.Y, State.PartyDirection, State.MapIndex, true);
			});
            return;
        }

        if (!InputEnabled)
			return;

		if (ScreenHandler.ActiveScreen?.KeyDown(key, keyModifiers) == true)
			return;
	}

	public void KeyUp(Key key, KeyModifiers keyModifiers)
	{
		if (!InputEnabled)
			return;

		if (ScreenHandler.ActiveScreen?.KeyUp(key, keyModifiers) == true)
			return;
	}

	public void KeyChar(char ch, KeyModifiers keyModifiers)
	{
		if (!InputEnabled)
			return;

		if (ScreenHandler.ActiveScreen?.KeyChar(ch, keyModifiers) == true)
			return;
	}

	public void MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		if (!InputEnabled)
			return;

		if (ScreenHandler.ActiveScreen?.MouseDown(position, buttons, keyModifiers) == true)
			return;
	}

	public void MouseUp(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		if (!InputEnabled)
			return;

		if (ScreenHandler.ActiveScreen?.MouseUp(position, buttons, keyModifiers) == true)
			return;
	}

	public void MouseMove(Position position, MouseButtons buttons)
	{
		Position newPosition;


        if (mouseTrapArea != null)
		{
			newPosition = new Position
			(
				MathUtil.Limit(mouseTrapArea.Value.Left, position.X, mouseTrapArea.Value.Right - 1),
				MathUtil.Limit(mouseTrapArea.Value.Top, position.Y, mouseTrapArea.Value.Bottom - 1)
			);
        }
		else
		{
            newPosition = new Position
            (
                MathUtil.Limit(0, position.X, 319),
                MathUtil.Limit(0, position.Y, 199)
            );
        }

        if (position != newPosition)
        {
            position = newPosition;
            setMousePosition(position);
        }

        lastMousePosition = position;
        Cursor.Position = position;

		if (!InputEnabled)
			return;

		ScreenHandler.ActiveScreen?.MouseMove(position, buttons);
	}

	public void MouseWheel(Position position, float scrollX, float scrollY, MouseButtons buttons)
	{
		if (!InputEnabled)
			return;

		if (ScreenHandler.ActiveScreen?.MouseWheel(position, scrollX, scrollY, buttons) == true)
			return;
    }

    public void TrapMouse(Rect rect)
    {
		mouseTrapArea = rect;

		var position = lastMousePosition;

		position = new Position(MathUtil.Limit(rect.Left, position.X, rect.Right - 1), MathUtil.Limit(rect.Top, position.Y, rect.Bottom - 1));

		if (position != lastMousePosition)
		{
            setMousePosition(position);
        }
    }

	internal void TrapMouseInPortraitArea() => TrapMouse(PortraitArea);

	public void UntrapMouse()
	{
		mouseTrapArea = null;
		ScreenHandler.ActiveScreen?.MouseMove(lastMousePosition, MouseButtons.None);
    }

	public void SimulateMouseMoveWithoutButton() => MouseMove(lastMousePosition, MouseButtons.None);

    internal bool IsKeyDown(Key key) => InputEnabled && (pressedKeys ??= pressedKeyProvider()).Contains(key);

	internal bool IsKeyDown(char ch) => InputEnabled && (pressedKeys ??= pressedKeyProvider()).Contains(KeyByChar(ch));

	private static Key KeyByChar(char ch)
	{
		if (ch >= '0' && ch <= '9')
			return Key.Number0 + ch - '0';
		else if (ch >= 'A' && ch <= 'Z')
			return Key.LetterA + ch - 'A';
		else if (ch >= 'a' && ch <= 'z')
			return Key.LetterA + ch - 'a';
		else if (ch == '\n')
			return Key.Enter;
		else if (ch == ' ')
			return Key.Space;
		else
			return Key.Invalid;
	}

	internal void Pause()
	{
		Paused = true;
	}

	internal void Resume()
	{
		Paused = false;
	}

	internal void EnableInput(bool enable)
	{
		InputEnabled = enable;
	}
}
