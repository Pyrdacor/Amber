using Amber.Common;
using Amber.Renderer;
using Amber.Renderer.Common;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;
using static System.Net.Mime.MediaTypeNames;

namespace Amberstar.Game.UI;

internal class PersonInfoView
{
    enum InfoText : int
    {
        Race,
        Gender,
        Age,
        ClassAndLevel,
        EP,
        LP,
        SP,
        SLP,
        GoldAndFood,
        Damage,
        Protection,
        Count,
    }

    readonly IColoredRect portraitBackground;
    readonly ISprite portrait;
    readonly IRenderText name;
    readonly IRenderText?[] personInfoTexts = new IRenderText?[(int)InfoText.Count];
    readonly ISprite? swordIcon;
    readonly ISprite? shieldIcon;
    bool visible = true;
    bool extended = true;

    public PersonInfoView(Game game, IPerson person, int personIndex, byte palette)
    {
        var paperColor = TextManager.TransparentPaper;
        int portraitIndex = game.GraphicIndexProvider.GetPersonPortraitIndex(personIndex);

        var portraitPosition = new Position(208, 49);
        var portraitSize = new Size(32, 34);
        portraitBackground = game.CreateColoredRect(Layer.UI, portraitPosition, portraitSize, Color.Black)!;
        portrait = game.CreateSprite(Layer.UI, portraitPosition, portraitSize, portraitIndex, palette)!;
        portraitBackground!.DisplayLayer = 0;
        portrait!.DisplayLayer = 2;

        name = game.TextManager.Create(person.Name, 1, paperColor, palette);
        name.ShowInArea(portraitPosition.X, portraitPosition.Y + portraitSize.Height + 1, 96, 7, 2, TextAlignment.Center);

        var textLoader = game.AssetProvider.TextLoader;
        var textPositionX = portraitPosition.X + portraitSize.Width + 2;
        var textPositionY = portraitPosition.Y;
        int maxTextWidth = Game.VirtualScreenWidth - textPositionX;

        if (person.Race < Race.Monster)
        {
            // Race
            var raceName = textLoader.LoadText(new AssetIdentifier(AssetType.RaceName, (int)person.Race));
            personInfoTexts[(int)InfoText.Race] = game.TextManager.Create(raceName, maxTextWidth, 15, paperColor, palette);
        }
        else
        {
            personInfoTexts[(int)InfoText.Race] = null; // hidden
        }

        // Gender
        var genderText = person.Gender == Gender.Male ? UIText.Male : UIText.Female;
        var genderName = textLoader.LoadText(new AssetIdentifier(AssetType.UIText, (int)genderText)).GetString();
        personInfoTexts[(int)InfoText.Gender] = game.TextManager.Create(genderName, 15, paperColor, palette);

        if (person.Race < Race.Monster)
        {
            // Age
            var ageLabelName = textLoader.LoadText(new AssetIdentifier(AssetType.UIText, (int)UIText.Age)).GetString();
            var ageString = $"{ageLabelName}{person.Age.CurrentValue}";
            personInfoTexts[(int)InfoText.Age] = game.TextManager.Create(ageString, 15, paperColor, palette);

            // Class + Level
            if (person.Class != Class.None)
            {
                var className = textLoader.LoadText(new AssetIdentifier(AssetType.ClassName, (int)person.Class));
                var classAndLevelString = textLoader.Concat(className, $" {person.Level}");
                personInfoTexts[(int)InfoText.ClassAndLevel] = game.TextManager.Create(classAndLevelString, maxTextWidth, 15, paperColor, palette);
            }
            else
            {
                personInfoTexts[(int)InfoText.ClassAndLevel] = null; // hidden
            }
        }
        else
        {
            personInfoTexts[(int)InfoText.Age] = null; // hidden
            personInfoTexts[(int)InfoText.ClassAndLevel] = null; // hidden
        }

        if (person is IPartyMember partyMember)
        {
            // EP
            var epString = textLoader.LoadText(new AssetIdentifier(AssetType.UIText, (int)UIText.EP)).GetString();
            epString = Game.InsertNumberIntoString(epString, ":", true, partyMember.ExperiencePoints, int.MaxValue);
            personInfoTexts[(int)InfoText.EP] = game.TextManager.Create(epString, 15, paperColor, palette);

            // LP
            var lpString = textLoader.LoadText(new AssetIdentifier(AssetType.UIText, (int)UIText.LP)).GetString();
            lpString = Game.InsertNumberIntoString(lpString, "/", false, partyMember.HitPoints.CurrentValue, 3, '0');
            lpString = Game.InsertNumberIntoString(lpString, "/", true, partyMember.HitPoints.TotalMax, 3, '0');
            personInfoTexts[(int)InfoText.LP] = game.TextManager.Create(lpString, 15, paperColor, palette);

            if (partyMember.LearnedSpellSchools != SpellSchoolFlags.None)
            {
                // SP
                var spString = textLoader.LoadText(new AssetIdentifier(AssetType.UIText, (int)UIText.SP)).GetString();
                spString = Game.InsertNumberIntoString(spString, "/", false, partyMember.SpellPoints.CurrentValue, 3, '0');
                spString = Game.InsertNumberIntoString(spString, "/", true, partyMember.SpellPoints.TotalMax, 3, '0');
                personInfoTexts[(int)InfoText.SP] = game.TextManager.Create(spString, 15, paperColor, palette);

                // SLP
                var slpString = textLoader.LoadText(new AssetIdentifier(AssetType.UIText, (int)UIText.SLP)).GetString();
                slpString = Game.InsertNumberIntoString(slpString, "  ", false, partyMember.SpellLearningPoints, 3, '0');
                personInfoTexts[(int)InfoText.SLP] = game.TextManager.Create(slpString, 15, paperColor, palette);
            }
            else
            {
                personInfoTexts[(int)InfoText.SP] = null;
                personInfoTexts[(int)InfoText.SLP] = null;
            }

            // Gold and Food
            var goldFoodString = textLoader.LoadText(new AssetIdentifier(AssetType.UIText, (int)UIText.GoldFood)).GetString();
            goldFoodString = Game.FormatValueString(goldFoodString, partyMember.Gold, partyMember.Food);
            personInfoTexts[(int)InfoText.GoldAndFood] = game.TextManager.Create(goldFoodString, 15, paperColor, palette);

            // Damage and proection
            var placeholderString = textLoader.LoadText(new AssetIdentifier(AssetType.UIText, (int)UIText.Colon)).GetString();
            var damageString = Game.InsertNumberIntoString(placeholderString, ":", true, partyMember.Damage + partyMember.BonusDamage, 3, '0');
            var protectionString = Game.InsertNumberIntoString(placeholderString, ":", true, partyMember.Defense + partyMember.BonusDefense, 3, '0');
            personInfoTexts[(int)InfoText.Damage] = game.TextManager.Create(damageString, 15, paperColor, palette);
            personInfoTexts[(int)InfoText.Protection] = game.TextManager.Create(protectionString, 15, paperColor, palette);
            swordIcon = game.CreateSprite(Layer.UI, new Position(208, 120), new Size(16, 10), (int)UIGraphic.Sword, palette);
            shieldIcon = game.CreateSprite(Layer.UI, new Position(256, 120), new Size(16, 10), (int)UIGraphic.Shield, palette);
            swordIcon!.Opaque = true;
            shieldIcon!.Opaque = true;
        }

        int lineHeight = personInfoTexts[(int)InfoText.Gender]!.LineHeight; // personInfoTexts[1] (gender) is always given, the rest is optional

        for (int i = 0; i < personInfoTexts.Length; i++)
        {
            var text = personInfoTexts[i];
            text?.Show(textPositionX, textPositionY, 2);
            textPositionY += lineHeight;

            if (i == (int)InfoText.EP)
            {
                textPositionX = portraitPosition.X + 12;
                textPositionY += lineHeight + 1; // extra space between EP and next text (LP)
            }
            else if (i == (int)InfoText.SLP)
            {
                textPositionX -= 6;
            }
            else if (i == (int)InfoText.GoldAndFood)
            {
                if (swordIcon != null)
                    textPositionX = swordIcon.Position.X + swordIcon.Size.Width + 2;

                textPositionY += 2;
            }
            else if (i == (int)InfoText.Damage)
            {
                if (shieldIcon != null)
                    textPositionX = shieldIcon.Position.X + shieldIcon.Size.Width + 2;

                textPositionY -= lineHeight;
            }
        }
    }

