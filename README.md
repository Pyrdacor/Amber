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


## NuGet packages

The engine projects (Amber.Common, Amber.Assets.Common, Amber.IO.*, Amber.Audio.* and Amber.Renderer.*) are published as NuGet packages by the workflow [publish-nuget.yml](.github/workflows/publish-nuget.yml). Push a tag like `v0.1.0` or start the workflow by hand with a version. It needs the repository secret `NUGET_API_KEY`.

## Amberstar

The remake of Amberstar which is based on this engine is in its own repository: https://github.com/Pyrdacor/Amberstar.net
