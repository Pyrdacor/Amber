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
    Button? upButton;
    Button? downButton;

    public sealed override ScreenType Type { get; } = ScreenType.SelectWord;

    public sealed override bool Dark { get; } = true;

    public override void Init()
    {
        base.Init();

        var (clientWidth, clientHeight) = ClientArea.Size;
        list = AddList(0, 0, clientWidth, clientHeight - Button.Height, ControlDisplayLayer, backgroundColorIndex: 3);
        list.ItemClicked += (_, word) =>
        {
            Game.CurrentWord = word;
            Game.ScreenHandler.PopScreen();
        };

        int x = 0;
        int y = list.Area.Size.Height;

        var mouthButton = AddButton(ref x, y, ButtonType.Mouth, ControlDisplayLayer);
        mouthButton.ClickAction += () => Game.ScreenHandler.PushScreen(ScreenType.InputWord);

        upButton = AddButton(ref x, y, ButtonType.ArrowUp, ControlDisplayLayer);
        upButton.ClickAction += () => list?.Scroll(-1);
        upButton.RightClickAction += () => list?.ScrollToBegin();

        downButton = AddButton(ref x, y, ButtonType.ArrowDown, ControlDisplayLayer);
        downButton.ClickAction += () => list?.Scroll(1);
        downButton.RightClickAction += () => list?.ScrollToEnd();

        var exitButton = AddButton(ref x, y, ButtonType.Exit, ControlDisplayLayer);
        exitButton.ClickAction += () =>
        {
            Game.CurrentWord = null;
            Game.ScreenHandler.PopScreen();
        };
    }

    public override void Open(Action? closeAction)
    {
        base.Open(closeAction);

        list!.Clear();

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
    }

    public override void ScreenPopped(Screen screen)
    {
        base.ScreenPopped(screen);

        if (screen is InputWordScreen)
        {
            if (!string.IsNullOrEmpty(Game.CurrentWord))
                Game.ScreenHandler.PopScreen();
        }
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
            case Key.Up:
                if (keyModifiers == KeyModifiers.None)
                    list?.Scroll(-1);
                else
                    list?.ScrollToBegin();
                return true;
            case Key.Down:
                if (keyModifiers == KeyModifiers.None)
                    list?.Scroll(1);
                else
                    list?.ScrollToEnd();
                return true;
            case Key.PageUp:
                if (keyModifiers == KeyModifiers.None)
                    list?.Scroll(-10);
                else
                    list?.ScrollToBegin();
                return true;
            case Key.PageDown:
                if (keyModifiers == KeyModifiers.None)
                    list?.Scroll(10);
                else
                    list?.ScrollToEnd();
                return true;
            case Key.Home:
                list?.ScrollToBegin();
                return true;
            case Key.End:
                list?.ScrollToEnd();
                return true;
            case Key.Escape:
            case Key.Space:
            case Key.Enter:
                Game.CurrentWord = null;
                Game.ScreenHandler.PopScreen();
                return true;
        }

        return false;
    }

	public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
        if (buttons == MouseButtons.Left && list?.MouseClick(position) == true)
            return true;

        // Ctrl, Shift or Alt with LMB has the same effect as RMB for up and down button.
        if (keyModifiers != KeyModifiers.None &&
            (upButton?.Area.Contains(position) == true ||
            downButton?.Area.Contains(position) == true))
            buttons = MouseButtons.Right;

        return base.MouseDown(position, buttons, keyModifiers);
    }

    public override bool MouseWheel(Position position, float scrollX, float scrollY, MouseButtons buttons)
    {
        list?.MouseWheel(Math.Sign(scrollY));

        return true;
    }
}
