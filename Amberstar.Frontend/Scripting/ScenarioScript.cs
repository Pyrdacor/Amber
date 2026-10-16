using Amber.Common;
using Amberstar.Game;
using Amberstar.GameData;

namespace Amberstar.Frontend.Scripting;

using G = Game.Game;

/// <summary>
/// Executes a test scenario line by line while the game runs
/// (command line option --script or the automated tests).
///
/// All waiting is based on the game time, so a script behaves the same in the
/// window and headless (where the game runs as fast as possible).
///
/// Map positions and party member slots are 1-based. Commands which print a value
/// also check it if an expected value is given. Numeric values can be checked
/// against the last printed value of the same thing with +n, -n, + or -.
///
/// Directives at the beginning of a script (used by the command line and the tests):
///   #! data &lt;path&gt;             game data, e.g. English/AmberfilesST (default German/AmberfilesST)
///   #! seed &lt;number&gt;           seed for random numbers (default 0)
///   #! maxtime &lt;seconds&gt;       maximum game time for headless runs (default 600)
///
/// Commands (one per line, # starts a comment):
///   wait &lt;ms&gt;                 wait some milliseconds of game time
///   waitidle [timeout ms]     wait until a map is shown and the player could move (default timeout 10000)
///   waittext [timeout ms]     wait until a text box, picture text or confirmation is shown (default 10000)
///   waitscreen &lt;type&gt; [timeout ms]
///                             wait until the given screen (e.g. Chest, Conversation) is active (default 10000)
///   dismiss                   scroll or close the shown text or end a message which waits for a click (like a click)
///   dismissall [timeout ms]   close all texts until the game is idle (default timeout 15000)
///   closeall [timeout ms]     like dismissall but also answers confirmations with no and leaves all other
///                             screens like doors, chests or conversations with escape (default timeout 15000)
///   yes / no                  answer the shown confirmation window
///   answer &lt;word&gt;             answer the shown riddlemouth with a word or a dictionary index like #1615
///   word &lt;index&gt; [text]       print a dictionary word, optionally check it
///   tile &lt;x&gt; &lt;y&gt; [tile]       print a tile of the shown map (3D: lab tile, 2D: underlay/overlay like 12/0),
///                             optionally check it
///   useitem &lt;item&gt;            use an item of the active party member at the party position like the inventory
///                             (e.g. a key on a use item event), items which are used up are removed.
///                             "useitem &lt;item&gt; noeffect" checks that the item has no effect.
///   button &lt;index&gt;           press a button of the button grid (0 to 8, row by row, like the keypad
///                             keys 7, 8, 9, 4, ...), it is a failure if the button is disabled
///   buttonenabled &lt;index&gt; [0|1]
///                             print if a button is enabled, optionally check it
///   amount &lt;amount&gt;          enter an amount in the shown amount input window
///   portrait &lt;slot&gt;          click the portrait of a party member (e.g. to give gold)
///   slot &lt;index&gt;             click one of the 12 item slots (1-based) of doors, chests or places
///   placegold [amount]        print the gold of the shown place, optionally check it
///   identified &lt;item&gt; [0|1]   print if an item of the active party member is identified, optionally check it
///   placefood [amount]        print the food of the shown food dealer, optionally check it
///   transport &lt;map&gt; &lt;x&gt; &lt;y&gt; [type]
///                             print the transport at a position (none, Horse, Raft or Ship), optionally check it
///   key &lt;key&gt;                 press and release a key (Amberstar.Game.Key name, e.g. Enter, Escape, F5)
///   hold &lt;key&gt; &lt;ms&gt;          hold a key for some milliseconds (e.g. movement keys)
///   click &lt;x&gt; &lt;y&gt; [right]     click at a game position (320x200)
///   hover &lt;x&gt; &lt;y&gt;             move the mouse to a game position (320x200)
///   move &lt;dir&gt; [steps]       move the party like the player does (dir: up, down, left, right, upleft,
///                             upright, downleft, downright or north, east, south, west), one step per frame.
///                             Map events on the way are triggered. If something interrupts the movement (a text,
///                             a map change, ...) the remaining steps are skipped, which is no failure. A blocked
///                             step is a failure. Use "move &lt;dir&gt; &lt;steps&gt; optional" if it may be blocked.
///   turn &lt;dir&gt;                change the party direction
///   teleport &lt;map&gt; &lt;x&gt; &lt;y&gt; [dir]
///                             teleport the party without fading
///   trigger &lt;x&gt; &lt;y&gt; [trigger] trigger the map event at a position (Eye, Move, Ear, Mouth or UseItem, default Eye).
///                             With Mouth a person at that position is talked to.
///   cheat &lt;command&gt;           execute a cheat console command
///   seed &lt;number&gt;             make random numbers reproducible
///   settime &lt;hour&gt; [minute]   set the time of day
///   hour [hour]               print the hour, optionally check it
///   state                     print map, position, screen and other state
///   map [index]               print the map index, optionally check it
///   pos [x y]                 print the party position, optionally check it
///   dir [direction]           print the party direction, optionally check it
///   screen [type]             print the active screen type, optionally check it
///   eventactive &lt;map&gt; &lt;event&gt; [0|1]
///                             print if a map event (1-based) would be executed, optionally check it
///   eventbit &lt;map&gt; &lt;save index&gt; [0|1]
///                             print an event bit (by the save index of an event, see the cheat 'events'),
///                             optionally check it. A set bit disables all events with this save index.
///   seteventbit &lt;map&gt; &lt;save index&gt; &lt;0|1&gt;
///                             set or clear an event bit
///   questbit &lt;bit&gt; [0|1]      print a quest bit, optionally check it
///   setquestbit &lt;bit&gt; &lt;0|1&gt;   set or clear a quest bit
///   charactive &lt;map&gt; &lt;char&gt; [0|1]
///                             print if a map character (1-based) is still active, optionally check it
///   wordknown &lt;index&gt; [0|1]   print if a dictionary word is known, optionally check it
///   partysize [count]         print the party size, optionally check it
///   activate &lt;slot&gt;           make the party member in slot 1-6 the active one
///   value &lt;slot&gt; &lt;name&gt; [v]   print a party member value (gold, food, hp, maxhp, sp, maxsp, level, exp, slp,
///                             weight or an attribute or skill name), optionally check it
///   setvalue &lt;slot&gt; &lt;name&gt; &lt;v&gt; set a party member value
///   condition &lt;slot&gt; &lt;condition&gt; [0|1]
///                             print if a party member has a condition (e.g. Poisoned), optionally check it
///   setcondition &lt;slot&gt; &lt;condition&gt; &lt;0|1&gt;
///                             add or remove a condition
///   count &lt;item&gt; [amount]     print the amount of an item in the party, optionally check it
///   give &lt;slot&gt; &lt;item&gt; [count] add an item to the inventory of a party member
///   texts                     print the texts shown since the last cleartexts
///   textshown &lt;text&gt;          check that a shown text contained the given text (case insensitive)
///   notextshown [text]        check that no text (containing the given text) was shown
///   cleartexts                forget the shown texts
///   log &lt;text&gt;                print text
///   fail &lt;text&gt;               report a failure (e.g. as a reminder for something not implemented)
///   exit                      close the game
/// </summary>
public sealed class ScenarioScript
{
	sealed record Wait(string Name, double Timeout, Func<G, bool> Done, Action<G>? Poll = null, Action? Finish = null, bool Optional = false);

