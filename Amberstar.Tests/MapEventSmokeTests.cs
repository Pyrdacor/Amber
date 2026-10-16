using Amberstar.Frontend;
using Amberstar.Frontend.Scripting;
using Amberstar.GameData;
using Xunit.Abstractions;

namespace Amberstar.Tests;

/// <summary>
/// Enters every placed map event of the game once and closes everything which
/// opens afterwards. This finds crashes in events, screens and map changes.
/// </summary>
public sealed class MapEventSmokeTests(ITestOutputHelper output)
{
	static IEnumerable<string> CreateScript(string folder)
	{
		var assetProvider = TestData.CreateAssetProvider(folder);

		yield return "waittext";
		yield return "dismissall";

		foreach (var (mapIndex, map) in assetProvider.MapLoader.LoadAllMaps().OrderBy(map => map.Key))
		{
			var tileEvents = map switch
			{
				IMap2D map2D => map2D.Tiles.Select(tile => (int)tile.Event).ToArray(),
				IMap3D map3D => map3D.Tiles.Select(tile => (int)tile.Event).ToArray(),
				_ => []
			};

			for (int eventIndex = 1; eventIndex <= map.Events.Count; eventIndex++)
			{
				int tileIndex = Array.IndexOf(tileEvents, eventIndex);

				if (tileIndex == -1)
					continue;

				yield return $"log event {eventIndex} ({map.Events[eventIndex - 1].Type}) of map {mapIndex}";
				yield return $"teleport {mapIndex} {1 + tileIndex % map.Width} {1 + tileIndex / map.Width}";
				yield return "closeall 30000";
			}
		}
	}

	[Theory]
	[MemberData(nameof(TestData.DataFolders), MemberType = typeof(TestData))]
	public void AllMapEventsCanBeEntered(string folder)
	{
		// only the failures are logged as the output would be huge
		var script = new ScenarioScript(CreateScript(folder), line =>
		{
			if (line.Contains("FAIL") || line.StartsWith("    at "))
				output.WriteLine(line);
		});

		using var game = new HeadlessGame(TestData.GetDataPath(folder));
		var result = game.RunScript(script, 24 * 60 * 60, output.WriteLine);

		Assert.True(result.Success, $"{result.Failures} of {result.Checks} checks failed. See the test output.");
	}
}
