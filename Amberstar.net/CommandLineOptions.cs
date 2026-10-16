namespace Amberstar;

/// <summary>
/// Options for the current run. They are never stored in the configuration.
/// </summary>
internal sealed class CommandLineOptions
{
	public string? DataPath { get; private set; }
	public string? ScriptPath { get; private set; }
	public bool Headless { get; private set; }
	public int? RandomSeed { get; private set; }
	public double MaxScriptTime { get; private set; } = 600.0;
	public bool ShowHelp { get; private set; }

	/// <summary>
	/// Parses the arguments. Returns null and prints an error if they are invalid.
	/// </summary>
	public static CommandLineOptions? Parse(string[] args)
	{
		var options = new CommandLineOptions();

		for (int i = 0; i < args.Length; i++)
		{
			string NextValue()
			{
				if (i + 1 >= args.Length)
					throw new FormatException($"Missing value for option '{args[i]}'.");

				return args[++i];
			}

			try
			{
				switch (args[i].ToLowerInvariant())
				{
					case "--help":
					case "-h":
					case "/?":
						options.ShowHelp = true;
						break;
					case "--data":
						options.DataPath = NextValue();
						break;
					case "--script":
						options.ScriptPath = NextValue();
						break;
					case "--headless":
						options.Headless = true;
						break;
					case "--seed":
						options.RandomSeed = int.Parse(NextValue());
						break;
					case "--max-time":
						options.MaxScriptTime = double.Parse(NextValue(), System.Globalization.CultureInfo.InvariantCulture);
						break;
					default:
						throw new FormatException($"Unknown option '{args[i]}'.");
				}
			}
			catch (FormatException ex)
			{
				Console.WriteLine(ex.Message);
				Console.WriteLine();
				PrintUsage();
				return null;
			}
		}

		if (options.Headless && options.ScriptPath == null)
		{
			Console.WriteLine("Option '--headless' needs a script given by '--script'.");
			Console.WriteLine();
			PrintUsage();
			return null;
		}

		return options;
	}

	public static void PrintUsage()
	{
		Console.WriteLine("Usage: Amberstar.net [options]");
		Console.WriteLine();
		Console.WriteLine("  --data <path>      Path to the game data. Default: environment variable AMBERSTAR_DATA");
		Console.WriteLine("                     or the data folders of the repository (German/AmberfilesST first).");
		Console.WriteLine("  --script <file>    Run a scenario script (see Tests/Scenarios/README.md).");
		Console.WriteLine("  --headless         Run the script without a window as fast as possible.");
		Console.WriteLine("  --seed <number>    Seed for random numbers (headless default: 0).");
		Console.WriteLine("  --max-time <s>     Maximum game time in seconds for a headless script (default: 600).");
		Console.WriteLine("  --help             Show this help.");
		Console.WriteLine();
		Console.WriteLine("The exit code is 1 if a script check failed or the script timed out.");
	}
}
