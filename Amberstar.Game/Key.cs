namespace Amberstar.Game;

public enum Key
{
	Invalid = 0,
	Up,
	Right,
	Down,
	Left,
	Enter,
	Backspace,
	Delete,
	Escape,
	Space,
	Keypad1,
	Keypad2,
	Keypad3,
	Keypad4,
	Keypad5,
	Keypad6,
	Keypad7,
	Keypad8,
	Keypad9,
	Number0 = 100,
	LetterA = 110,
}

[Flags]
public enum KeyModifiers
{
	None = 0,
	Shift = 1 << 0,
	Control = 1 << 1,
	Alt = 1 << 2,
}
