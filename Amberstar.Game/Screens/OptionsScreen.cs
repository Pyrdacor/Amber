using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

/// <summary>
/// The options menu (Options_menu in EYE.ASM of the original): quit the game,
/// switch the music on or off and save or load the game.
/// </summary>
internal sealed class OptionsScreen : ButtonGridScreen
{
	const int QuitButtonIndex = 0;
	const int ExitButtonIndex = 2;
	const int MusicOnButtonIndex = 3;
	const int MusicOffButtonIndex = 4;
	const int SaveButtonIndex = 6;
	const int LoadButtonIndex = 7;

	public override ScreenType Type { get; } = ScreenType.Options;

	internal override byte ButtonGridPaletteIndex => Game?.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI] ?? 0;

	public override void Open(Action? closeAction)
	{
		base.Open(closeAction);

		Game.SetLayout(Layout.Save, ButtonGridPaletteIndex);
		Game.Cursor.CursorType = CursorType.Sword;
	}

	public override void ScreenPopped(Screen screen)
	{
		base.ScreenPopped(screen);

		RequestButtonSetup();
	}

	protected override void SetupButtons(ButtonGrid buttonGrid)
	{
		for (int i = 0; i < 9; i++)
			buttonGrid.SetButton(i, ButtonType.Empty);

		buttonGrid.SetButton(QuitButtonIndex, ButtonType.Quit);
		buttonGrid.SetButton(ExitButtonIndex, ButtonType.Exit);
		buttonGrid.SetButton(MusicOnButtonIndex, ButtonType.Music);
		buttonGrid.SetButton(MusicOffButtonIndex, ButtonType.NoMusic);
		buttonGrid.SetButton(SaveButtonIndex, ButtonType.Save);
		buttonGrid.SetButton(LoadButtonIndex, ButtonType.Load);

		buttonGrid.EnableButton(MusicOnButtonIndex, Game.State.MusicBlock);
		buttonGrid.EnableButton(MusicOffButtonIndex, !Game.State.MusicBlock);
		buttonGrid.EnableButton(SaveButtonIndex, Game.SavegamePath != null);
		buttonGrid.EnableButton(LoadButtonIndex, Game.HasSavegame);
	}

	protected override void ButtonClicked(int index)
	{
		switch (index)
		{
			case QuitButtonIndex:
				Game.AskForConfirmation(Game.LoadMessageText(Message.ReallyQuitProgram), () => Game.Quit());
				break;
			case ExitButtonIndex:
				Game.ScreenHandler.PopScreen();
				break;
			case MusicOnButtonIndex:
			case MusicOffButtonIndex:
				Game.SetMusic(index == MusicOnButtonIndex);
				RequestButtonSetup();
				break;
			case SaveButtonIndex:
				// TODO: The original lets the player dismiss party members before saving.
				Game.AskForConfirmation(Game.LoadMessageText(Message.SaveGame), () =>
				{
					Game.SaveGame();
					RequestButtonSetup();
				});
				break;
			case LoadButtonIndex:
				Game.AskForConfirmation(Game.LoadMessageText(Message.LoadGame), Game.LoadGameAndContinue);
				break;
		}
	}
}
