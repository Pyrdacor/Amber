using Amber.Common;
using ButtonType = Amberstar.GameData.Serialization.Button;

namespace Amberstar.Game.UI;

internal class ButtonGrid
{
	public const int OffsetX = 208;
	public const int OffsetY = 37 + 108;
	public static readonly Rect Area = new(OffsetX, OffsetY, 3 * Button.Width, 3 * Button.Height);
	const int ButtonCount = 9;
	readonly Game game;
	readonly Button[] buttons = new Button[ButtonCount];

	public event Action<int>? ClickButtonAction;

	public byte PaletteIndex
	{
		get => buttons[0].PaletteIndex;
		set
		{
			for (int i = 0; i < ButtonCount; i++)
				buttons[i].PaletteIndex = value;
		}
	}

	public ButtonGrid(Game game)
	{
		this.game = game;

		for (int y = 0; y < 3; y++)
		{
			for (int x = 0; x < 3; x++)
			{
				int index = x + y * 3;
				buttons[index] = new(game, OffsetX + x * Button.Width, OffsetY + y * Button.Height, ButtonType.Empty, 10);
				buttons[index].ClickAction += () => ClickButtonAction?.Invoke(index);
			}
		}
	}

	public void SetButton(int index, ButtonType buttonType)
	{
		if (index < 0 || index >= buttons.Length)
			return;

		buttons[index].SetType(buttonType);
	}

	public void EnableButton(int index, bool enable)
	{
		if (index < 0 || index >= buttons.Length)
			return;

		buttons[index].Disabled = !enable;
	}

	public bool MouseClick(Position position)
	{
		foreach (var button in buttons)
		{
			if (button.MouseClick(position))
				return true;
		}

		return false;
	}

	public void Press(int index)
	{
		if (index < 0 || index >= buttons.Length)
			return;

		buttons[index].Press();
	}

	public void Destroy()
	{
		foreach (var button in buttons)
			button.Destroy();
	}
}
