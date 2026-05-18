using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

internal abstract class ButtonGridScreen : Screen
{
    ButtonGrid? buttonGrid;

	protected abstract void SetupButtons(ButtonGrid buttonGrid);
	protected abstract void ButtonClicked(int index);
    internal abstract byte ButtonGridPaletteIndex { get; }
    protected void RequestButtonSetup() => SetupButtons(buttonGrid!);
    protected void RequestButtonGridPaletteUpdate() => buttonGrid!.PaletteIndex = ButtonGridPaletteIndex;

    public ButtonType GetButtonType(int index) => buttonGrid?.GetButtonType(index) ?? ButtonType.Empty;

    public override void Open(Game game, Action? closeAction)
	{
		base.Open(game, closeAction);

        if (buttonGrid == null)
        {
            buttonGrid = new(game);
            buttonGrid.PaletteIndex = ButtonGridPaletteIndex;
            buttonGrid.ClickButtonAction += ButtonClicked;
        }

        SetupButtons(buttonGrid);
    }

	public override void Close(Game game)
	{
        if (buttonGrid != null)
        {
            buttonGrid.ClickButtonAction -= ButtonClicked;
            buttonGrid.Destroy();
            buttonGrid = null;
        }

        base.Close(game);
    }

	public override void ScreenPushed(Game game, Screen screen)
	{
		if (!screen.Transparent && buttonGrid != null)
        {
			buttonGrid.ClickButtonAction -= ButtonClicked;
            buttonGrid.Destroy();
			buttonGrid = null;
        }
    }

	public override void ScreenPopped(Game game, Screen screen)
	{
        if (!screen.Transparent)
        {
			if (buttonGrid == null)
			{
				buttonGrid = new(game);
                buttonGrid.PaletteIndex = ButtonGridPaletteIndex;
                buttonGrid.ClickButtonAction += ButtonClicked;
			}

            SetupButtons(buttonGrid);
        }
    }

	public override bool KeyUp(Key key, KeyModifiers keyModifiers)
	{
		if (keyModifiers == KeyModifiers.None && key >= Key.Keypad1 && key <= Key.Keypad9)
		{
			int buttonIndex = (int)key - (int)Key.Keypad1;

            if (buttonGrid?.IsButtonEnabled(buttonIndex) == true)
                ButtonClicked(buttonIndex);

            return true;
		}

        return base.KeyUp(key, keyModifiers);
    }

	public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		if (buttonGrid?.MouseClick(position) == true)
            return true;

        return base.MouseDown(position, buttons, keyModifiers);
    }
}
