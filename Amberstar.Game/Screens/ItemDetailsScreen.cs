using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

// TODO: Rework
// TODO: Test cursed items
internal class ItemDetailsScreen : Screen
{
	const int WindowX = 32;
	const int WindowY = 48;
	const int WindowWidthInTiles = 12;
	const int WindowHeightInTiles = 6;
    const byte WindowDisplayLayer = 150;
    const byte TextDisplayLayer = 175;
	Window? window;
    byte uiPalette = 0;
    Label? lpMaxValue;
    Label? spMaxValue;
    Label? mbwValue;
    Label? mbaValue;
    Label? attributeValue;
    Label? skillValue;
    Label? spellLabel; // If no spell just "Magic", otherwise spell school name
    Label? spellValue;
    // Holds all created labels (including the above) to easily show/hide/destroy all of them at once
    readonly List<Label> createdLabels = [];

    public override ScreenType Type { get; } = ScreenType.ItemDetails;

	public override bool Transparent => true;

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

            createdLabels.Add(label);

            return label;
        }

        Label CreateFixedLabel(int x, int y, IText text) => CreateLabel(x, y, Game.GetMaxLineWidth(text), text);

        Label CreateKeyValuePair(int x, int y, IText labelText, bool twoLines = false)
        {
            var label = CreateFixedLabel(x, y, labelText);
            var value = CreateLabel(twoLines ? x : x + label.Size.Width, twoLines ? y + 7 : y, twoLines ? 160 : 80 - label.Size.Width);

            label.Visible = true;
            value.Visible = true;

            return value;
        }

        var lpMaxText = Game.LoadUIText(UIText.LPMax);
        var spMaxText = Game.LoadUIText(UIText.SPMax);
        var mbwText = Game.LoadUIText(UIText.MBW);
        var mbaText = Game.LoadUIText(UIText.MBA);
        var attributeText = Game.LoadUIText(UIText.Attribute);
        var skillText = Game.LoadUIText(UIText.Skill);
        var spellText = Game.LoadUIText(UIText.Magic);

        int x = WindowX + 16;
        int y = WindowY + 16;

        lpMaxValue = CreateKeyValuePair(x, y, lpMaxText);
        spMaxValue = CreateKeyValuePair(x + 80, y, spMaxText);

        y += 7;

        mbwValue = CreateKeyValuePair(x, y, mbwText);
        mbaValue = CreateKeyValuePair(x + 80, y, mbaText);

        y += 7;

        attributeValue = CreateKeyValuePair(x, y, attributeText, true);

        y += 14;

        skillValue = CreateKeyValuePair(x, y, skillText, true);

        y += 14;

        spellValue = CreateKeyValuePair(x, y, spellText, true);
        spellLabel = createdLabels[^2];
    }

    public override void Open(Action? closeAction)
	{
		if (Game.CurrentItem == null)
			throw new InvalidOperationException($"{nameof(ItemDetailsScreen)} needs {nameof(Game.CurrentItem)} to be set beforehand.");

		base.Open(closeAction);

		InitText(Game.CurrentItem);
    }

	private void InitText(IItem item)
	{
        // Create the window
        window?.Destroy();
        window = new(Game, WindowX, WindowY, WindowWidthInTiles, WindowHeightInTiles, dark: false, WindowDisplayLayer, uiPalette);

        createdLabels.ForEach(label => label.Visible = true);

        bool cursed = item.Flags.HasFlag(ItemFlags.Cursed);

        void SetText(Label? label, string text, int colorIndex = 15)
        {
            label!.SetText(text, colorIndex, TextManager.TransparentPaper, uiPalette);
            label.Visible = true;
        }

        void SetValue(Label? valueLabel, int value)
        {
            if (cursed)
                value = -value;

            SetText(valueLabel, $"{value}");
        }

        SetValue(lpMaxValue, item.HitPoints);
        SetValue(spMaxValue, item.SpellPoints);

        SetValue(mbwValue, item.MagicWeaponBonus);
        SetValue(mbaValue, item.MagicArmorBonus);

        var colon = Game.LoadUIText(UIText.Colon).GetString();

        if (item.Attribute == GameData.Attribute.None)
            attributeValue!.Visible = false;
        else
        {
            var attributeName = Game.AssetProvider.TextLoader.LoadText(new(AssetType.AttributeName, (int)(item.Attribute - 1))).GetString();
            int value = cursed ? -item.AttributeValue : item.AttributeValue;
            
            SetText(attributeValue, $"{attributeName}{colon}{value}");
        }

        if (item.Skill == Skill.None)
            skillValue!.Visible = false;
        else
        {
            var skillName = Game.AssetProvider.TextLoader.LoadText(new(AssetType.SkillName, (int)(item.Skill - 1))).GetString();
            int value = cursed ? -item.SkillValue : item.SkillValue;

            SetText(skillValue, $"{skillName}{colon}{value}");
        }

        if (item.SpellCharges == 0)
            spellValue!.Visible = false;
        else
        {
            var spellSchoolName = Game.AssetProvider.TextLoader.LoadText(new(AssetType.SpellSchoolName, (int)item.SpellSchool)).GetString();
            var spellName = Game.AssetProvider.TextLoader.LoadText(new(AssetType.SpellName, ((int)item.SpellSchool - 1) * 30 + item.SpellIndex)).GetString();
            var openBracket = Game.LoadUIText(UIText.OpenBracket).GetString();
            var closingBracket = Game.LoadUIText(UIText.CloseBracket).GetString();
            string charges = item.SpellCharges == 255 ? Game.LoadUIText(UIText.ThreeStars).GetString() : $"{item.SpellCharges}";

            SetText(spellLabel, spellSchoolName, colorIndex: 1);
            SetText(spellValue, $"{spellName}{openBracket}{charges}{closingBracket}");
        }

        Game.TrapMouse(window.ClientArea);
    }

	public override void Close()
	{
        window?.Destroy();
        createdLabels.ForEach(label => label.Visible = false);

        Game.UntrapMouse();

        base.Close();
	}

    public override void Destroy()
    {
        window?.Destroy();

        createdLabels.ForEach(label => label.Destroy());
        createdLabels.Clear();

        base.Destroy();
    }

	public override bool KeyDown(Key key, KeyModifiers keyModifiers)
	{
        Game.ScreenHandler.PopScreen();
        return true;
    }

	public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
        Game.ScreenHandler.PopScreen();
        return true;
    }
}
