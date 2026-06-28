At some point we want to be able to (re-)generate the whole
game data (containers) automatically. For this purpose we
have some build scripts.


# Sprite containers

For example all monster sprites, NPCs sprites, etc are in their
own file container. We need to pack those containers.

```
// The sections starting with # specify what type of
// build operation is done. Every build operation has
// some distinct workflow. The workflow for Container
// will just take all files matching the pattern on the
// right and create the container file on the left from
// it. Note that the folder "<projectDir>/assets/" is
// always the working directory or root folder for all
// these operations.
# Container
"mon_sprites.aic" <- "monsters/*.aispr"
```

File names should have the form "001_name.aispr" where 001 is
the file index (e.g. monster sprite index) and name is just some
descriptive text to see what it is (will be ignored in container).


# Map-related sprites

For each map a texture atlas and a palette atlas is created
per layer (e.g. monsters, NPCs, etc). The graphics are still
in the sprite containers, but we have the sprite sheets which
store per map which sprites are used (via index) and which
palette indices can be used for that sprite. It also stores
a combined palette which can be used directly as the palette
atlas.

```
# MapSpriteSheet
// @ prefix means temporary file during build (deleted afterwards).
// The thing in [] is a container file selector (by index).
// So here files with index 2, 5, 6, 7 and 9 are considered.
// Dependent containers are always built first.
// Commas (outside the []) separate arguments for the build
// operation. MapSpriteSheet needs 2 arguments. The second
// arguments is a dictionary. It assigns the possible palette
// indices to the sprite index.
// Referencing temp files do not require the @ symbol!
@"mon_ssheet.aic" <- "mon_sprites.aic"[2,5-7,9], {2:[0-3], 5:0, 6-9: 1}
// Note: The mon_ssheet.aic should not be temporary actually.
// It is only here to demonstrate that it should be possible!
```