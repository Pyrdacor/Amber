using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

internal class ItemScreen() : WindowScreen(WindowX, WindowY, WindowWidthInTiles, WindowHeightInTiles, WindowDisplayLayer)
{
	const int WindowX = 16;
	const int WindowY = 112;
	const int WindowWidthInTiles = 18;
	const int WindowHeightInTiles = 6;
    const byte WindowDisplayLayer = 100;
    const byte ControlDisplayLayer = 125;
    Label? itemNameLabel;
    Label? itemTypeLabel;
    Label? weightValue;
    Label? handsValue;
    Label? fingersValue;
    Label? damageValue;
    Label? shieldValue;
    readonly Label?[] classLabels = new Label?[8];
    Label? genderValue;
    Button? showDetailsButton;

    public override ScreenType Type { get; } = ScreenType.ItemView;

    public override void Init()
    {
        base.Init();

        Label CreateLabel(int x, int y, int width, IText? text = null)
        {
            Label label = AddLabel(x, y, width, 7, ControlDisplayLayer);

            if (text != null)
                label.SetText(text, width);

            return label;
        }

        var weightText = Game.LoadUIText(UIText.WeightWithColon);   // "Weight : " (with color flags)
        var handsText = Game.LoadUIText(UIText.Hands);              // "Hands   : "
        var fingersText = Game.LoadUIText(UIText.Fingers);          // "Fingers : "
        var damageText = Game.LoadUIText(UIText.Damage);            // "Damage  : "
        var shieldText = Game.LoadUIText(UIText.Protection);        // "Shield  : "
        var classesText = Game.LoadUIText(UIText.Classes);          // "---- Classes ----"
        var genderText = Game.LoadUIText(UIText.Gender);            // "Gender:"

        // === Left side ===

        itemNameLabel       = CreateLabel(16 + 3, 1, 126);
        itemTypeLabel       = CreateLabel(16 + 3, 9, 126);
        var weightLabel     = CreateLabel(0,      19, Game.GetMaxLineWidth(weightText), weightText);
        weightValue         = CreateLabel(60,     19, 54 + 91 - weightLabel.Size.Width);
        var handsLabel      = CreateLabel(0,      30, Game.GetMaxLineWidth(handsText), handsText);
        handsValue          = CreateLabel(60,     30, 54 + 91 - handsLabel.Size.Width);
        var fingersLabel    = CreateLabel(0,      38, Game.GetMaxLineWidth(fingersText), fingersText);
        fingersValue        = CreateLabel(60,     38, 54 + 91 - fingersLabel.Size.Width);
        var damageLabel     = CreateLabel(0,      46, Game.GetMaxLineWidth(damageText), damageText);
        damageValue         = CreateLabel(60,     46, 54 + 91 - damageLabel.Size.Width);
        var shieldLabel     = CreateLabel(0,      54, Game.GetMaxLineWidth(shieldText), shieldText);
        shieldValue         = CreateLabel(60,     54, 54 + 91 - shieldLabel.Size.Width);

        // === Right side ===

        // Classes label
        CreateLabel(16 + 3 + 126, 1, 112, classesText);

        int x = 16 + 3 + 126;
        int y = 8;
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

        var genderLabel = CreateLabel(x, y, Game.GetMaxLineWidth(genderText), genderText);
        genderValue     = CreateLabel(x + genderLabel.Size.Width, y, 114 - genderLabel.Size.Width);


        // === Button ===

        showDetailsButton = AddButton(224, 48, ButtonType.Eye, ControlDisplayLayer);
        showDetailsButton.Disabled = true;
        showDetailsButton.ClickAction += ShowItemDetails;
    }

    public override void Open(Action? closeAction)
	{
		if (Game.CurrentItem == null)
			throw new InvalidOperationException($"{nameof(ItemScreen)} needs {nameof(Game.CurrentItem)} to be set beforehand.");

		base.Open(closeAction);

		InitTexts(Game.CurrentItem);

        var itemPosition = new Position(WindowX + 16, WindowY + 16);
        var itemSize = new Size(16, 16);

        AddImage(0, 0, 16, 16, Game.GraphicIndexProvider.GetUIGraphicIndex(UIGraphic.EmptyItemSlot), displayLayer: 105, opaque: true);
        var itemImage = AddImage(0, 0, 16, 16, Game.GraphicIndexProvider.GetItemGraphicIndex(Game.CurrentItem.GraphicIndex), displayLayer: 110);
        itemImage.PaletteIndex = Game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.Item];

        if (showDetailsButton != null)
        {
            showDetailsButton.Disabled = !Game.CurrentItem.SlotFlags.HasFlag(ItemSlotFlags.Identified) && Game.State.TravelType != TravelType.SuperChicken;
            showDetailsButton.Visible = true;
        }
    }

	private void InitTexts(IItem item)
	{
        IText itemName = item.GetName(Game.AssetProvider.TextLoader);
        IText itemType = Game.AssetProvider.TextLoader.LoadText(new(AssetType.ItemTypeName, (int)item.Type));
        IText[] classNames = [.. Enumerable.Range(1, 8).Select(i => Game.AssetProvider.TextLoader.LoadText(new(AssetType.ClassName, i)))];
        string weightUnit = Game.LoadUIText(UIText.Grams).GetString();

        itemNameLabel!.SetText(itemName);
        itemTypeLabel!.SetText(itemType);

        weightValue!.SetText($"{item.Weight}{weightUnit}");

        handsValue!.SetText($"{(int)item.Hands}");
        fingersValue!.SetText($"{(int)item.Fingers}");
        damageValue!.SetText($"{(int)item.Damage}");
        shieldValue!.SetText($"{(int)item.Defense}");

        genderValue!.SetText(Game.LoadUIText(item.Genders switch
        {
            GenderFlags.Male => UIText.Male,
            GenderFlags.Female => UIText.Female,
            _ => UIText.Both,
        }));

        int classIndex = 1;

        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                var classLabel = classLabels[x + y * 2]!;

                if (((word)item.UsableClasses & (1 << classIndex)) != 0)
                {
                    classLabel.SetText(classNames[classIndex - 1]);
                    classLabel.Visible = true;
                }
                else
                    classLabel.Visible = false;

                classIndex++;
            }
        }
    }

	public override bool KeyDown(Key key, KeyModifiers keyModifiers)
	{
        Game.ScreenHandler.PopScreen();
        return true;
    }

	public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
        if (showDetailsButton?.MouseClick(position) == true)
            return true;

        Game.ScreenHandler.PopScreen();
        return true;
    }

    private void ShowItemDetails()
    {
        Game.ScreenHandler.PushScreen(ScreenType.ItemDetails);
    }
}
