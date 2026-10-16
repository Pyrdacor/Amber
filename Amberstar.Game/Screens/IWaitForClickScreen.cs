namespace Amberstar.Game.Screens;

/// <summary>
/// Screens which show a message and wait for a click (e.g. door, chest, conversation).
/// </summary>
internal interface IWaitForClickScreen
{
	bool WaitingForClick { get; }
}
