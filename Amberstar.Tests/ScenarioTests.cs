using Amberstar.Frontend;
using Amberstar.Frontend.Scripting;
using Xunit.Abstractions;

namespace Amberstar.Tests;

/// <summary>
/// Runs every scenario script in Tests/Scenarios headless.
/// </summary>
public sealed class ScenarioTests(ITestOutputHelper output)
{
	public static TheoryData<string> Scripts
	{
		get
		{
			var data = new TheoryData<string>();

			foreach (var path in Directory.GetFiles(TestData.ScenarioFolder, "*.txt").Order())
				data.Add(Path.GetFileNameWithoutExtension(path));

			return data;
		}
	}

	[Theory]
	[MemberData(nameof(Scripts))]
	public void Scenario(string name)
	{
		var script = ScenarioScript.FromFile(Path.Combine(TestData.ScenarioFolder, name + ".txt"), output.WriteLine);
		var dataPath = TestData.GetDataPath(script.DataPath ?? GameSetup.RepositoryDataFolders[0]);

		using var game = new HeadlessGame(dataPath, script.RandomSeed ?? 0);
		var result = game.RunScript(script, script.MaxGameSeconds ?? 600.0, output.WriteLine);

		Assert.False(result.TimedOut, "The script timed out.");
		Assert.True(result.Failures == 0, $"{result.Failures} of {result.Checks} checks failed. See the test output.");
	}
}
