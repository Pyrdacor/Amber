using Amber.Common;
using Amberstar.Frontend;
using Amberstar.Frontend.Scripting;
using Amberstar.GameData;
using G = Amberstar.Game.Game;

namespace Amberstar.Tests;

/// <summary>
/// Runs the game headless and checks basic game logic.
/// </summary>
public sealed class HeadlessGameTests
{
	static HeadlessGame CreateGame() => new(TestData.GetDataPath(GameSetup.RepositoryDataFolders[0]));

	/// <summary>
	/// Starts the game and closes the intro text of the graveyard.
	/// </summary>
	static HeadlessGame StartGame()
	{
		var game = CreateGame();
		var result = game.RunScript(new ScenarioScript(["waittext", "dismissall"], _ => { }), output: _ => { });

		Assert.True(result.Success, game.Game.ScriptState());

		return game;
	}

	[Fact]
	public void GameStartsWithTheGraveyardText()
	{
		using var game = CreateGame();

		Assert.True(game.StepUntil(() => game.Game.ScriptActiveScreen == "PictureText"), game.Game.ScriptState());
		Assert.Equal(65, game.Game.ScriptMapIndex);
		Assert.Single(game.Game.ScriptShownTexts);
	}

	[Fact]
	public void IntroShowsTheCreditsAndStartsTheGame()
	{
		using var game = new HeadlessGame(TestData.GetDataPath(GameSetup.RepositoryDataFolders[0]), showIntro: true);

		// the title picture stays until the space key is pressed
		Assert.True(game.StepUntil(() => game.Game.ScriptActiveScreen == "Intro"), game.Game.ScriptState());
		game.Step(120);
		Assert.Empty(game.Game.ScriptShownTexts);
		game.Game.KeyDown(Amberstar.Game.Key.Space, Amberstar.Game.KeyModifiers.None);
		// the credits after the fading and the logo
		Assert.True(game.StepUntil(() => game.Game.ScriptShownTexts.Count == 1, 300), game.Game.ScriptState());
		Assert.Contains("Karsten", game.Game.ScriptShownTexts[0]);
		Assert.True(game.StepUntil(() => game.Game.ScriptShownTexts.Count == 2, 400), game.Game.ScriptState());
		// the space key ends the intro and the game starts with the graveyard text
		game.Game.KeyDown(Amberstar.Game.Key.Space, Amberstar.Game.KeyModifiers.None);
		Assert.True(game.StepUntil(() => game.Game.ScriptActiveScreen == "PictureText", 600), game.Game.ScriptState());
		Assert.Equal(65, game.Game.ScriptMapIndex);
	}

	[Fact]
	public void TeleportToAnotherMapFadesInGameTime()
	{
		using var game = StartGame();

		Assert.Null(game.Game.ScriptTeleport(67, 7, 15));
		game.Step();
		Assert.True(game.Game.ScriptFading);

		// the fading takes 1 second of game time
		game.Step(90);

		Assert.False(game.Game.ScriptFading);
		Assert.True(game.Game.ScriptIdle, game.Game.ScriptState());
		Assert.Equal(67, game.Game.ScriptMapIndex);
		Assert.Equal(new Position(7, 15), game.Game.ScriptPartyPosition);
	}

	[Fact]
	public void EventBitsUseTheSaveIndexOfTheEvent()
	{
		using var game = StartGame();

		// Event 2 of the graveyard is the intro text with save index 1.
		Assert.True(game.Game.ScriptGetEventBit(65, 1));
		Assert.False(game.Game.ScriptGetEventBit(65, 2));
		Assert.False(game.Game.ScriptIsEventActive(65, 2));
		// Event 6 uses save index 2 and is not triggered yet.
		Assert.True(game.Game.ScriptIsEventActive(65, 6));
		// Events without save index are always active.
		Assert.True(game.Game.ScriptIsEventActive(65, 1));
	}

	[Fact]
	public void RandomMaximumIsInclusive()
	{
		G.SetRandomSeed(1);

		var values = Enumerable.Range(0, 200).Select(_ => G.Random(1, 3)).ToHashSet();

		Assert.Equal([1, 2, 3], values.Order());
	}

	[Fact]
	public void SameSeedGivesSameRandomNumbers()
	{
		G.SetRandomSeed(42);
		var first = Enumerable.Range(0, 20).Select(_ => G.Random(1, 100)).ToArray();
		G.SetRandomSeed(42);
		var second = Enumerable.Range(0, 20).Select(_ => G.Random(1, 100)).ToArray();

		Assert.Equal(first, second);
	}

	[Fact]
	public void MovementIsBlockedByWalls()
	{
		using var game = StartGame();

		var start = game.Game.ScriptPartyPosition;

		// The graveyard start position has a wall below and free space to the right.
		Assert.NotNull(game.Game.ScriptMove(0, 1));
		Assert.Equal(start, game.Game.ScriptPartyPosition);
		Assert.Null(game.Game.ScriptMove(1, 0));
		Assert.Equal(start + new Position(1, 0), game.Game.ScriptPartyPosition);
		Assert.Equal(Direction.Right, game.Game.ScriptPartyDirection);
	}

	[Fact]
	public void ItemsCanBeGiven()
	{
		using var game = StartGame();

		// item 1 is used by some character or chest
		int before = game.Game.ScriptCountItem(1);
		Assert.Null(game.Game.ScriptGiveItem(1, 1, 1));
		Assert.Equal(before + 1, game.Game.ScriptCountItem(1));
	}
}