	sealed class Movement(int x, int y, int steps, bool optional)
	{
		public int X { get; } = x;
		public int Y { get; } = y;
		public int Steps { get; set; } = steps;
		public bool Optional { get; } = optional;
	}

	const double DefaultTimeout = 10000.0;
	const double DismissInterval = 100.0;
	static readonly Position TextClickPosition = new(160, 100);
	readonly Queue<(int Line, string Command)> commands = new();
	readonly Action<string> output;
	readonly Dictionary<string, int> lastValues = [];
	double time = 0.0;
	double continueTime = 0.0;
	double lastDismissTime = double.MinValue;
	Wait? wait = null;
	Movement? movement = null;
	int currentLine = 0;
	bool exited = false;

	public ScenarioScript(IEnumerable<string> lines, Action<string>? output = null)
	{
		this.output = output ?? Console.WriteLine;
		int lineNumber = 0;

		foreach (var line in lines)
		{
			++lineNumber;
			string command = line.Trim();

			if (command.StartsWith("#!"))
				ParseDirective(command[2..].Trim());
			else if (command.Length != 0 && !command.StartsWith('#'))
				commands.Enqueue((lineNumber, command));
		}
	}

	public static ScenarioScript FromFile(string path, Action<string>? output = null) => new(File.ReadAllLines(path), output);

