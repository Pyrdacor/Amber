using Amber.Common;
using Amberstar.Game;
using Silk.NET.Input;
using MousePosition = System.Numerics.Vector2;

namespace Amberstar.net
{
	using Key = Game.Key;

	internal static class InputConverter
	{
		static readonly Dictionary<Silk.NET.Input.Key, Key> keyMap = [];

		static InputConverter()
		{
			foreach (var key in Enum.GetValues<Key>())
			{
				if (key == Key.Invalid)
					continue; // don't try to map it

				if (key == Key.Number0 || key == Key.LetterA)
					continue; // special manual handling

				if (!Enum.TryParse<Silk.NET.Input.Key>(key.ToString(), out var mappedKey))
					throw new AmberException(ExceptionScope.Application, $"Key {key} could not be mapped.");

				keyMap[mappedKey] = key;
			}
		}

		public static Key Convert(Silk.NET.Input.Key key)
		{
			if (key >= Silk.NET.Input.Key.A && key <= Silk.NET.Input.Key.Z)
				return key - Silk.NET.Input.Key.A + Key.LetterA;
			else if (key >= Silk.NET.Input.Key.Number0 && key <= Silk.NET.Input.Key.Number9)
				return key - Silk.NET.Input.Key.Number0 + Key.Number0;
			else
				return keyMap.GetValueOrDefault(key, Key.Invalid);
		}

		public static KeyModifiers GetModifiers(IKeyboard keyboard)
		{
			var modifiers = KeyModifiers.None;

			if (keyboard.IsKeyPressed(Silk.NET.Input.Key.ShiftLeft) || keyboard.IsKeyPressed(Silk.NET.Input.Key.ShiftRight))
				modifiers |= KeyModifiers.Shift;
			if (keyboard.IsKeyPressed(Silk.NET.Input.Key.ControlLeft) || keyboard.IsKeyPressed(Silk.NET.Input.Key.ControlRight))
				modifiers |= KeyModifiers.Control;
			if (keyboard.IsKeyPressed(Silk.NET.Input.Key.AltLeft) || keyboard.IsKeyPressed(Silk.NET.Input.Key.AltRight))
				modifiers |= KeyModifiers.Alt;

			return modifiers;
		}

		public static MouseButtons GetMouseButtons(IMouse mouse)
		{
			var buttons = MouseButtons.None;

			if (mouse.IsButtonPressed(MouseButton.Left))
				buttons |= MouseButtons.Left;
			if (mouse.IsButtonPressed(MouseButton.Right))
				buttons |= MouseButtons.Right;
			if (mouse.IsButtonPressed(MouseButton.Middle))
				buttons |= MouseButtons.Middle;

			return buttons;
		}

		public static MouseButtons ConvertMouseButtons(MouseButton mouseButton)
		{
			return mouseButton switch
			{
				MouseButton.Left => MouseButtons.Left,
				MouseButton.Right => MouseButtons.Right,
				MouseButton.Middle => MouseButtons.Middle,
				_ => MouseButtons.None
			};
		}

		public static Position ConvertMousePosition(MousePosition position)
		{
			return new Position(MathUtil.Round(position.X), MathUtil.Round(position.Y));
		}
	}
}
