using Amber.IO.FileSystem;
using Amberstar.Frontend;
using Amberstar.GameData.Legacy;

// The game uses some static state (random numbers, text blinking), so games must not run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Amberstar.Tests;

/// <summary>
/// Access to the game data and test scripts of the repository.
/// </summary>
internal static class TestData
{
	public static TheoryData<string> DataFolders
	{
		get
		{
			var data = new TheoryData<string>();

			foreach (var folder in GameSetup.RepositoryDataFolders)
				data.Add(folder);

			return data;
		}
	}

	public static string GetDataPath(string folder) => GameSetup.FindDataPath(folder)
		?? throw new DirectoryNotFoundException($"Game data folder '{folder}' was not found above '{AppContext.BaseDirectory}'.");

	public static AssetProvider CreateAssetProvider(string folder) =>
		new(FileSystem.FromOperatingSystemPath(GetDataPath(folder)).AsReadOnly());

	public static string ScenarioFolder => FindRepositoryFolder(Path.Combine("Tests", "Scenarios"));

	static string FindRepositoryFolder(string relativePath)
	{
		var directory = new DirectoryInfo(AppContext.BaseDirectory);

		while (directory != null)
		{
			var path = Path.Combine(directory.FullName, relativePath);

			if (Directory.Exists(path))
				return path;

			directory = directory.Parent;
		}

		throw new DirectoryNotFoundException($"Folder '{relativePath}' was not found above '{AppContext.BaseDirectory}'.");
	}
}
