using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

/// <summary>
/// Shows the view direction of the party (MagicCompass_Mod in the original).
/// Any input closes it.
/// </summary>
internal sealed class MagicCompassScreen() : WindowScreen(WindowX, WindowY, WindowWidthInTiles, WindowHeightInTiles, WindowDisplayLayer)
{
	const int WindowX = 48;
	const int WindowY = 64;
	const int WindowWidthInTiles = 4;
	const int WindowHeightInTiles = 4;
	const byte WindowDisplayLayer = 150;
	const byte ImageDisplayLayer = 175;

	public override ScreenType Type { get; } = ScreenType.MagicCompass;

	public override bool CloseOnNextInput { get; } = true;

	public override void Open(Action? closeAction)
	{
		base.Open(closeAction);

		var direction = (int)Game.State.PartyDirection % 4;
		var graphic = (UIGraphic)((int)UIGraphic.CompassNorth + direction);
		AddImage(0, 0, 32, 32, Game.GraphicIndexProvider.GetUIGraphicIndex(graphic), ImageDisplayLayer);
	}
}
