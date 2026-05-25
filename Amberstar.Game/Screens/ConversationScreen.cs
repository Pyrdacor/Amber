using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

// TODO: Rework and implement fully
internal sealed class ConversationScreen : ButtonGridScreen
{
    const int ThatKeywordIndex = 193; // "THAT" is used if the entered word is no valid keyword

    IPerson? person;
    PersonInfoView? personInfoView;

    public sealed override ScreenType Type { get; } = ScreenType.Conversation;

    internal sealed override byte ButtonGridPaletteIndex => Game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI];

    protected override void SetupButtons(ButtonGrid buttonGrid)
    {
        // Upper row
        buttonGrid.SetButton(0, ButtonType.GiveItem);
        buttonGrid.SetButton(1, ButtonType.DropItem);
        buttonGrid.SetButton(2, ButtonType.Exit);
        // Middle row
        buttonGrid.SetButton(3, ButtonType.ExamineItem);
        buttonGrid.SetButton(4, ButtonType.Mouth);
        buttonGrid.SetButton(5, ButtonType.AskToJoin);
        // Lower row
        buttonGrid.SetButton(6, ButtonType.GiveItemToPerson);
        buttonGrid.SetButton(7, ButtonType.GiveGoldToPerson);
        buttonGrid.SetButton(8, ButtonType.GiveFoodToPerson);

        // Enable "Ask to join" button only for party members
        buttonGrid.EnableButton(5, person is IPartyMember);
    }

    public override void Init()
    {
        base.Init();

        var dialogLabel = AddLabel(16, 39, Game.LoadUIText(UIText.Dialog), 176, 7);
        dialogLabel.Alignment = TextAlignment.Center;
    }

    public override void Open(Action? closeAction)
	{
		base.Open(closeAction);

        var palette = Game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI];

        Game.SetLayout(Layout.Conversation, palette);
        Game.Cursor.CursorType = CursorType.Sword;

        if (Game.State.CurrentConversationCharacterIndex is not int personIndex)
            throw new AmberException(ExceptionScope.Application, "No conversation character specified.");

        person = Game.AssetProvider.PersonLoader.LoadPerson(personIndex);
        personInfoView = new(Game, person, personIndex, palette);
    }

    public override void Close()
    {
        personInfoView?.Destroy();

        base.Close();
    }

    public override void ScreenPushed(Screen screen)
    {
        if (!screen.Transparent)
        {
            if (personInfoView != null)
                personInfoView.Visible = false;
        }

        base.ScreenPushed(screen);        
    }

    public override void ScreenPopped(Screen screen)
    {
        base.ScreenPopped(screen);

        if (!screen.Transparent)
        {
            if (personInfoView != null)
                personInfoView.Visible = true;
        }
    }

    protected override void ButtonClicked(int index)
    {
        switch (index)
        {
            case 0: // Give item to party
                // TODO: switch to pickup item mode and display some message.
                break;
            case 1: // Drop item
                // TODO
                break;
            case 2:
                Game.ScreenHandler.PopScreen();
                break;
            case 3: // Show item
                // TODO
                break;
            case 4: // Speak
                Game.ScreenHandler.PushScreen(ScreenType.SelectWord);
                break;
            case 5: // Ask to join
                // TODO
                break;
            case 6: // Give item to person
                // TODO
                break;
            case 7: // Give gold to person
                // TODO
                break;
            case 8: // Give food to person
                // TODO
                break;
        }
    }
}
