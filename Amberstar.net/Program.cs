using Amberstar.Frontend;
using Amberstar.Frontend.Scripting;

namespace Amberstar;

class Program
{
	// Only wait for the user after errors if nobody runs a script (e.g. CI).
	static bool interactive = true;

	[STAThread]
	static void Main(string[] args)
	{
		AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

		var options = CommandLineOptions.Parse(args);

		if (options == null)
			Environment.Exit(1);

		if (options.ShowHelp)
		{
			CommandLineOptions.PrintUsage();
			return;
		}

		interactive = options.ScriptPath == null;
		int exitCode = 0;

		try
		{
			if (options.Headless)
			{
				exitCode = RunHeadless(options);
			}
			else
			{
				var gameWindow = new GameWindow(options);
				gameWindow.Run();
				exitCode = gameWindow.ExitCode;
			}
		}
		catch (Exception ex)
		{
			PrintException(ex);
			exitCode = 1;
		}
		finally
		{
			Environment.Exit(exitCode);
		}
	}

	static int RunHeadless(CommandLineOptions options)
	{
		var script = ScenarioScript.FromFile(options.ScriptPath!);
		// command line options win over the script directives
		var dataPath = GameSetup.FindDataPath(options.DataPath ?? script.DataPath);

		if (dataPath == null)
		{
			Console.WriteLine("Game data not found. Use '--data <path>' or the environment variable " + GameSetup.DataPathEnvironmentVariable + ".");
			return 1;
		}

		using var game = new HeadlessGame(dataPath, options.RandomSeed ?? script.RandomSeed ?? 0);
		var result = game.RunScript(script, options.MaxScriptTime ?? script.MaxGameSeconds ?? 600.0);

		return result.Success ? 0 : 1;
	}

	static void OutputError(string error)
	{
		Console.WriteLine(error);

		if (interactive)
		{
			Console.WriteLine();
			Console.WriteLine("Press return to exit");
			Console.ReadLine();
		}
	}

	static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		if (e.ExceptionObject is Exception ex)
			PrintException(ex);
		else
			OutputError(e.ExceptionObject?.ToString() ?? "Unhandled exception without exception object");

		Environment.Exit(1);
	}

	static void PrintException(Exception ex)
	{
		string message = ex.Message;
		string? stackTrace = ex.StackTrace;
		var e = ex.InnerException;

		while (e != null)
		{
			message += Environment.NewLine + e.Message;
			e = e.InnerException;
		}

		OutputError(message + Environment.NewLine + stackTrace ?? "");
	}
}
