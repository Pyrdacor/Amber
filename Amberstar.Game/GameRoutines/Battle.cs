namespace Amberstar.Game;

partial class Game
{
	/// <summary>
	/// Starts a battle against a monster group (MON_DATA.AMB).
	/// </summary>
	internal void StartBattle(int monsterGroupIndex)
	{
		// TODO: Battles are not implemented yet. The released original source code only has a stub as well.
		ShowNotImplementedMessage($"Battles (monster group {monsterGroupIndex})");
	}

	internal void ShowNotImplementedMessage(string feature, Action? nextAction = null) => ShowTextMessage($"{feature} is not implemented yet.", nextAction);
}
