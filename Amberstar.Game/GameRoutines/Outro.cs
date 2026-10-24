using Amberstar.Game.Screens;

namespace Amberstar.Game;

partial class Game
{
	/// <summary>
	/// The outro is shown when the party reaches the ritual room of Godsbane (the outro event at $a262 in the
	/// program which starts EXTRO.68K). Afterwards the game starts again.
	/// </summary>
	internal void StartOutro()
	{
		if (AssetProvider.OutroData == null)
		{
			ShowNotImplementedMessage("The outro without EXTRO.68K");
			return;
		}

		ScreenHandler.PushScreen(ScreenType.Outro);
	}
}
