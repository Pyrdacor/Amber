using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

internal sealed class SelectWordScreen() : WindowScreen(WindowX, WindowY, WindowWidthInTiles, WindowHeightInTiles, WindowDisplayLayer)
{
    const int MaxWordCount = 5000;
	const int WindowX = 32;
	const int WindowY = 40;
	const int WindowWidthInTiles = 10;
	const int WindowHeightInTiles = 10;
    const byte WindowDisplayLayer = 110;
    const byte ControlDisplayLayer = 120;
    List? list;

    public sealed override ScreenType Type { get; } = ScreenType.SelectWord;

    public sealed override bool Dark { get; } = true;

    public override void Init()
    {
        base.Init();

        var (clientWidth, clientHeight) = ClientArea.Size;
        list = AddList(0, 0, clientWidth, clientHeight - Button.Height, ControlDisplayLayer, backgroundColorIndex: 3);

        List<IText> knownWords = [];

        for (word wordIndex = 0; wordIndex <= MaxWordCount; wordIndex++)
        {
            if (Game.State.IsWordKnown(wordIndex))
            {
                knownWords.Add(Game.AssetProvider.TextLoader.FromTextFragmentIndex(wordIndex));
            }
        }

        foreach (var knownWord in knownWords.OrderBy(word => word.ToString()))
        {
            list.AddItem(knownWord);
        }

        int x = 0;
        int y = list.Area.Size.Height;

        var mouthButton = AddButton(ref x, y, ButtonType.Mouth, ControlDisplayLayer);
        mouthButton.ClickAction += () => Game.ScreenHandler.PushScreen(ScreenType.InputWord);

        var upButton = AddButton(ref x, y, ButtonType.ArrowUp, ControlDisplayLayer);
        //upButton.ClickAction += () => Game.ScreenHandler.PopScreen();

        var downButton = AddButton(ref x, y, ButtonType.ArrowDown, ControlDisplayLayer);
        //downButton.ClickAction += () => Game.ScreenHandler.PopScreen();

        var exitButton = AddButton(ref x, y, ButtonType.Exit, ControlDisplayLayer);
        exitButton.ClickAction += () => Game.ScreenHandler.PopScreen();
    }

    /*private void ChangeAmount(int change)
    {
        if (change == 0)
            return;

        Game.CurrentAmount = MathUtil.Limit(0, Game.CurrentAmount + change, Game.CurrentMaxAmount);
        inputValue!.SetText(Game.CurrentAmount.ToString(), 15, TextManager.TransparentPaper, uiPalette);

        upButton!.Disabled = Game.CurrentAmount == Game.CurrentMaxAmount;
        downButton!.Disabled = Game.CurrentAmount == 0;
    }*/

	public override bool KeyDown(Key key, KeyModifiers keyModifiers)
	{
        switch (key)
        {
            /*case Key.Up:
                if (keyModifiers == KeyModifiers.None)
                    ChangeAmount(1);
                else
                    ChangeAmount(Game.CurrentMaxAmount - Game.CurrentAmount);
                return true;
            case Key.Down:
                if (keyModifiers == KeyModifiers.None)
                    ChangeAmount(-1);
                else
                    ChangeAmount(-Game.CurrentAmount);
                return true;
            case Key.PageUp:
                if (keyModifiers == KeyModifiers.None)
                    ChangeAmount(10);
                else
                    ChangeAmount(Game.CurrentMaxAmount - Game.CurrentAmount);
                return true;
            case Key.PageDown:
                if (keyModifiers == KeyModifiers.None)
                    ChangeAmount(-10);
                else
                    ChangeAmount(-Game.CurrentAmount);
                return true;*/
            case Key.Escape:
                Game.CurrentAmount = 0;
                Game.ScreenHandler.PopScreen();
                return true;
            case Key.Space:
            case Key.Enter:
                Game.ScreenHandler.PopScreen();
                return true;
        }

        return false;
    }

	public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
        if (keyModifiers != KeyModifiers.None && !list!.Area.Contains(position))
            buttons = MouseButtons.Right;

        return base.MouseDown(position, buttons, keyModifiers);
    }
}