	public string? DataPath { get; private set; }
	public int? RandomSeed { get; private set; }
	public double? MaxGameSeconds { get; private set; }
	public int Checks { get; private set; } = 0;
	public int Failures { get; private set; } = 0;
	public bool Finished => exited || (commands.Count == 0 && wait == null && movement == null && time >= continueTime);
	public string Summary => $"Script finished: {Checks - Failures} of {Checks} checks passed, {Failures} failed.";

	void ParseDirective(string directive)
	{
		var parts = directive.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

		if (parts.Length != 2)
			throw new FormatException($"invalid script directive '{directive}'");

		switch (parts[0].ToLowerInvariant())
		{
			case "data":
				DataPath = parts[1].Trim();
				break;
			case "seed":
				RandomSeed = int.Parse(parts[1]);
				break;
			case "maxtime":
				MaxGameSeconds = double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
				break;
			default:
				throw new FormatException($"unknown script directive '{parts[0]}'");
		}
	}

	/// <summary>
	/// Aborts the script with a failure, e.g. if it took too long.
	/// </summary>
	public void Abort(string reason, G game)
	{
		Report(false, $"aborted at line {currentLine}: {reason} ({game.ScriptState()})");
		commands.Clear();
		wait = null;
		movement = null;
		continueTime = time;
	}

	/// <summary>
	/// Executes commands until a waiting command is reached.
	/// Must be called once per frame before the game is updated.
	/// </summary>
	public void Update(G game, IScenarioScriptHost host, double elapsedSeconds)
	{
		time += elapsedSeconds * 1000.0;

		while (!exited)
		{
			if (time < continueTime)
				return;

			if (wait != null)
			{
				if (wait.Done(game))
				{
					wait.Finish?.Invoke();
					wait = null;
				}
				else if (time > wait.Timeout)
				{
					wait.Finish?.Invoke();

					if (wait.Optional)
						output($"  {wait.Name} did not happen");
					else
						Report(false, $"{wait.Name} timed out: {game.ScriptState()}");

					wait = null;
				}
				else
				{
					wait.Poll?.Invoke(game);
					return;
				}
			}

			if (movement != null)
			{
				UpdateMovement(game);
				return;
			}

			if (commands.Count == 0)
				return;

			var (lineNumber, command) = commands.Dequeue();
			currentLine = lineNumber;
			output($"[script {lineNumber}] {command}");

			try
			{
				Execute(game, host, command);
			}
			catch (Exception ex)
			{
				Report(false, $"line {lineNumber} failed: {ex.Message} ({game.ScriptState()})");

				foreach (var frame in (ex.StackTrace ?? "").Split(Environment.NewLine).Take(4))
					output("    " + frame.Trim());
			}
		}
	}

