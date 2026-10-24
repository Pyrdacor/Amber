using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

/// <summary>
/// A text with yes and no buttons. The combat requester uses a large window like the text boxes
/// with the sword and flee buttons at the left (CombatReq_DisInit in PERSONS.ASM).
/// </summary>
internal class ConfirmationScreen() : WindowScreen(WindowX, WindowY, WindowWidthInTiles, WindowHeightInTiles, WindowDisplayLayer)
{
	const int WindowX = 48;
	const int WindowY = 60;
	const int WindowWidthInTiles = 12;
	const int WindowHeightInTiles = 5;
	const int LargeWindowX = 16;
	const int LargeWindowY = 52;
	const int LargeWindowWidthInTiles = 18;
    const byte WindowDisplayLayer = 150;
    const byte ControlDisplayLayer = 175;
    Label? text = null;
    bool large = false;

    public override ScreenType Type { get; } = ScreenType.Confirmation;

    public override bool CreateWindowInOpenHandler { get; } = true;

    public override int X => large ? LargeWindowX : WindowX;

    public override int Y => large ? LargeWindowY : WindowY;

    public override int WidthInTiles => large ? LargeWindowWidthInTiles : WindowWidthInTiles;

    public override void Open(Action? closeAction)
	{
		if (Game.CurrentText == null)
			throw new InvalidOperationException($"{nameof(ConfirmationScreen)} needs {nameof(Game.CurrentText)} to be set beforehand.");

        large = Game.CurrentConfirmationLarge;
        Game.CurrentConfirmationLarge = false;

		base.Open(closeAction);

        text = AddLabel(x: 0, y: 0, "", ClientArea.Size.Width, 32, ControlDisplayLayer);

        // The combat requester shows the sword (fight) and flee buttons and the sword cursor.
        int buttonX = large ? 0 : 16;
        var yesButton = AddButton(x: buttonX, y: 32, large ? ButtonType.Sword : ButtonType.ThumbsUp, ControlDisplayLayer);
        var noButton = AddButton(x: buttonX + Button.Width, y: 32, large ? ButtonType.Flee : ButtonType.ThumbsDown, ControlDisplayLayer);

        if (large)
            Game.Cursor.CursorType = CursorType.Sword;

        yesButton.ClickAction += Yes;
        noButton.ClickAction += No;

        Game.ScriptTextShown(Game.CurrentText);
        text!.SetText(Game.CurrentText);
        text.Visible = true;

        Game.CurrentResult = false;
    }

    internal void ScriptAnswer(bool yes)
    {
        if (yes)
            Yes();
        else
            No();
    }

	private void Yes()
	{
        Game.CurrentResult = true;
        Game.ScreenHandler.PopScreen();
    }

    private void No()
    {
        Game.CurrentResult = false;
        Game.ScreenHandler.PopScreen();
    }
}
