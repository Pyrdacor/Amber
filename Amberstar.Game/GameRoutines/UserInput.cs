using Amber.Common;

namespace Amberstar.Game
{
	partial class Game
	{
		readonly Func<List<Key>> pressedKeyProvider;
		List<Key>? pressedKeys = null;

		internal bool InputEnabled { get; private set; } = true;		
		internal bool Paused { get; private set; } = false;

		public void KeyDown(Key key, KeyModifiers keyModifiers)
		{
			if (!InputEnabled)
				return;

			ScreenHandler.ActiveScreen?.KeyDown(key, keyModifiers);
		}

		public void KeyUp(Key key, KeyModifiers keyModifiers)
		{
			if (!InputEnabled)
				return;

			ScreenHandler.ActiveScreen?.KeyUp(key, keyModifiers);
		}

		public void KeyChar(char ch, KeyModifiers keyModifiers)
		{
			if (!InputEnabled)
				return;

			ScreenHandler.ActiveScreen?.KeyChar(ch, keyModifiers);
		}

		public void MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
		{
			if (!InputEnabled)
				return;

			ScreenHandler.ActiveScreen?.MouseDown(position, buttons, keyModifiers);
		}

		public void MouseUp(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
		{
			if (!InputEnabled)
				return;

			ScreenHandler.ActiveScreen?.MouseUp(position, buttons, keyModifiers);
		}

		public void MouseMove(Position position, MouseButtons buttons)
		{
			if (!InputEnabled)
				return;

			ScreenHandler.ActiveScreen?.MouseMove(position, buttons);
		}

		public void MouseWheel(Position position, float scrollX, float scrollY, MouseButtons buttons)
		{
			if (!InputEnabled)
				return;

			ScreenHandler.ActiveScreen?.MouseWheel(position, scrollX, scrollY, buttons);
		}

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
}