	void Execute(G game, IScenarioScriptHost host, string command)
	{
		var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		string name = parts[0].ToLowerInvariant();
		string argument = command[parts[0].Length..].Trim();
		var args = parts[1..];

		int Arg(int index) => int.Parse(args[index]);
		int? OptionalArg(int index) => args.Length > index ? int.Parse(args[index]) : null;
		bool? OptionalBool(int index) => args.Length > index ? ParseBool(args[index]) : null;
		double Timeout(int index) => time + (OptionalArg(index) ?? DefaultTimeout);

		switch (name)
		{
			case "wait":
				continueTime = time + Arg(0);
				break;
			case "waitidle":
				wait = new("waitidle", Timeout(0), g => g.ScriptIdle);
				break;
			case "waittext":
				wait = new("waittext", Timeout(0), IsTextShown);
				break;
			case "waitscreen":
			{
				var screen = args[0];
				wait = new($"waitscreen {screen}", Timeout(1), g => string.Equals(g.ScriptActiveScreen, screen, StringComparison.OrdinalIgnoreCase));
				break;
			}
			case "dismiss":
				if (!game.ScriptWaitingForClick)
					Report(false, $"no text to dismiss: {game.ScriptState()}");
				else
					Click(game, TextClickPosition, MouseButtons.Left);
				// give the game a frame to react
				continueTime = time + 1;
				break;
			case "dismissall":
				wait = new("dismissall", time + (OptionalArg(0) ?? 15000), g => g.ScriptIdle, DismissTexts);
				break;
			case "closeall":
				wait = new("closeall", time + (OptionalArg(0) ?? 15000), g => g.ScriptIdle, CloseScreens);
				break;
			case "yes":
			case "no":
				CheckResult(game.ScriptAnswer(name == "yes"));
				continueTime = time + 1;
				break;
			case "button":
				CheckResult(game.ScriptPressButton(Arg(0)));
				// buttons react after a short highlight
				continueTime = time + 300;
				break;
			case "buttonenabled":
			{
				var enabled = game.ScriptIsButtonEnabled(Arg(0));

				if (enabled == null)
					Report(false, $"the active screen {game.ScriptActiveScreen} has no buttons");
				else
					CheckBool($"buttonenabled {Arg(0)}", enabled.Value, OptionalBool(1));
				break;
			}
			case "amount":
				CheckResult(game.ScriptEnterAmount(Arg(0)));
				continueTime = time + 1;
				break;
			case "portrait":
				Click(game, new Position(16 + (Arg(0) - 1) * 48 + 16, 17), MouseButtons.Left);
				continueTime = time + 1;
				break;
			case "slot":
			{
				int slot = Arg(0) - 1;
				Click(game, new Position(16 + (slot % 6) * 32 + 8, 37 + 108 + (slot / 6) * 32 + 8), MouseButtons.Left);
				continueTime = time + 1;
				break;
			}
			case "identified":
			{
				var identified = game.ScriptIsItemIdentified((uint)Arg(0));

				if (identified == null)
					Report(false, $"the active party member has no item {Arg(0)}");
				else
					CheckBool($"identified {Arg(0)}", identified.Value, OptionalBool(1));
				break;
			}
			case "placegold":
			case "placefood":
			{
				var value = name == "placegold" ? game.ScriptPlaceGold : game.ScriptPlaceFood;

				if (value == null)
					Report(false, $"no matching place is shown but {game.ScriptActiveScreen}");
				else
					CheckValue(name, value.Value, args.FirstOrDefault());
				break;
			}
			case "transport":
			{
				var transport = game.ScriptGetTransport(Arg(0), Arg(1), Arg(2)) ?? "none";

				if (args.Length < 4)
					output($"  transport = {transport}");
				else
					Report(transport.Equals(args[3], StringComparison.OrdinalIgnoreCase), $"transport = {transport} (expected {args[3]})");
				break;
			}
			case "answer":
				CheckResult(game.ScriptAnswer(argument.StartsWith('#') ? game.ScriptGetWord(int.Parse(argument[1..])) : argument));
				continueTime = time + 1;
				break;
			case "word":
			{
				var word = game.ScriptGetWord(Arg(0));

				if (args.Length < 2)
					output($"  word {Arg(0)} = {word}");
				else
					Report(word.Equals(args[1], StringComparison.OrdinalIgnoreCase), $"word {Arg(0)} = {word} (expected {args[1]})");
				break;
			}
			case "tile":
			{
				var tile = game.ScriptGetTile(Arg(0), Arg(1));

				if (tile == null)
					Report(false, $"no tile at {Arg(0)},{Arg(1)}: {game.ScriptState()}");
				else if (args.Length < 3)
					output($"  tile {Arg(0)},{Arg(1)} = {tile}");
				else
					Report(tile == args[2], $"tile {Arg(0)},{Arg(1)} = {tile} (expected {args[2]})");
				break;
			}
			case "useitem":
				if (args.Length > 1 && args[1].Equals("noeffect", StringComparison.OrdinalIgnoreCase))
				{
					var error = game.ScriptUseItem((uint)Arg(0));
					Report(error != null, $"item {Arg(0)} has no effect ({error ?? "but it had one"})");
				}
				else
				{
					CheckResult(game.ScriptUseItem((uint)Arg(0)));
				}
				continueTime = time + 1;
				break;
			case "key":
				PressKey(game, ParseEnum<Key>(args[0]));
				continueTime = time + 1;
				break;
			case "hold":
			{
				var key = ParseEnum<Key>(args[0]);
				double end = time + Arg(1);
				host.SetKeyHeld(key, true);
				wait = new($"hold {key}", double.MaxValue, _ => time >= end, Finish: () => host.SetKeyHeld(key, false));
				break;
			}
			case "click":
			{
				var position = new Position(Arg(0), Arg(1));
				var button = args.Length > 2 && args[2].Equals("right", StringComparison.OrdinalIgnoreCase) ? MouseButtons.Right : MouseButtons.Left;
				Click(game, position, button);
				continueTime = time + 1;
				break;
			}
			case "hover":
				game.MouseMove(new Position(Arg(0), Arg(1)), MouseButtons.None);
				break;
			case "move":
			{
				var (x, y) = ParseMoveDirection(args[0]);
				int steps = OptionalArg(1) ?? 1;
				bool optional = args.Length > 2 && args[2].Equals("optional", StringComparison.OrdinalIgnoreCase);
				movement = new(x, y, steps, optional);
				break;
			}
			case "turn":
				CheckResult(game.ScriptTurn(ParseDirection(args[0])));
				break;
			case "teleport":
			{
				// without fading the transition happens immediately
				int transitions = game.ScriptMapTransitions;
				var error = game.ScriptTeleport(Arg(0), Arg(1), Arg(2), args.Length > 3 ? ParseDirection(args[3]) : Direction.Keep);
				CheckResult(error);

				// Wait for the new map. Events at the target may show a text, open a screen
				// like a chest or even lead to another map.
				if (error == null)
				{
					wait = new("teleport", time + DefaultTimeout, g => !g.ScriptFading && g.ScriptMapTransitions > transitions);
				}
				break;
			}
			case "trigger":
				CheckResult(game.ScriptTrigger(args.Length > 2 ? args[2] : "Eye", Arg(0), Arg(1)));
				continueTime = time + 1;
				break;
			case "cheat":
				Cheats.Execute(game, argument);
				break;
			case "seed":
				G.SetRandomSeed(Arg(0));
				break;
			case "settime":
				game.ScriptSetTime(Arg(0), OptionalArg(1) ?? 0);
				break;
			case "hour":
				CheckValue("hour", game.ScriptHour, args.FirstOrDefault());
				break;
			case "state":
				output("  " + game.ScriptState());
				break;
			case "map":
				CheckValue("map", game.ScriptMapIndex, args.FirstOrDefault());
				break;
			case "pos":
			{
				var position = game.ScriptPartyPosition;
				string actual = $"{position.X},{position.Y}";

				if (args.Length < 2)
					output($"  pos = {actual}");
				else
					Report(actual == $"{Arg(0)},{Arg(1)}", $"pos = {actual} (expected {Arg(0)},{Arg(1)})");
				break;
			}
			case "dir":
			{
				var direction = game.ScriptPartyDirection;

				if (args.Length == 0)
					output($"  dir = {direction}");
				else
					Report(direction == ParseDirection(args[0]), $"dir = {direction} (expected {args[0]})");
				break;
			}
			case "screen":
			{
				var screen = game.ScriptActiveScreen ?? "none";

				if (args.Length == 0)
					output($"  screen = {screen}");
				else
					Report(screen.Equals(args[0], StringComparison.OrdinalIgnoreCase), $"screen = {screen} (expected {args[0]})");
				break;
			}
			case "eventactive":
			{
				var active = game.ScriptIsEventActive(Arg(0), Arg(1));

				if (active == null)
					Report(false, $"event {Arg(1)} of map {Arg(0)} does not exist");
				else
					CheckBool($"eventactive {Arg(0)} {Arg(1)}", active.Value, OptionalBool(2));
				break;
			}
			case "eventbit":
				CheckBool($"eventbit {Arg(0)} {Arg(1)}", game.ScriptGetEventBit(Arg(0), Arg(1)), OptionalBool(2));
				break;
			case "seteventbit":
				game.ScriptSetEventBit(Arg(0), Arg(1), ParseBool(args[2]));
				break;
			case "questbit":
				CheckBool($"questbit {Arg(0)}", game.ScriptIsQuestBitSet(Arg(0)), OptionalBool(1));
				break;
			case "setquestbit":
				game.ScriptSetQuestBit(Arg(0), ParseBool(args[1]));
				break;
			case "charactive":
				CheckBool($"charactive {Arg(0)} {Arg(1)}", game.ScriptIsMapCharacterActive(Arg(0), Arg(1)), OptionalBool(2));
				break;
			case "wordknown":
				CheckBool($"wordknown {Arg(0)}", game.ScriptIsWordKnown(Arg(0)), OptionalBool(1));
				break;
			case "partysize":
				CheckValue("partysize", game.ScriptPartySize, args.FirstOrDefault());
				break;
			case "activate":
				if (!game.ScriptSetActivePartyMember(Arg(0)))
					Report(false, $"no party member in slot {Arg(0)}");
				break;
			case "value":
			{
				var value = game.ScriptGetPartyMemberValue(Arg(0), args[1]);

				if (value == null)
					Report(false, $"value {args[1]} of slot {Arg(0)} is not available");
				else
					CheckValue($"value {Arg(0)} {args[1].ToLowerInvariant()}", value.Value, args.ElementAtOrDefault(2));
				break;
			}
			case "setvalue":
				CheckResult(game.ScriptSetPartyMemberValue(Arg(0), args[1], Arg(2)));
				break;
			case "condition":
			{
				var conditions = game.ScriptGetConditions(Arg(0));
				var condition = ParseEnum<Condition>(args[1]);

				if (conditions == null)
					Report(false, $"no party member in slot {Arg(0)}");
				else
					CheckBool($"condition {Arg(0)} {condition}", conditions.Value.HasFlag(condition), OptionalBool(2));
				break;
			}
			case "setcondition":
				CheckResult(game.ScriptSetCondition(Arg(0), args[1], ParseBool(args[2])));
				break;
			case "count":
				CheckValue($"count {Arg(0)}", game.ScriptCountItem((uint)Arg(0)), args.ElementAtOrDefault(1));
				break;
			case "give":
				CheckResult(game.ScriptGiveItem(Arg(0), (uint)Arg(1), OptionalArg(2) ?? 1));
				break;
			case "texts":
				foreach (var text in game.ScriptShownTexts)
					output($"  text: {text.Replace("\n", " | ")}");
				break;
			case "textshown":
				Report(game.ScriptShownTexts.Any(text => text.Contains(argument, StringComparison.OrdinalIgnoreCase)),
					$"text \"{argument}\" was shown{ShownTextsInfo(game)}");
				break;
			case "notextshown":
				Report(!game.ScriptShownTexts.Any(text => text.Contains(argument, StringComparison.OrdinalIgnoreCase)),
					(argument.Length == 0 ? "no text was shown" : $"text \"{argument}\" was not shown") + ShownTextsInfo(game));
				break;
			case "cleartexts":
				game.ScriptClearShownTexts();
				break;
			case "log":
				output("  " + argument);
				break;
			case "fail":
				Report(false, argument);
				break;
			case "exit":
				exited = true;
				commands.Clear();
				host.Exit();
				break;
			default:
				Report(false, $"unknown command '{parts[0]}'");
				break;
		}
	}

