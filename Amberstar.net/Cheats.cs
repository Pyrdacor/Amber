using System.Linq;
using System.Text;

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

    private static void Teleport(G game, string[] args)
    {

    }
}
