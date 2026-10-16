using Amber.Audio;
using Amber.Common;
using Amber.IO.FileSystem;
using Amber.Renderer.Common;
using Amberstar.Game;
using Amberstar.GameData.Legacy;

namespace Amberstar.Frontend;

/// <summary>
/// Creates the game for a renderer and some game data.
/// It is shared by the window frontend, the headless runner and the tests.
/// </summary>
public static class GameSetup
{
	/// <summary>
	/// Environment variable which can provide the path to the game data.
	/// </summary>
	public const string DataPathEnvironmentVariable = "AMBERSTAR_DATA";

	/// <summary>
	/// Data folders of the repository, used if no path is given.
	/// </summary>
	public static readonly string[] RepositoryDataFolders =
	[
		Path.Combine("German", "AmberfilesST"),
		Path.Combine("English", "AmberfilesST"),
	];

	/// <summary>
	/// Finds the game data. The order is: the given path, the environment variable
	/// AMBERSTAR_DATA and then the data folders of the repository (searched upwards
	/// from the application folder). Returns null if nothing was found.
	///
	/// The given path can also be one of the repository data folders like "English/AmberfilesST".
	/// </summary>
	public static string? FindDataPath(string? path = null)
	{
		if (!string.IsNullOrWhiteSpace(path))
		{
			if (Directory.Exists(path))
				return Path.GetFullPath(path);

			return FindRepositoryFolder(path);
		}

		var environmentPath = Environment.GetEnvironmentVariable(DataPathEnvironmentVariable);

		if (!string.IsNullOrWhiteSpace(environmentPath) && Directory.Exists(environmentPath))
			return Path.GetFullPath(environmentPath);

		foreach (var folder in RepositoryDataFolders)
		{
			var repositoryPath = FindRepositoryFolder(folder);

			if (repositoryPath != null)
				return repositoryPath;
		}

		return null;
	}

	static string? FindRepositoryFolder(string relativePath)
	{
		var directory = new DirectoryInfo(AppContext.BaseDirectory);

		while (directory != null)
		{
			var path = Path.Combine(directory.FullName, relativePath);

			if (Directory.Exists(path))
				return path;

			directory = directory.Parent;
		}

		return null;
	}

	public static Game.Game CreateGame(IRenderer renderer, string dataPath, IAudioOutput audioOutput,
		IConfiguration configuration, Func<List<Key>> pressedKeyProvider, Action<Position> setMousePosition)
	{
		var fileSystem = FileSystem.FromOperatingSystemPath(dataPath);
		var assetProvider = new AssetProvider(fileSystem.AsReadOnly());

		LayerSetup.Run(assetProvider, renderer, out var uiGraphicIndexProvider,
			out var paletteIndexProvider, out var paletteColorProvider, out var fontInfoProvider);

		return new Game.Game(renderer, assetProvider, audioOutput, uiGraphicIndexProvider,
			paletteIndexProvider, paletteColorProvider, fontInfoProvider, configuration,
			pressedKeyProvider, setMousePosition);
	}
}
