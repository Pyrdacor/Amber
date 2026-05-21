using Amber.Common;
using Amber.Renderer.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

internal class ItemScreen : Screen
{
	const int WindowX = 16;
	const int WindowY = 112;
	const int WindowWidthInTiles = 18;
	const int WindowHeightInTiles = 6;
    const byte WindowDisplayLayer = 100;
    const byte TextDisplayLayer = 125;
    Game? game;
	Window? window;
    byte uiPalette = 0;
    ISprite? itemBackground;
    ISprite? itemSprite;
    Label? itemNameLabel;
    Label? itemTypeLabel;
    Label? weightValue;
    Label? handsValue;
    Label? fingersValue;
    Label? damageValue;
    Label? shieldValue;
    readonly Label?[] classLabels = new Label?[8];
    Label? genderValue;
    // Holds all created labels (including the above) to easily show/hide/destroy all of them at once
    readonly List<Label> createdLabels = [];
    Button? showDetailsButton;

    public override ScreenType Type { get; } = ScreenType.ItemView;

	public override bool Transparent => true;

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
                Area = new(WindowX + x, WindowY + y, width, 7),
            };

            if (text != null)
                label.SetText(text, width, 15, TextManager.DefaultPaperColorIndex, uiPalette);

            createdLabels.Add(label);

            return label;
        }

        var weightText = game.LoadUIText(UIText.WeightWithColon); // "Weight : " (with color flags)
        var handsText = game.LoadUIText(UIText.Hands);          // "Hands   : "
        var fingersText = game.LoadUIText(UIText.Fingers);      // "Fingers : "
        var damageText = game.LoadUIText(UIText.Damage);        // "Damage  : "
        var shieldText = game.LoadUIText(UIText.Protection);    // "Shield  : "
        var classesText = game.LoadUIText(UIText.Classes);      // "---- Classes ----"
        var genderText = game.LoadUIText(UIText.Gender);        // "Gender:"

        // === Left side ===

        itemNameLabel = CreateLabel(32 + 3, 16 + 1, 126);
        itemTypeLabel       = CreateLabel(32 + 3, 24 + 1, 126);
        var weightLabel     = CreateLabel(16,     34 + 1, game.GetMaxLineWidth(weightText), weightText);
        weightValue         = CreateLabel(76,     34 + 1, 54 + 91 - weightLabel.Size.Width);
        var handsLabel      = CreateLabel(16,     45 + 1, game.GetMaxLineWidth(handsText), handsText);
        handsValue          = CreateLabel(76,     45 + 1, 54 + 91 - handsLabel.Size.Width);
        var fingersLabel    = CreateLabel(16,     53 + 1, game.GetMaxLineWidth(fingersText), fingersText);
        fingersValue        = CreateLabel(76,     53 + 1, 54 + 91 - fingersLabel.Size.Width);
        var damageLabel     = CreateLabel(16,     61 + 1, game.GetMaxLineWidth(damageText), damageText);
        damageValue         = CreateLabel(76,     61 + 1, 54 + 91 - damageLabel.Size.Width);
        var shieldLabel     = CreateLabel(16,     69 + 1, game.GetMaxLineWidth(shieldText), shieldText);
        shieldValue         = CreateLabel(76,     69 + 1, 54 + 91 - shieldLabel.Size.Width);

        // === Right side ===

        // Classes label
        CreateLabel(32 + 3 + 126, 16 + 1, 112, classesText);

        int x = 32 + 3 + 126;
        int y = 24;
        int width = 54;

        for (int i = 0; i < classLabels.Length; i++)
        {
            classLabels[i] = CreateLabel(x, y, width);

            if (i % 2 == 0)
                x += width;
            else
            {
                x -= width;
                y += 7;
            }
        }

        y += 3;

        var genderLabel = CreateLabel(x, y, game.GetMaxLineWidth(genderText), genderText);
        genderValue     = CreateLabel(x + genderLabel.Size.Width, y, 114 - genderLabel.Size.Width);


        // === Button ===

        showDetailsButton = new(game, WindowX + 240, WindowY + 64, ButtonType.Eye, TextDisplayLayer, uiPalette)
        {
            Disabled = true
        };
        showDetailsButton.ClickAction += ShowItemDetails;
    }

    public override void Open(Game game, Action? closeAction)
	{
		if (game.CurrentItem == null)
			throw new InvalidOperationException($"{nameof(ItemScreen)} needs {nameof(Game.CurrentItem)} to be set beforehand.");

		base.Open(game, closeAction);

		this.game = game;

		InitText(game.CurrentItem);

        var itemPosition = new Position(WindowX + 16, WindowY + 16);
        var itemSize = new Size(16, 16);

        itemBackground = game.CreateSprite(Layer.UI, itemPosition, itemSize,
            game.GraphicIndexProvider.GetUIGraphicIndex(UIGraphic.EmptyItemSlot),
            uiPalette);
        itemBackground!.DisplayLayer = 105;
        itemBackground.Visible = true;

        itemSprite = game.CreateSprite(Layer.UI, itemPosition, itemSize,
            game.GraphicIndexProvider.GetItemGraphicIndex(game.CurrentItem.GraphicIndex),
            game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.Item]);
        itemSprite!.DisplayLayer = 110;
        itemSprite.Visible = true;

        if (showDetailsButton != null)
        {
            showDetailsButton.Disabled = !game.CurrentItem.SlotFlags.HasFlag(ItemSlotFlags.Identified) && game.State.TravelType != TravelType.SuperChicken;
            showDetailsButton.Visible = true;
        }

        game.TrapMouse(window!.ClientArea);
    }

	private void InitText(IItem item)
	{
        // Create the window
        window?.Destroy();
        window = new(game!, WindowX, WindowY, WindowWidthInTiles, WindowHeightInTiles, dark: false, WindowDisplayLayer, uiPalette);

        IText itemName = item.GetName(game!.AssetProvider.TextLoader);
        IText itemType = game.AssetProvider.TextLoader.LoadText(new(AssetType.ItemTypeName, (int)item.Type));
        IText[] classNames = [.. Enumerable.Range(1, 8).Select(i => game.AssetProvider.TextLoader.LoadText(new(AssetType.ClassName, i)))];
        string weightUnit = game.LoadUIText(UIText.Grams).GetString();

        itemNameLabel!.SetText(itemName, null, 15, TextManager.DefaultPaperColorIndex, uiPalette);
        itemTypeLabel!.SetText(itemType, null, 15, TextManager.DefaultPaperColorIndex, uiPalette);

        weightValue!.SetText($"{item.Weight}{weightUnit}", 15, TextManager.DefaultPaperColorIndex, uiPalette);

        handsValue!.SetText($"{(int)item.Hands}", 15, TextManager.DefaultPaperColorIndex, uiPalette);
        fingersValue!.SetText($"{(int)item.Fingers}", 15, TextManager.DefaultPaperColorIndex, uiPalette);
        damageValue!.SetText($"{(int)item.Damage}", 15, TextManager.DefaultPaperColorIndex, uiPalette);
        shieldValue!.SetText($"{(int)item.Defense}", 15, TextManager.DefaultPaperColorIndex, uiPalette);

        genderValue!.SetText(game.LoadUIText(item.Genders switch
        {
            GenderFlags.Male => UIText.Male,
            GenderFlags.Female => UIText.Female,
            _ => UIText.Both,
        }), null, 15, TextManager.DefaultPaperColorIndex, uiPalette);

        createdLabels.ForEach(label => label.Visible = true);

        int classIndex = 1;

        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                var classLabel = classLabels[x + y * 2]!;

                if (((word)item.UsableClasses & (1 << classIndex)) != 0)
                {
                    classLabel.SetText(classNames[classIndex - 1], null, 15, TextManager.DefaultPaperColorIndex, uiPalette);
                    classLabel.Visible = true;
                }
                else
                    classLabel.Visible = false;

                classIndex++;
            }
        }
    }

	public override void Close(Game game)
	{
        window?.Destroy();
        createdLabels.ForEach(label => label.Visible = false);
        itemSprite!.Visible = false;
        itemBackground!.Visible = false;

        if (showDetailsButton != null)
            showDetailsButton.Visible = false;

        game.UntrapMouse();

        base.Close(game);
	}

    public override void ScreenPushed(Game game, Screen screen)
    {
        game.UntrapMouse();

        base.ScreenPushed(game, screen);        
    }

    public override void ScreenPopped(Game game, Screen screen)
    {
        base.ScreenPopped(game, screen);

        game.TrapMouse(window!.ClientArea);
    }

    public override void Destroy(Game game)
    {
        window?.Destroy();

        createdLabels.ForEach(label => label.Destroy());
        createdLabels.Clear();

        if (itemBackground != null)
        {
            itemBackground.Visible = false;
            itemBackground = null;
        }

        if (itemSprite != null)
        {
            itemSprite.Visible = false;
            itemSprite = null;
        }

        showDetailsButton?.Destroy();
        showDetailsButton = null;

        base.Destroy(game);
    }

	public override bool KeyDown(Key key, KeyModifiers keyModifiers)
	{
        game!.ScreenHandler.PopScreen();
        return true;
    }

	public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
        if (showDetailsButton?.MouseClick(position) == true)
            return true;

        game!.ScreenHandler.PopScreen();
        return true;
    }

    private void ShowItemDetails()
    {
        game!.ScreenHandler.PushScreen(ScreenType.ItemDetails);
    }
}