    public bool Visible
    {
        get => visible;
        set
        {
            if (visible == value)
                return;

            visible = value;

            portraitBackground.Visible = value;
            portrait.Visible = value;
            name.Visible = value;

            for (int i = 0; i < personInfoTexts.Length; i++)
            {
                var text = personInfoTexts[i];

                if (text != null)
                {
                    if (i == (int)InfoText.GoldAndFood || i == (int)InfoText.Damage || i == (int)InfoText.Protection)
                        text.Visible = value && extended;
                    else
                        text.Visible = value;
                }
            }

            if (swordIcon != null)
                swordIcon.Visible = value && extended;
            if (shieldIcon != null)
                shieldIcon.Visible = value && extended;
        }
    }

    public bool Extended
    {
        get => extended;
        set
        {
            if (extended == value)
                return;

            extended = value;

            void SetExtendedTextVisibility(int index)
            {
                var text = personInfoTexts[index];

                if (text != null)
                    text.Visible = visible && extended;
            }

            SetExtendedTextVisibility((int)InfoText.GoldAndFood);
            SetExtendedTextVisibility((int)InfoText.Damage);
            SetExtendedTextVisibility((int)InfoText.Protection);

            if (swordIcon != null)
                swordIcon.Visible = visible && extended;
            if (shieldIcon != null)
                shieldIcon.Visible = visible && extended;
        }
    }

    public void Destroy()
    {
        portraitBackground.Visible = false;
        portrait.Visible = false;
        name.Delete();

        for (int i = 0; i < personInfoTexts.Length; i++)
        {
            personInfoTexts[i]?.Delete();
            personInfoTexts[i] = null;
        }

        if (swordIcon != null)
            swordIcon.Visible = false;

        if (shieldIcon != null)
            shieldIcon.Visible = false;
    }
}
