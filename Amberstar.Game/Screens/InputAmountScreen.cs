using Amber.Common;
using Amber.Renderer.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

internal abstract class InputAmountScreen : Screen
{
	const int WindowX = 32;
	const int WindowY = 80+32;
	const int WindowWidthInTiles = 10;
	const int WindowHeightInTiles = 6;
    const byte WindowDisplayLayer = 150;
    const byte TextDisplayLayer = 175;
    Game? game;
	Window? window;
    byte uiPalette = 0;
    ISprite? imageBackground;
    ISprite? image;
    Label? inputLabel;
    Label? inputValue;
    Label? message;
    Button? upButton;
    Button? downButton;
    Button? exitButton;
    // Holds all created controls (including the above) to easily show/hide/destroy all of them at once
    readonly List<Control> createdControls = [];

	public sealed override bool Transparent => true;

    protected abstract ItemGraphic Graphic { get; }

    protected abstract IText InputLabelText { get; }

    protected abstract Message Message { get; }

    public override void Init(Game game)
    {
        base.Init(game);

        uiPalette = game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI];

        Label CreateLabel(int x, int y, int width, IText? text = null)
        {
            Label label = new(game)
            {
                DisplayLayer = TextDisplayLayer,
                Alignment = TextAlignment.Left,
                Area = new(x, y, width, 7),
            };

            if (text != null)
                label.SetText(text, width, 15, TextManager.DefaultPaperColorIndex, uiPalette);

            createdControls.Add(label);

            return label;
        }

        Label CreateFixedLabel(int x, int y, IText text) => CreateLabel(x, y, game.GetMaxLineWidth(text), text);

        int x = WindowX + 16;
        int y = WindowY + 16;

        imageBackground = game.CreateSprite(Layer.UI, new(x, y), new(16, 16), game.GraphicIndexProvider.GetUIGraphicIndex(UIGraphic.EmptyItemSlot), uiPalette, true);
        imageBackground!.DisplayLayer = TextDisplayLayer;
        imageBackground.Visible = true;

        image = game.CreateSprite(Layer.UI, new(x, y), new(16, 16), game.GraphicIndexProvider.GetItemGraphicIndex(Graphic), game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.Item]);
        image!.DisplayLayer = TextDisplayLayer + 2;
        image.Visible = true;

        x += 16 + 3;
        y += 2;

        inputLabel = CreateFixedLabel(x, y, InputLabelText);
        inputLabel.Visible = true;

        y += 11;

        inputValue = CreateLabel(x, y, 52);
        inputValue.Alignment = TextAlignment.Right;

        x = WindowX + 16 + 1;
        y += 9;

        var text = game.AssetProvider.TextLoader.LoadText(new AssetIdentifier(AssetType.Message, (int)Message));
        message = CreateFixedLabel(x, y, text);
        message.Visible = true;

        x = WindowX + 16;
        y = WindowY + 48;

        upButton = new Button(game, x, y, ButtonType.ArrowUp, TextDisplayLayer, uiPalette);
        upButton.ClickAction += () => ChangeAmount(1);
        upButton.RightClickAction += () => ChangeAmount(short.MaxValue);
        downButton = new Button(game, x, y + 16, ButtonType.ArrowDown, TextDisplayLayer, uiPalette);
        downButton.ClickAction += () => ChangeAmount(-1);
        downButton.RightClickAction += () => ChangeAmount(short.MinValue);

        exitButton = new Button(game, x + 48, y + 16, ButtonType.Exit, TextDisplayLayer, uiPalette);
        exitButton.ClickAction += () => game.ScreenHandler.PopScreen();

        createdControls.Add(upButton);
        createdControls.Add(downButton);
        createdControls.Add(exitButton);
    }

    private void ChangeAmount(int change)
    {
        if (change == 0)
            return;

        game!.CurrentAmount = MathUtil.Limit(0, game.CurrentAmount + change, game.CurrentMaxAmount);
        inputValue!.SetText(game.CurrentAmount.ToString(), 15, TextManager.TransparentPaper, uiPalette);

        upButton!.Disabled = game.CurrentAmount == game.CurrentMaxAmount;
        downButton!.Disabled = game.CurrentAmount == 0;
    }

    public override void Open(Game game, Action? closeAction)
	{
		if (game.CurrentMaxAmount <= 0)
			throw new InvalidOperationException($"{nameof(InputAmountScreen)} needs {nameof(Game.CurrentMaxAmount)} to be set beforehand.");

		base.Open(game, closeAction);

		this.game = game;

		Init();
    }

	private void Init()
	{
        // Create the window
        window?.Destroy();
        window = new(game!, WindowX, WindowY, WindowWidthInTiles, WindowHeightInTiles, dark: false, WindowDisplayLayer, uiPalette);

        inputValue!.SetText(game!.CurrentAmount.ToString(), 15, TextManager.TransparentPaper, uiPalette);
        inputValue.Visible = true;

        upButton!.Disabled = game.CurrentAmount == game.CurrentMaxAmount;
        downButton!.Disabled = game.CurrentAmount == 0;

        createdControls.ForEach(control => control.Visible = true);

        game!.TrapMouse(window.ClientArea);
    }

	public override void Close(Game game)
	{
        window?.Destroy();
        createdControls.ForEach(label => label.Visible = false);

        if (image != null)
            image.Visible = false;

        if (imageBackground != null)
            imageBackground.Visible = false;

        game.UntrapMouse();

        base.Close(game);
	}

    public override void Destroy(Game game)
    {
        window?.Destroy();

        if (image != null)
        {
            image.Visible = false;
            image = null;
        }

        if (imageBackground != null)
        {
            imageBackground.Visible = false;
            imageBackground = null;
        }

        createdControls.ForEach(label => label.Destroy());
        createdControls.Clear();

        base.Destroy(game);
    }

	public override bool KeyDown(Key key, KeyModifiers keyModifiers)
	{
        switch (key)
        {
            case Key.Up:
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
                return true;
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
        if (keyModifiers != KeyModifiers.None)
            buttons = MouseButtons.Right;

        if (upButton!.MouseClick(position, buttons) ||
            downButton!.MouseClick(position, buttons) ||
            exitButton!.MouseClick(position))
            return true;

        return false;
    }
}
