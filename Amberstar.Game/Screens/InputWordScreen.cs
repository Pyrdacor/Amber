using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;

namespace Amberstar.Game.Screens;

internal class InputWordScreen() : WindowScreen(WindowX, WindowY, WindowWidthInTiles, WindowHeightInTiles, WindowDisplayLayer)
{
	const int WindowX = 48;
	const int WindowY = 80;
	const int WindowWidthInTiles = 10;
	const int WindowHeightInTiles = 4;
	const int WindowDisplayLayer = 180;
	const int ControlDisplayLayer = 190;
	Input? input;

	public override ScreenType Type { get; } = ScreenType.InputWord;

	public override bool CloseOnRightClick { get; } = true;

    public override bool CloseOnSpace { get; } = false;

    public override void Init()
    {
        base.Init();

		AddLabel(0, 7, Game.LoadUIText(UIText.EnterWord), 144, 7, ControlDisplayLayer);
	
		input = AddInput(-1, 16, 128, ControlDisplayLayer);
    }

    public override void Open(Action? closeAction)
    {
        Game.CurrentWord = "";
        input?.Clear();

        base.Open(closeAction);
    }

    public override bool KeyChar(char ch, KeyModifiers keyModifiers)
    {
		if (input?.KeyChar(ch) == true)
			return true;

        return base.KeyChar(ch, keyModifiers);
    }

    public override bool KeyDown(Key key, KeyModifiers keyModifiers)
    {
		if (key == Key.Escape)
		{
			Game.CurrentWord = null;
			Game.ScreenHandler.PopScreen();
			return true;
		}
		else if (key == Key.Enter)
		{
			Game.CurrentWord = input!.Text.Length == 0 ? null : input.Text;
            Game.ScreenHandler.PopScreen();
            return true;
        }

		if (input?.KeyDown(key) == true)
			return true;

        return base.KeyDown(key, keyModifiers);
    }

    public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
    {
		if (buttons == MouseButtons.Right)
		{
            Game.CurrentWord = null;
            Game.ScreenHandler.PopScreen();
            return true;
        }

        return base.MouseDown(position, buttons, keyModifiers);
    }
}