	void UpdateMovement(G game)
	{
		var move = movement!;

		if (move.Steps <= 0)
		{
			movement = null;
			return;
		}

		if (!game.ScriptIdle)
		{
			output($"  movement interrupted with {move.Steps} step(s) left: {game.ScriptState()}");
			movement = null;
			return;
		}

		var error = game.ScriptMove(move.X, move.Y);

		if (error != null)
		{
			if (move.Optional)
				output($"  movement stopped: {error}");
			else
				Report(false, $"move failed: {error} ({game.ScriptState()})");

			movement = null;
			return;
		}

		move.Steps--;
	}

	static bool IsTextShown(G game) => game.ScriptActiveScreen is "TextBox" or "PictureText" or "Confirmation";

	void DismissTexts(G game)
	{
		if (time - lastDismissTime < DismissInterval || game.ScriptFading)
			return;

		var screen = game.ScriptActiveScreen;

		if (game.ScriptWaitingForClick)
		{
			// text screens scroll or close on any click, other screens end the message
			Click(game, TextClickPosition, MouseButtons.Left);
			lastDismissTime = time;
		}
		else if (screen == "Confirmation")
		{
			// confirmations must be answered explicitly with yes or no
			wait = null;
			Report(false, $"dismissall reached a confirmation: {game.ScriptState()}");
		}
	}

