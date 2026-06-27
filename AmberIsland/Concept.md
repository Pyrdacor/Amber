# Maps

Maps have 3 layers:

- Background (always drawn behind player)
- Foreground (always drawn above player)
- Object (drawn behind the upper part of the player but drawn above the lower part of the player)

Each map tile has 3 bytes. Each specifies a tile index for the mentioned layer. First background, then object, then foreground.

If the tile index is 0, there is no tile. For background layer it is also transparent! This allows things like sky maps
where you see the sky beneath. Usually you should not use it on normal maps.

Each map layer uses its own tileset but of course can use the same tileset as other layers as well.

## Tileset

A tileset can have up to 255 tiles but also less.

Each tileset references a specific graphic atlas.

Each tile has the following information:

- Graphic atlas tile index
- Num animation frames (can be 1 for no animation, animation frames must be in a sequence and in the same row in the atlas!)
- Animation type (cyclic or alternating)
- Collision block modes (walking, flying, swimming, player/monster only, use lower layer block mode, etc)
- Tile type (normal, mud, water, sky, chair down/up/right/left, bed down/up/right/left, etc), maybe other terrains as well

## Monsters

Monsters spawn randomly. The map can specify a set of values per monster type:

- Monster (type) Id
- Min Amount (if dropped below, they are immediately respawned ignoring respawn delays)
- Max Amount (spawning stops there)
- Respawn Min Delay (minimum delay before respawning)
- Respawn Max Delay (maximum delay before respawning)

## Map events

There are some potential map events.

- Teleport (includes map changes)
- Text
- Damage
- TileChange
- SpawnMonster
- SpawnNPC
- Delay
- Shake
- ChangeTime
- ChangeWeather
- KillPlayer
- RemoveMonster
- RemoveNPC
- SetQuestBit
- SetQuestValue
- tbd ...

Each event has a trigger:

- Map enter
- Tile enter (x, y)
- Real time (time)
- Game time (gameTime)
- Monster slain (mapMonsterId)
- Map monsters slain (monsterId, count)
- Use item (itemId)
- Interact

Each event can have conditions:

- Quest bit
- Quest value
- Has item (itemId, count)
- Has class (classId)
- Has race (raceId)
- Has hp (percentage, compOperator)
- Has sp (percentage, compOperator)
- Has level
- tbd ...

Map events are stored after the tile layers.
First 4 words, giving the amount of triggers, conditions, actions and events.
Then all triggers, all conditions, all actions and all events follow.

Events just store:

- 1 byte for the number of used conditions
- 1 byte for the number of used actions
- 1 word with the trigger index
- n words for the condition indices (they are ANDed)
- n words for the action indices (they are chained in the order given)
- 1 word for the amount of tiles which use the event
- n words (x and y as byte) for the tile which uses this event

# Names

Names of maps, characters, items, etc are not stored in those object data.
Instead they are provided in text/name containes with an id and text. This
also makes translations much more easy.

# Monsters

Monsters consist of 3 parts:

- Graphic frames
- Monster data
- Animation data

Each is provided by its own container:

"monatlas.aic" contains all monster graphic atlasses
which are combined into one large atlas per map.
To reuse the same atlas for different maps, the file
index is its own index and maps reference the monster
atlas index.

"mondata.aic" contains all monster data files.

"monanim.aic" contains all animations. The same
file index belongs to the same atlas graphic.
Each container file itself is a container where
each file represents one monster state (like idle).
The file index should be the state + 1. Not all
states have to be present. The fallback is:

- Walking -> Idle
- Chasing -> Walking
- Fleeing -> Walking
- Sleeping -> Idle
- Attacking -> Walking
- Casting -> Attacking
- ReceiveDamage -> Idle
- Die -> nothing shown
- Spawn -> nothing shown

So only the idle state (file index 1) is mandatory.
