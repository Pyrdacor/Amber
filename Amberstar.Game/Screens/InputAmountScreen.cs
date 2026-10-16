using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

internal abstract class InputAmountScreen() : WindowScreen(WindowX, WindowY, WindowWidthInTiles, WindowHeightInTiles, WindowDisplayLayer)
{
	const int WindowX = 32;
	const int WindowY = 80+32;
	const int WindowWidthInTiles = 10;
	const int WindowHeightInTiles = 6;
    const byte WindowDisplayLayer = 150;
    const byte ControlDisplayLayer = 175;
    Label? inputValue;
    Image? image;
    Label? inputLabel;
    Label? messageLabel;
    Button? upButton;
    Button? downButton;

    protected abstract ItemGraphic Graphic { get; }

    protected abstract IText InputLabelText { get; }

    protected abstract Message Message { get; }

    public override bool CloseOnEscape { get; } = false;

    /// <summary>
    /// If true, the graphic and the texts are updated whenever the
    /// window is opened (e.g. for the amount of different items).
    /// </summary>
    protected virtual bool DynamicContent => false;

    const int MaxDynamicTextWidth = 128;

    public override void Init()
    {
        base.Init();

        Label CreateLabel(int x, int y, int width, IText? text = null)
        {
            var label = AddLabel(x, y, width, 7, ControlDisplayLayer);

            if (text != null)
                label.SetText(text, width);

            return label;
        }

        Label CreateFixedLabel(int x, int y, IText text) => CreateLabel(x, y, DynamicContent ? MaxDynamicTextWidth : Game.GetMaxLineWidth(text), text);

        int x = WindowX + 16;
        int y = WindowY + 16;

        // Image background
        AddImage(x, y, 16, 16, Game.GraphicIndexProvider.GetUIGraphicIndex(UIGraphic.EmptyItemSlot), ControlDisplayLayer, true);

        image = AddImage(x, y, 16, 16, Game.GraphicIndexProvider.GetItemGraphicIndex(Graphic), ControlDisplayLayer + 5);
        image.PaletteIndex = Game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.Item];

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
        messageLabel = CreateFixedLabel(x, y, text);
        messageLabel.Visible = true;

        x = WindowX + 16;
        y = WindowY + 48;

        upButton = AddButton(x, y, ButtonType.ArrowUp, ControlDisplayLayer);
        upButton.ClickAction += () => ChangeAmount(1);
        upButton.RightClickAction += () => ChangeAmount(short.MaxValue);
        downButton = AddButton(x, y + 16, ButtonType.ArrowDown, ControlDisplayLayer);
        downButton.ClickAction += () => ChangeAmount(-1);
        downButton.RightClickAction += () => ChangeAmount(short.MinValue);

        var exitButton = AddButton(x + 48, y + 16, ButtonType.Exit, ControlDisplayLayer);
        exitButton.ClickAction += () => Game.ScreenHandler.PopScreen();
    }

    private void ChangeAmount(int change)
    {
        if (change == 0)
            return;

        Game.CurrentAmount = MathUtil.Limit(0, Game.CurrentAmount + change, Game.CurrentMaxAmount);

        UpdateControls();
    }

    private void UpdateControls()
    {
        inputValue!.SetText(Game.CurrentAmount.ToString());

        upButton!.Disabled = Game.CurrentAmount == Game.CurrentMaxAmount;
        downButton!.Disabled = Game.CurrentAmount == 0;
    }

    public override void Open(Action? closeAction)
	{
		if (Game.CurrentMaxAmount <= 0)
			throw new InvalidOperationException($"{nameof(InputAmountScreen)} needs {nameof(Game.CurrentMaxAmount)} to be set beforehand.");

		base.Open(closeAction);

        if (DynamicContent)
        {
            image!.SetTextureIndex(Game.GraphicIndexProvider.GetItemGraphicIndex(Graphic));
            inputLabel!.SetText(InputLabelText, MaxDynamicTextWidth);
            messageLabel!.SetText(Game.LoadMessageText(Message), MaxDynamicTextWidth);
        }

        UpdateControls();
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

        return base.MouseDown(position, buttons, keyModifiers);
    }
}