	void CloseScreens(G game)
	{
		if (time - lastDismissTime < DismissInterval || game.ScriptFading || !game.ScriptInputEnabled)
			return;

		lastDismissTime = time;

		if (game.ScriptWaitingForClick)
			Click(game, TextClickPosition, MouseButtons.Left);
		else if (game.ScriptActiveScreen == "Confirmation")
			game.ScriptAnswer(false);
		else if (game.ScriptActiveScreen is not ("Map2D" or "Map3D"))
			PressKey(game, Key.Escape);
	}

	static void Click(G game, Position position, MouseButtons button)
	{
		game.MouseMove(position, MouseButtons.None);
		game.MouseDown(position, button, KeyModifiers.None);
		game.MouseUp(position, button, KeyModifiers.None);
	}

	static void PressKey(G game, Key key)
	{
		game.KeyDown(key, KeyModifiers.None);
		game.KeyUp(key, KeyModifiers.None);
	}

	static string ShownTextsInfo(G game)
	{
		if (game.ScriptShownTexts.Count == 0)
			return " (no texts were shown)";

		return " (shown: " + string.Join(" / ", game.ScriptShownTexts.Select(text => $"\"{text.Replace("\n", " ")}\"")) + ")";
	}

	void CheckResult(string? error)
	{
		if (error != null)
			Report(false, error);
	}

