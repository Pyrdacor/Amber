# Scenario scripts

Each `.txt` file in this folder is a scenario script. It plays a part of the game like a player would
(moving, triggering events, answering questions, closing texts) and checks the game state afterwards.

All scripts are executed by the automated tests (`Amberstar.Tests`, class `ScenarioTests`):

```
dotnet test Amberstar.Tests
```

A single script can also be run with the game itself, either visible in the window or headless:

```
Amberstar.net --script Tests/Scenarios/Graveyard.txt
Amberstar.net --headless --script Tests/Scenarios/Graveyard.txt
```

The exit code is 1 if a check failed or the script timed out, so headless runs can be used in CI.
`Amberstar.net --help` lists all command line options.

## How scripts run

- The game starts from the initial savegame of the game data, so every script begins on the graveyard
  of Twinlake with the intro text. Most scripts start with `waittext` and `dismissall`.
- All waiting uses the game time. Headless runs update the game in fixed steps of 1/60 second as fast
  as possible, so they are reproducible and much faster than real time.
- Random numbers use the seed 0 unless the script or `--seed` says otherwise.
- Every command is printed as `[script <line>] <command>`. Checks print `PASS` or `FAIL`, and at the end
  `Script finished: x of y checks passed, z failed.` is printed.
- Map positions and party member slots are 1-based like in the cheats and the data docs.
  On the world map, positions are positions inside the 50x50 map parts (maps 1 to 64).
- `teleport` only works while the map is shown and nothing happens, so use `dismissall` or `closeall` before.
- If the game crashes, the script is aborted with a failure that contains the exception.

## Directives

Directives at the beginning of a script configure the run. Command line options win over them.

| Directive               | Meaning                                                      |
|-------------------------|--------------------------------------------------------------|
| `#! data <path>`        | Game data, e.g. `English/AmberfilesST` (default: German ST)  |
| `#! seed <number>`      | Seed for random numbers (default: 0)                         |
| `#! maxtime <seconds>`  | Maximum game time for headless runs (default: 600)           |

## Commands

The full command reference is the documentation comment of `Amberstar.Frontend/Scripting/ScenarioScript.cs`.
The most important ones:

| Command                                  | Description                                                          |
|------------------------------------------|----------------------------------------------------------------------|
| `waittext`, `waitidle`, `waitscreen <t>` | Wait until a text, the map or a screen is shown                      |
| `dismiss`, `dismissall`                  | Scroll or close texts (dismissall until the map is shown again)      |
| `closeall`                               | Like dismissall, but also leaves doors, chests, places, ...          |
| `yes`, `no`                              | Answer a confirmation window                                         |
| `answer <word\|#index>`                  | Answer a riddlemouth (a dictionary index keeps scripts language-independent) |
| `useitem <item> [noeffect]`              | Use an item at the party position like the inventory                 |
| `move <dir> [steps] [optional]`          | Move like the player, events on the way are triggered                |
| `teleport <map> <x> <y> [dir]`           | Teleport without fading                                              |
| `trigger <x> <y> [Eye\|Mouth\|...]`      | Trigger the event at a position, Mouth talks to persons              |
| `key`, `hold`, `click`, `hover`          | Raw input                                                            |
| `button <0-8>`, `buttonenabled`          | Press or check a button of the button grid (row by row)              |
| `slot <1-12>`, `portrait <slot>`         | Click an item slot or a party member portrait                        |
| `amount <n>`                             | Enter an amount in the amount input window                           |
| `placegold`, `placefood`, `transport`    | Check the gold/food pile of a place or a bought transport            |
| `spellknown <slot> <school> <spell>`     | Check if a party member knows a spell                                |
| `automap <x> <y> [icon]`, `automapview`  | Check the shown automap                                              |
| `trainskill`, `finishtraining`, `class`  | Distribute skill points after a level-up in a guild                  |
| `map`, `pos`, `dir`, `screen`            | Print or check the location and the active screen                    |
| `tile <x> <y> [tile]`                    | Check a tile of the shown map (3D: lab tile, 2D: `underlay/overlay`) |
| `word <index> [text]`                    | Print or check a dictionary word                                     |
| `eventactive <map> <event> [0\|1]`       | Check if an event would be executed                                  |
| `eventbit <map> <save index> [0\|1]`     | Check an event bit (see below)                                       |
| `value <slot> <name> [v\|+n\|-n\|+\|-]`  | Check gold, food, hp, sp, attributes, skills, ...                    |
| `condition <slot> <condition> [0\|1]`    | Check a condition like Poisoned                                      |
| `count <item> [amount]`, `give`          | Check or give items                                                  |
| `textshown <text>`, `notextshown [text]` | Check the texts shown since the last `cleartexts`                    |
| `cheat <command>`                        | Execute a cheat console command (e.g. `cheat events 65`)             |

Values printed without an expected value are remembered, so a later check can use `+n`, `-n`
(changed by n) or `+`, `-` (increased or decreased at all).

## Event bits

Events are disabled by event bits. The bit is not given by the event index but by the save index
of the event data (byte 5): bit = (map - 1) * 65 + save index. Events with save index 0 are never
disabled, and one bit can disable several events. The cheat `events <map>` lists all events of a map
with their state, positions and raw data (`[byte1 byte2 byte3 byte4, save index, word6 word8]`).

## Smoke test

`MapEventSmokeTests` enters every placed event of every map (German and English data) and closes everything
which opens afterwards. It takes only a few seconds and finds crashes in events, screens and map changes.

## Conventions

- Name scripts `<Topic>.txt` and `<Topic>_<Case>.txt`, e.g. `Graveyard_English.txt`.
- Start with a comment that says what the script tests. Mark sections with `# --- step` comments.
- Use teleports and cheats only to get to the tested situation, then play it the normal way.
- Scripts which check texts depend on the language. Give the data with `#! data` if it is not German.
