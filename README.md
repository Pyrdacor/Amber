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


## Amberstar

Currently Amberstar is ported to modern platforms based on this new project so the Amberstar related parts are kept here as well for easier development. At some point they will move to their own repository though.

### Amberstar.GameData

Definitions for Amberstar game data.

### Amberstar.GameData.Legacy

Concrete loaders and implementations for Amberstar game data which are related to the original legacy games for Atari ST, Amiga or DOS.

Game data which is loaded from the main executable or other executables is not provided here as those files differ even on the legacy platforms.

### Amberstar.GameData.Atari

Extends `Amberstar.GameData.Legacy` and provides loaders for the executables of the Atari ST.

Those contain things like UI graphics, item graphics, layout graphics or texts.