	void CheckBool(string name, bool actual, bool? expected)
	{
		if (expected == null)
			output($"  {name} = {(actual ? 1 : 0)}");
		else
			Report(actual == expected, $"{name} = {(actual ? 1 : 0)} (expected {(expected.Value ? 1 : 0)})");
	}

	void CheckValue(string name, int actual, string? expected)
	{
		bool hasLastValue = lastValues.TryGetValue(name, out int lastValue);
		lastValues[name] = actual;

		if (expected == null)
		{
			output($"  {name} = {actual}");
			return;
		}

		bool ok;

		if (expected == "+" || expected == "-")
		{
			if (!hasLastValue)
				throw new InvalidOperationException($"{name} was not printed before, so it can't be compared with '{expected}'");

			ok = expected == "+" ? actual > lastValue : actual < lastValue;
		}
		else if (expected.StartsWith('+') || expected.StartsWith('-'))
		{
			if (!hasLastValue)
				throw new InvalidOperationException($"{name} was not printed before, so it can't be compared with '{expected}'");

			ok = actual == lastValue + int.Parse(expected);
		}
		else
		{
			ok = actual == int.Parse(expected);
		}

		Report(ok, $"{name} = {actual} (expected {expected}{(hasLastValue && (expected[0] == '+' || expected[0] == '-') ? $" from {lastValue}" : "")})");
	}

	void Report(bool ok, string message)
	{
		Checks++;

		if (!ok)
			Failures++;

		output((ok ? "  PASS " : "  FAIL ") + message);
	}

	static bool ParseBool(string value) => value.ToLowerInvariant() switch
	{
		"1" or "true" or "yes" or "on" => true,
		"0" or "false" or "no" or "off" => false,
		_ => throw new FormatException($"invalid boolean value '{value}'")
	};

	static TEnum ParseEnum<TEnum>(string value) where TEnum : struct, Enum
	{
		if (Enum.TryParse<TEnum>(value, true, out var result))
			return result;

		throw new FormatException($"invalid {typeof(TEnum).Name} '{value}'");
	}

	static Direction ParseDirection(string value) => value.ToLowerInvariant() switch
	{
		"up" or "north" or "n" => Direction.Up,
		"right" or "east" or "e" => Direction.Right,
		"down" or "south" or "s" => Direction.Down,
		"left" or "west" or "w" => Direction.Left,
		_ => throw new FormatException($"invalid direction '{value}'")
	};

	static (int X, int Y) ParseMoveDirection(string value) => value.ToLowerInvariant() switch
	{
		"up" or "north" or "n" => (0, -1),
		"right" or "east" or "e" => (1, 0),
		"down" or "south" or "s" => (0, 1),
		"left" or "west" or "w" => (-1, 0),
		"upleft" or "nw" => (-1, -1),
		"upright" or "ne" => (1, -1),
		"downleft" or "sw" => (-1, 1),
		"downright" or "se" => (1, 1),
		_ => throw new FormatException($"invalid move direction '{value}'")
	};
}
