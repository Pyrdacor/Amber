using System.Linq;
using System.Text;
using Amberstar.GameData;

namespace Amberstar;

using G = Game.Game;

internal static class Cheats
{
    record Parameter(string Name)
    {
        public Parameter(string name, string description)
            : this(name)
        {
            Description = description;
        }

        public string Description { get; } = "";

        public static implicit operator Parameter(string str) => new(str);
    }

    static readonly Dictionary<string, (Action<G, string[]> Action, string Description, int RequiredParameterCount, Parameter[] Parameters)> commands = [];
    static readonly Queue<string> commandQueue = [];

    static Cheats()
    {
        static void AddCommand(string name, Action<G, string[]> action, string description, int requiredParameterCount, params Parameter[] parameters)
            => commands.Add(name, (action, description, requiredParameterCount, parameters));

        AddCommand("help", Help, "Shows the help (for some command)", 0, "command");
        AddCommand("teleport", Teleport, "Teleports to a new location", 1, "mapIndex", "x", "y", new("dir", "0: Up, 1: Right, 2: Down, 3: Left"));
        AddCommand("maps", Maps, "Shows all maps", 0, "partialMapNameOrIndex");
    }

    public static void Init()
    {
        Help(null!, []);
        EndCommand();
    }

    public static void EnqueueCommand(string input)
    {
        lock (commandQueue)
        {
            commandQueue.Enqueue(input);
        }
    }

    public static void HandleQueuedCommands(G game)
    {
        lock (commandQueue)
        {
            while (commandQueue.Count > 0)
            {
                string command = commandQueue.Dequeue();

                ProcessCheatCommand(game, command);
            }
        }
    }

    private static void ProcessCheatCommand(G game, string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            EndCommand();
            return;
        }

        var parts = command.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (!commands.TryGetValue(parts[0].ToLower(), out var commandInfo))
        {
            Console.WriteLine($"Invalid command: '{parts[0]}'");
            EndCommand();
            return;
        }

        int paramCount = parts.Length - 1;

        if (paramCount < commandInfo.RequiredParameterCount)
        {
            Console.WriteLine($"Command '{parts[0]}' needs at least {commandInfo.RequiredParameterCount} parameter(s). Use 'help {parts[0]}'");
            EndCommand();
            return;
        }
        else if (paramCount > commandInfo.Parameters.Length && parts[0].ToLower() != "help")
        {
            Console.WriteLine($"Command '{parts[0]}' supports at max {commandInfo.Parameters.Length} parameter(s). Use 'help {parts[0]}'");
            EndCommand();
            return;
        }

        commandInfo.Action(game, parts[1..]);
        EndCommand();
    }

    private static void EndCommand()
    {
        Console.WriteLine();
        Console.Write("> ");
    }

    static bool PrintCommandInfo(string name)
    {
        if (!commands.TryGetValue(name, out var command))
            return false;

        var lineBuilder = new StringBuilder();

        lineBuilder.Append("  ");
        lineBuilder.Append(name);

        for (int i = 0; i < command.Parameters.Length; i++)
            lineBuilder.Append(i < command.RequiredParameterCount ? $" <{command.Parameters[i].Name}>" : $" ({command.Parameters[i].Name})");

        lineBuilder.Append(" - ");

        int offset = lineBuilder.Length;
        var indent = new string(' ', offset);
        var description = command.Description.Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd('\n').Replace("\n", "\n" + indent);

        lineBuilder.AppendLine(description);

        foreach (var param in command.Parameters.Where(p => p.Description.Length > 0))
            lineBuilder.AppendLine($"    {param.Name} - {param.Description}");

        Console.Write(lineBuilder.ToString());

        return true;
    }

    internal static void Help(G _, string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Available commands:");
            Console.WriteLine();

            foreach (var command in commands.Keys.Order())
                Console.WriteLine(command);
        }
        else
        {
            if (!PrintCommandInfo(args[0].ToLower()))
                Console.WriteLine($"Unknown command: '{args[0]}'");
        }
    }

    private static int ParseNumber(string arg, int defaultValue) => int.TryParse(arg, out int n) ? n : defaultValue;

    private static TEnum ParseNumberAsEnum<TEnum>(string arg, TEnum defaultValue) where TEnum : struct, Enum => (TEnum)Enum.ToObject(typeof(TEnum), ParseNumber(arg, Convert.ToInt32(defaultValue)));

    private static TEnum ParseEnum<TEnum>(string arg, TEnum defaultValue) where TEnum : struct, Enum => Enum.TryParse(arg, true, out TEnum n) ? n : ParseNumberAsEnum(arg, defaultValue);

    private static void Maps(G game, string[] args)
    {
        IEnumerable<(int Index, string Name)> maps = game.GetMaps();

        if (args.Length > 0)
        {
            if (int.TryParse(args[0], out var partialIndex))
            {
                var partialIndexString = partialIndex.ToString();
                maps = maps.Where(map => map.Index.ToString().Contains(partialIndexString) || map.Name.ToLower().Contains(partialIndexString));
            }
            else
            {
                var partialName = args[0].ToLower();
                maps = maps.Where(map => map.Name.ToLower().Contains(partialName));
            }
        }

        if (!maps.Any())
        {
            Console.WriteLine("No matching maps found.");
            return;
        }

        foreach (var map in maps.OrderBy(map => map.Index))
        {
            Console.WriteLine($"{map.Index:000}: {map.Name}");
        }
    }

    private static void Teleport(G game, string[] args)
    {
        int mapIndex = ParseNumber(args[0], -1);

        if (mapIndex == -1)
        {
            Console.WriteLine("Invalid map index. Use 'maps'");
            return;
        }

        var mapSize = game.GetMapSize(mapIndex);

        if (mapSize == null)
        {
            Console.WriteLine($"Map {mapIndex} does not exist. Use 'maps'");
            return;
        }

        int x = args.Length < 2 ? G.Random(1, mapSize.Value.Width) : ParseNumber(args[1], -1);
        int y = args.Length < 3 ? G.Random(1, mapSize.Value.Height) : ParseNumber(args[2], -1);
        var direction = args.Length < 4 ? (Direction)G.Random(0, 3) : ParseEnum(args[3], Direction.Keep);

        if (x == -1)
        {
            Console.WriteLine("Invalid x value");
            return;
        }

        if (y == -1)
        {
            Console.WriteLine("Invalid y value");
            return;
        }

        if (direction == Direction.Keep)
        {
            Console.WriteLine("Invalid direction, using 'keep'");
        }

        game.Teleport(x, y, direction, mapIndex, fade: false);
    }
}
