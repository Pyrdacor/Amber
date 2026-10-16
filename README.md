# Amber

**Note: This is still in early development. Many things will change and be added.**

This repository contains libraries to implement games of the Amber trilogy for modern platforms like Windows, Linux, Mac and Android.

Many parts are designed in a way that they can be used or extended for different environments, game data providers, renderers, sound backends and so on.


## Projects

### Amber.Common

Provides basic utilities which all other projects might use.

### Amber.Assets.Common

Interfaces and general classes for working with assets.

### Amber.IO.Common

Interfaces and general classes for serialization and file systems.

### Amber.IO.FileFormats

Common file formats used in Amberstar and Ambermoon like the container files. Also provides readers, writers as well as compression and encoding routines.

### Amber.IO.FileSystem

Implementation of different file systems (e.g. virtual and operating system).

### Amber.Renderer.Common

Interfaces and general classes for rendering.

### Amber.Renderer.Headless

Renderer which only keeps the state of the drawables and never draws anything. It is used to run games without a window or graphics hardware, e.g. for automated tests.


## Amberstar

Currently Amberstar is ported to modern platforms based on this new project so the Amberstar related parts are kept here as well for easier development. At some point they will move to their own repository though.

### Amberstar.GameData

Definitions for Amberstar game data.

### Amberstar.GameData.Legacy

Concrete loaders and implementations for Amberstar game data which are related to the original legacy games for Atari ST, Amiga or DOS.

### Amberstar.Game

The game logic, screens and UI.

### Amberstar.Frontend

Creates the game for a renderer and game data. Also contains the cheats, the headless runner and the scenario scripts which are shared by the window frontend and the tests.

### Amberstar.net

The game with a window, OpenGL rendering and OpenAL audio. Use `--help` for the command line options (e.g. `--script` and `--headless`).

### Amberstar.Tests

Automated tests: loading the game data of all versions in the repository, game logic in a headless game and all scenario scripts in [Tests/Scenarios](Tests/Scenarios/README.md). Run them with `dotnet test Amberstar.Tests`.

