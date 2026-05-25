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

    public override void Open(Action? closeAction)
	{
		base.Open(closeAction);

        if (buttonGrid == null)
        {
            buttonGrid = new(Game)
            {
                PaletteIndex = ButtonGridPaletteIndex
            };
            buttonGrid.ClickButtonAction += ButtonClicked;
        }

        SetupButtons(buttonGrid);
    }

	public override void Close()
	{
        if (buttonGrid != null)
        {
            buttonGrid.ClickButtonAction -= ButtonClicked;
            buttonGrid.Destroy();
            buttonGrid = null;
        }

        base.Close();
    }

	public override void ScreenPushed(Screen screen)
	{
		if (!screen.Transparent && buttonGrid != null)
        {
			buttonGrid.ClickButtonAction -= ButtonClicked;
            buttonGrid.Destroy();
			buttonGrid = null;
        }

        base.ScreenPushed(screen);
    }

	public override void ScreenPopped(Screen screen)
	{
        if (!screen.Transparent)
        {
			if (buttonGrid == null)
			{
                buttonGrid = new(Game)
                {
                    PaletteIndex = ButtonGridPaletteIndex
                };
                buttonGrid.ClickButtonAction += ButtonClicked;
			}

            SetupButtons(buttonGrid);
        }

        base.ScreenPopped(screen);
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
