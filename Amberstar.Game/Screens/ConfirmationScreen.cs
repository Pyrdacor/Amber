using Amberstar.Game.UI;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

internal class ConfirmationScreen() : WindowScreen(WindowX, WindowY, WindowWidthInTiles, WindowHeightInTiles, WindowDisplayLayer)
{
	const int WindowX = 48;
	const int WindowY = 60;
	const int WindowWidthInTiles = 12;
	const int WindowHeightInTiles = 5;
    const byte WindowDisplayLayer = 150;
    const byte ControlDisplayLayer = 175;
    Label? text = null;

    public override ScreenType Type { get; } = ScreenType.Confirmation;

    public override void Init()
    {
        base.Init();

        text = AddLabel(x: 0, y: 0, "", ClientArea.Size.Width, 32, ControlDisplayLayer);
        text.Visible = false;

        var yesButton = AddButton(x: 16, y: 32, ButtonType.ThumbsUp, ControlDisplayLayer);
        var noButton = AddButton(x: 16 + Button.Width, y: 32, ButtonType.ThumbsDown, ControlDisplayLayer);

        yesButton.ClickAction += Yes;
        noButton.ClickAction += No;
    }

    public override void Open(Action? closeAction)
	{
		if (Game.CurrentText == null)
			throw new InvalidOperationException($"{nameof(ConfirmationScreen)} needs {nameof(Game.CurrentText)} to be set beforehand.");

		base.Open(closeAction);

        text!.SetText(Game.CurrentText);
        text.Visible = true;

        Game.CurrentResult = false;
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
