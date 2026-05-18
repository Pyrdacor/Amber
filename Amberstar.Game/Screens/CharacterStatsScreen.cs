using Amber.Common;
using Amber.Renderer;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;
using Attribute = Amberstar.GameData.Attribute;

namespace Amberstar.Game.Screens;

internal class CharacterStatsScreen : ButtonGridScreen
{
    static readonly (Position Position, int Width, UIText Text)[] HeaderInfos =
    [
        (new(22, 50), 72, UIText.Attributes), // Attributes
        (new(22, 115), 72, UIText.Skills), // Skills
        (new(106, 50), 0, UIText.Languages), // Languages
        (new(106, 115), 0, UIText.Body), // Physical conditions
        (new(148, 115), 0, UIText.Mind), // Mental conditions
    ];
    static readonly Position AttributeOffset = new(22, 57);
    static readonly Position SkillOffset = new(22, 122);
    static readonly Position LanguageOffset = new(106, 57);
    static readonly Position PhysicalConditionOffset = new(106, 124);
    static readonly Position MentalConditionOffset = new(148, 124);
    static readonly Position[] ConditionAdvances =
    [
        new(0, 0),
        new(18, 12),
        new(0, 24),
        new(18, 36),
        new(0, 48),
    ];

    Game? game;
    IPartyMember? partyMember;
    PersonInfoView? personInfoView;
    readonly IRenderText?[] headers = new IRenderText?[5];
    readonly IRenderText?[] attributes = new IRenderText?[8];
    readonly IRenderText?[] skills = new IRenderText?[10];
    readonly IRenderText?[] languages = new IRenderText?[7];
    readonly ISprite?[] physicalConditions = new ISprite?[5];
    readonly ISprite?[] mentalConditions = new ISprite?[5];

    public override ScreenType Type { get; } = ScreenType.CharacterStats;

