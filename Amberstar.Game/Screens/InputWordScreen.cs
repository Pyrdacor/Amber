using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

internal sealed class InputWordScreen() : WindowScreen(WindowX, WindowY, WindowWidthInTiles, WindowHeightInTiles, WindowDisplayLayer)
{
    const int MaxWordCount = 5000;
	const int WindowX = 32;
	const int WindowY = 40;
	const int WindowWidthInTiles = 10;
	const int WindowHeightInTiles = 10;
    const byte WindowDisplayLayer = 110;
    const byte ControlDisplayLayer = 120;
    Game? game;
    byte uiPalette = 0;
    List? list;

    public sealed override ScreenType Type { get; } = ScreenType.InputWord;

    public override void Init(Game game)
    {
        base.Init(game);

        this.game = game;

        uiPalette = game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI];

        var (clientWidth, clientHeight) = ClientArea.Size;
        list = AddList(0, 0, clientWidth, clientHeight - Button.Height, ControlDisplayLayer, backgroundColorIndex: 3);

        List<IText> knownWords = [];

        for (word wordIndex = 0; wordIndex <= MaxWordCount; wordIndex++)
        {
            if (game.State.IsWordKnown(wordIndex))
            {
                knownWords.Add(game.AssetProvider.TextLoader.FromTextFragmentIndex(wordIndex));
            }
        }

        foreach (var knownWord in knownWords.OrderBy(word => word.ToString()))
        {
            list.AddItem(knownWord);
        }

        int x = 0;
        int y = list.Area.Size.Height;

        /*upButton = new Button(game, x, y, ButtonType.ArrowUp, TextDisplayLayer, uiPalette);
        upButton.ClickAction += () => ChangeAmount(1);
        upButton.RightClickAction += () => ChangeAmount(short.MaxValue);
        downButton = new Button(game, x, y + 16, ButtonType.ArrowDown, TextDisplayLayer, uiPalette);
        downButton.ClickAction += () => ChangeAmount(-1);
        downButton.RightClickAction += () => ChangeAmount(short.MinValue);*/

        var mouthButton = AddButton(ref x, y, ButtonType.Mouth, ControlDisplayLayer);
        //mouthButton.ClickAction += () => game.ScreenHandler.PopScreen();

        var upButton = AddButton(ref x, y, ButtonType.ArrowUp, ControlDisplayLayer);
        //upButton.ClickAction += () => game.ScreenHandler.PopScreen();

        var downButton = AddButton(ref x, y, ButtonType.ArrowDown, ControlDisplayLayer);
        //downButton.ClickAction += () => game.ScreenHandler.PopScreen();

        var exitButton = AddButton(ref x, y, ButtonType.Exit, ControlDisplayLayer);
        exitButton.ClickAction += () => game.ScreenHandler.PopScreen();
    }

    /*private void ChangeAmount(int change)
    {
        if (change == 0)
            return;

        game!.CurrentAmount = MathUtil.Limit(0, game.CurrentAmount + change, game.CurrentMaxAmount);
        inputValue!.SetText(game.CurrentAmount.ToString(), 15, TextManager.TransparentPaper, uiPalette);

        upButton!.Disabled = game.CurrentAmount == game.CurrentMaxAmount;
        downButton!.Disabled = game.CurrentAmount == 0;
    }*/

    public override void Open(Game game, Action? closeAction)
	{
		base.Open(game, closeAction);

        game!.TrapMouse(ClientArea);
    }

	public override void Close(Game game)
	{
        game.UntrapMouse();

        base.Close(game);
	}

    public override void Destroy(Game game)
    {
        base.Destroy(game);
    }

	public override bool KeyDown(Key key, KeyModifiers keyModifiers)
	{
        switch (key)
        {
            /*case Key.Up:
                if (keyModifiers == KeyModifiers.None)
                    ChangeAmount(1);
                else
                    ChangeAmount(game!.CurrentMaxAmount - game.CurrentAmount);
                return true;
            case Key.Down:
                if (keyModifiers == KeyModifiers.None)
                    ChangeAmount(-1);
                else
                    ChangeAmount(-game!.CurrentAmount);
                return true;
            case Key.PageUp:
                if (keyModifiers == KeyModifiers.None)
                    ChangeAmount(10);
                else
                    ChangeAmount(game!.CurrentMaxAmount - game.CurrentAmount);
                return true;
            case Key.PageDown:
                if (keyModifiers == KeyModifiers.None)
                    ChangeAmount(-10);
                else
                    ChangeAmount(-game!.CurrentAmount);
                return true;*/
            case Key.Escape:
                game!.CurrentAmount = 0;
                game.ScreenHandler.PopScreen();
                return true;
            case Key.Space:
            case Key.Enter:
                game!.ScreenHandler.PopScreen();
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
