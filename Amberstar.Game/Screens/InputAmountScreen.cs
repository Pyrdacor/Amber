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

    public override void Init()
    {
        base.Init();

        uiPalette = Game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI];

        Label CreateLabel(int x, int y, int width, IText? text = null)
        {
            Label label = new(Game)
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

        Label CreateFixedLabel(int x, int y, IText text) => CreateLabel(x, y, Game.GetMaxLineWidth(text), text);

        int x = WindowX + 16;
        int y = WindowY + 16;

        imageBackground = Game.CreateSprite(Layer.UI, new(x, y), new(16, 16), Game.GraphicIndexProvider.GetUIGraphicIndex(UIGraphic.EmptyItemSlot), uiPalette, true);
        imageBackground!.DisplayLayer = TextDisplayLayer;
        imageBackground.Visible = true;

        image = Game.CreateSprite(Layer.UI, new(x, y), new(16, 16), Game.GraphicIndexProvider.GetItemGraphicIndex(Graphic), Game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.Item]);
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

        var text = Game.AssetProvider.TextLoader.LoadText(new AssetIdentifier(AssetType.Message, (int)Message));
        message = CreateFixedLabel(x, y, text);
        message.Visible = true;

        x = WindowX + 16;
        y = WindowY + 48;

        upButton = new Button(Game, x, y, ButtonType.ArrowUp, TextDisplayLayer, uiPalette);
        upButton.ClickAction += () => ChangeAmount(1);
        upButton.RightClickAction += () => ChangeAmount(short.MaxValue);
        downButton = new Button(Game, x, y + 16, ButtonType.ArrowDown, TextDisplayLayer, uiPalette);
        downButton.ClickAction += () => ChangeAmount(-1);
        downButton.RightClickAction += () => ChangeAmount(short.MinValue);

        exitButton = new Button(Game, x + 48, y + 16, ButtonType.Exit, TextDisplayLayer, uiPalette);
        exitButton.ClickAction += () => Game.ScreenHandler.PopScreen();

        createdControls.Add(upButton);
        createdControls.Add(downButton);
        createdControls.Add(exitButton);
    }

    private void ChangeAmount(int change)
    {
        if (change == 0)
            return;

        Game.CurrentAmount = MathUtil.Limit(0, Game.CurrentAmount + change, Game.CurrentMaxAmount);
        inputValue!.SetText(Game.CurrentAmount.ToString(), 15, TextManager.TransparentPaper, uiPalette);

        upButton!.Disabled = Game.CurrentAmount == Game.CurrentMaxAmount;
        downButton!.Disabled = Game.CurrentAmount == 0;
    }

    public override void Open(Action? closeAction)
	{
		if (Game.CurrentMaxAmount <= 0)
			throw new InvalidOperationException($"{nameof(InputAmountScreen)} needs {nameof(Game.CurrentMaxAmount)} to be set beforehand.");

		base.Open(closeAction);

		InitControls();
    }

	private void InitControls()
	{
        // Create the window
        window?.Destroy();
        window = new(Game, WindowX, WindowY, WindowWidthInTiles, WindowHeightInTiles, dark: false, WindowDisplayLayer, uiPalette);

        inputValue!.SetText(Game.CurrentAmount.ToString(), 15, TextManager.TransparentPaper, uiPalette);
        inputValue.Visible = true;

        upButton!.Disabled = Game.CurrentAmount == Game.CurrentMaxAmount;
        downButton!.Disabled = Game.CurrentAmount == 0;

        createdControls.ForEach(control => control.Visible = true);

        Game.TrapMouse(window.ClientArea);
    }

	public override void Close()
	{
        window?.Destroy();
        createdControls.ForEach(label => label.Visible = false);

        if (image != null)
            image.Visible = false;

        if (imageBackground != null)
            imageBackground.Visible = false;

        Game.UntrapMouse();

        base.Close();
	}

    public override void Destroy()
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

        base.Destroy();
    }

	public override bool KeyDown(Key key, KeyModifiers keyModifiers)
	{
        switch (key)
        {
            case Key.Up:
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
                return true;
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
        if (keyModifiers != KeyModifiers.None)
            buttons = MouseButtons.Right;

        if (upButton!.MouseClick(position, buttons) ||
            downButton!.MouseClick(position, buttons) ||
            exitButton!.MouseClick(position))
            return true;

        return false;
    }
}
