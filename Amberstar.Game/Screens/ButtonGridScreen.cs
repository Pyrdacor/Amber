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
    protected void RequestButtonSetup()
    {
        // The grid does not exist while another screen covers this one.
        if (buttonGrid != null)
            SetupButtons(buttonGrid);
    }
    protected void RequestButtonGridPaletteUpdate() => buttonGrid!.PaletteIndex = ButtonGridPaletteIndex;

    public ButtonType GetButtonType(int index) => buttonGrid?.GetButtonType(index) ?? ButtonType.Empty;

    /// <summary>
    /// Presses a button like a click (used by scenario scripts).
    /// Returns false if the button is disabled or empty.
    /// </summary>
    internal bool ScriptPressButton(int index)
    {
        if (buttonGrid == null || !buttonGrid.IsButtonEnabled(index) || buttonGrid.GetButtonType(index) is ButtonType.Empty or ButtonType.EmptyArea)
            return false;

        buttonGrid.Press(index);

        return true;
    }

    internal bool ScriptIsButtonEnabled(int index) => buttonGrid != null && buttonGrid.IsButtonEnabled(index) && buttonGrid.GetButtonType(index) is not (ButtonType.Empty or ButtonType.EmptyArea);

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
