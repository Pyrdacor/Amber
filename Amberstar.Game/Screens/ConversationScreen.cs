using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

internal class ConversationScreen : ButtonGridScreen
{
	Game? game;
    IPerson? person;
    PersonInfoView? personInfoView;
    IRenderText? title;
    // TODO: paper = 2, ink = 15

    public override ScreenType Type { get; } = ScreenType.Conversation;

    internal override byte ButtonGridPaletteIndex => game?.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI] ?? 0;

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

    public override void Open(Game game, Action? closeAction)
	{
		base.Open(game, closeAction);

		this.game = game;

        var palette = game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI];

        game.SetLayout(Layout.Conversation, palette);
        game.Cursor.CursorType = CursorType.Sword;

        if (game.State.CurrentConversationCharacterIndex is not int personIndex)
            throw new AmberException(ExceptionScope.Application, "No conversation character specified.");

        person = game!.AssetProvider.PersonLoader.LoadPerson(personIndex);
        personInfoView = new(game, person, personIndex, palette);

        title = game.TextManager.Create("Gespräch", 1, TextManager.TransparentPaper, palette); // TODO
    }

    public override void Close(Game game)
    {
        personInfoView?.Destroy();
        title?.Delete();

        base.Close(game);
    }

    public override void ScreenPushed(Game game, Screen screen)
    {
        if (!screen.Transparent)
        {
            if (personInfoView != null)
                personInfoView.Visible = false;
            if (title != null)
                title.Visible = false;
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
            if (title != null)
                title.Visible = true;
        }
    }

    public override void KeyDown(Key key, KeyModifiers keyModifiers)
    {
        base.KeyDown(key, keyModifiers);

        if (key == Key.Escape)
            game!.ScreenHandler.PopScreen();
    }

    public override void MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
    {
        base.MouseDown(position, buttons, keyModifiers);
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
                game?.ScreenHandler.PopScreen();
                break;
            case 3: // Show item
                // TODO
                break;
            case 4: // Speak
                // TODO
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