    internal override byte ButtonGridPaletteIndex => game?.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI] ?? 0;

    protected override void SetupButtons(ButtonGrid buttonGrid)
    {
        if (partyMember == null)
            return;

        // Upper row
        buttonGrid.SetButton(0, ButtonType.Inventory);
        buttonGrid.SetButton(2, ButtonType.Exit);
    }

    public override void Open(Game game, Action? closeAction)
	{
        this.game = game;

		base.Open(game, closeAction);

        var palette = game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI];

        game.SetLayout(Layout.Stats, palette);
        game.Cursor.CursorType = CursorType.Sword;

        SwitchToPartyMember(game.State.CurrentInventoryIndex!.Value, true);
    }

    public override void Close(Game game)
    {
        personInfoView?.Destroy();

        DeleteTexts(headers);
        DeleteTexts(attributes);
        DeleteTexts(skills);
        DeleteTexts(languages);

        headers.SetAllNull();
        attributes.SetAllNull();
        skills.SetAllNull();
        languages.SetAllNull();

        DeleteSprites(physicalConditions);
        DeleteSprites(mentalConditions);

        physicalConditions.SetAllNull();
        mentalConditions.SetAllNull();

        base.Close(game);
    }

    public override void ScreenPushed(Game game, Screen screen)
    {
        if (!screen.Transparent)
        {
            if (personInfoView != null)
                personInfoView.Visible = false;

            ShowTexts(headers, false);
            ShowTexts(attributes, false);
            ShowTexts(skills, false);
            ShowTexts(languages, false);

            ShowSprites(physicalConditions, false);
            ShowSprites(mentalConditions, false);
        }

        base.ScreenPushed(game, screen);
    }

    public override void ScreenPopped(Game game, Screen screen)
    {
        base.ScreenPopped(game, screen);

        if (!screen.Transparent)
        {
            if (personInfoView != null)
                personInfoView.Visible = true;

            ShowTexts(headers, true);
            ShowTexts(attributes, true);
            ShowTexts(skills, true);
            ShowTexts(languages, true);

            ShowSprites(physicalConditions, true);
            ShowSprites(mentalConditions, true);
        }
    }

    public void SwitchToPartyMember(int index, bool force)
    {
        if (!force && game!.State.CurrentInventoryIndex == index)
            return;

        game!.State.CurrentInventoryIndex = index;
        int partyMemberIndex = 1; // TODO: get from savegame, slot is index
        partyMember = (game!.AssetProvider.PersonLoader.LoadPerson(partyMemberIndex) as IPartyMember)!;
        var graphicLoader = game!.AssetProvider.GraphicLoader;
        var uiPaletteIndex = game!.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI];

        personInfoView?.Destroy();
        personInfoView = new(game, partyMember, partyMemberIndex, uiPaletteIndex);

        // Headers
        for (int i = 0; i < headers.Length; i++)
        {
            if (headers[i] == null)
            {
                int maxWidth = HeaderInfos[i].Width;

                if (maxWidth <= 0)
                    maxWidth = int.MaxValue;

                var headerText = game.AssetProvider.TextLoader.LoadText(new AssetIdentifier(AssetType.UIText, (int)HeaderInfos[i].Text));
                var header = headers[i] = game.TextManager.Create(headerText, maxWidth);
                var position = HeaderInfos[i].Position;

                if (maxWidth == int.MaxValue)
                    header.Show(position.X, position.Y, 0);
                else
                    header.ShowInArea(position.X, position.Y, maxWidth, 10, 0, TextAlignment.Center);
            }
            else
            {
                headers[i]!.Visible = true;
            }
        }

        // Attributes
        var valueDivider = game.AssetProvider.TextLoader.LoadText(new AssetIdentifier(AssetType.UIText, (int)UIText.NormalTwoValues)).GetString();
        for (int i = 0; i < 8; i++)
        {
            attributes[i]?.Delete();

            var attributeValue = partyMember.Attributes[(Attribute)i];
            var attributeName = game.AssetProvider.TextLoader.LoadText(new AssetIdentifier(AssetType.AttributeName, i)).GetString()[..3];
            var attributeString = Game.InsertNumberIntoString(valueDivider, "/", false, attributeValue.TotalCurrent, 3, '0');
            attributeString = Game.InsertNumberIntoString(attributeString, "/", true, attributeValue.MaxValue, 3, '0');
            var attribute = attributes[i] = game.TextManager.Create(attributeName + "  " + attributeString, 15);

            var position = AttributeOffset + new Position(0, i * 7);
            attribute.Show(position.X, position.Y, 2);
        }

        // Skills
        var percentageValueDivider = game.AssetProvider.TextLoader.LoadText(new AssetIdentifier(AssetType.UIText, (int)UIText.PercentTwoValues)).GetString();
        for (int i = 0; i < 10; i++)
        {
            skills[i]?.Delete();

            var skillValue = partyMember.Skills[(Skill)i];
            var skillName = game.AssetProvider.TextLoader.LoadText(new AssetIdentifier(AssetType.SkillName, i)).GetString()[..3];
            var skillString = Game.InsertNumberIntoString(percentageValueDivider, "%", false, skillValue.TotalCurrent, 2, '0');
            skillString = Game.InsertNumberIntoString(skillString, "/", true, skillValue.MaxValue, 2, '0');
            var skill = skills[i] = game.TextManager.Create(skillName + "  " + skillString, 15);

            var position = SkillOffset + new Position(0, i * 7);
            skill.Show(position.X, position.Y, 2);
        }

        // Languages
        for (int i = 0; i < 7; i++)
        {
            languages[i]?.Delete();
        }

        int y = LanguageOffset.Y;

        for (int i = 0; i < 7; i++)
        {
            var languageValue = (LanguageFlags)(1 << i);

            if ((partyMember.ConversationData.LearnedLanguages & languageValue) == 0)
                continue;

            var languageName = game.AssetProvider.TextLoader.LoadText(new AssetIdentifier(AssetType.LanguageName, i)).GetString();
            var language = languages[i] = game.TextManager.Create(languageName, 15);

            language.Show(LanguageOffset.X, y, 2);
            y += 7;
        }

        // Physical conditions
        if (partyMember.IsDead())
        {
            int graphicIndex = game.GraphicIndexProvider.GetStatusIconIndex(StatusIcon.Dead);
            physicalConditions[0] = game.CreateSprite(Layer.UI, PhysicalConditionOffset, new Size(16, 16), graphicIndex, uiPaletteIndex);

            for (int i = 1; i < 5; i++)
            {
                var physicalCondition = physicalConditions[i];

                if (physicalCondition != null)
                {
                    physicalCondition.Visible = false;
                    physicalConditions[i] = null;
                }                
            }
        }
        else
        {
            var conditions = (int)partyMember.PhysicalConditions;

            for (int i = 0; i < 5; i++)
            {
                if ((conditions & (1 << i)) != 0)
                {
                    int graphicIndex = game.GraphicIndexProvider.GetStatusIconIndex(((PhysicalCondition)(1 << i)).ToStatusIcon());
                    physicalConditions[i] = game.CreateSprite(Layer.UI, PhysicalConditionOffset + ConditionAdvances[i], new Size(16, 16), graphicIndex, uiPaletteIndex);
                }
                else
                {
                    var physicalCondition = physicalConditions[i];

                    if (physicalCondition != null)
                    {
                        physicalCondition.Visible = false;
                        physicalConditions[0] = null;
                    }
                }
            }
        }

        // Mental conditions
        if (partyMember.IsDead())
        {
            for (int i = 0; i < 5; i++)
            {
                var mentalCondition = mentalConditions[i];

                if (mentalCondition != null)
                {
                    mentalCondition.Visible = false;
                    mentalConditions[0] = null;
                }
            }
        }
        else
        {
            var conditions = (int)partyMember.MentalConditions;

            for (int i = 0; i < 5; i++)
            {
                if ((conditions & (1 << i)) != 0)
                {
                    int graphicIndex = game.GraphicIndexProvider.GetStatusIconIndex(((MentalCondition)(1 << i)).ToStatusIcon());
                    mentalConditions[i] = game.CreateSprite(Layer.UI, MentalConditionOffset + ConditionAdvances[i], new Size(16, 16), graphicIndex, uiPaletteIndex);
                }
                else
                {
                    var mentalCondition = mentalConditions[i];

                    if (mentalCondition != null)
                    {
                        mentalCondition.Visible = false;
                        mentalConditions[0] = null;
                    }
                }
            }
        }

        RequestButtonSetup();
    }

    protected override void ButtonClicked(int index)
    {
        switch (index)
        {
            case 0: // Stats
                game?.ScreenHandler.PopScreen();
                game?.ScreenHandler.PushScreen(ScreenType.Inventory);
                break;
            case 2:
                game?.ScreenHandler.PopScreen();
                break;
        }
    }
}
