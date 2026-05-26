using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

// TODO: Test cursed items
internal class ItemDetailsScreen() : WindowScreen(WindowX, WindowY, WindowWidthInTiles, WindowHeightInTiles, WindowDisplayLayer)
{
	const int WindowX = 32;
	const int WindowY = 48;
	const int WindowWidthInTiles = 12;
	const int WindowHeightInTiles = 6;
    const byte WindowDisplayLayer = 150;
    const byte TextDisplayLayer = 175;
    byte uiPalette = 0;
    Label? lpMaxValue;
    Label? spMaxValue;
    Label? mbwValue;
    Label? mbaValue;
    Label? attributeValue;
    Label? skillValue;
    Label? spellLabel; // If no spell just "Magic", otherwise spell school name
    Label? spellValue;

    public override ScreenType Type { get; } = ScreenType.ItemDetails;

    public override bool CloseOnNextInput { get; } = true;

    public override void Init()
    {
        base.Init();

        uiPalette = Game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI];

        Label CreateLabel(int x, int y, int width, IText? text = null)
        {
            Label label = AddLabel(x, y, width, 7, TextDisplayLayer);

            if (text != null)
                label.SetText(text, width);

            return label;
        }

        Label CreateFixedLabel(int x, int y, IText text) => CreateLabel(x, y, Game.GetMaxLineWidth(text), text);

        Label CreateKeyValuePairAndGetLabel(int x, int y, IText labelText, out Label label, bool twoLines = false)
        {
            label = CreateFixedLabel(x, y, labelText);
            var value = CreateLabel(twoLines ? x : x + label.Size.Width, twoLines ? y + 7 : y, twoLines ? 160 : 80 - label.Size.Width);

            label.Visible = true;
            value.Visible = true;

            return value;
        }

        Label CreateKeyValuePair(int x, int y, IText labelText, bool twoLines = false)
            => CreateKeyValuePairAndGetLabel(x, y, labelText, out _, twoLines);

        var lpMaxText = Game.LoadUIText(UIText.LPMax);
        var spMaxText = Game.LoadUIText(UIText.SPMax);
        var mbwText = Game.LoadUIText(UIText.MBW);
        var mbaText = Game.LoadUIText(UIText.MBA);
        var attributeText = Game.LoadUIText(UIText.Attribute);
        var skillText = Game.LoadUIText(UIText.Skill);
        var spellText = Game.LoadUIText(UIText.Magic);

        int x = 0;
        int y = 0;

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

        spellValue = CreateKeyValuePairAndGetLabel(x, y, spellText, out spellLabel, true);
    }

    public override void Open(Action? closeAction)
	{
		if (Game.CurrentItem == null)
			throw new InvalidOperationException($"{nameof(ItemDetailsScreen)} needs {nameof(Game.CurrentItem)} to be set beforehand.");

		base.Open(closeAction);

		InitValues(Game.CurrentItem);
    }

	private void InitValues(IItem item)
	{
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
    }
}
