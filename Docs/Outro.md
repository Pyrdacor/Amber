# Amberstar outro (EXTRO.68K)

The outro is not part of the released source code. The game and the outro were disassembled.

## Start
- The outro event (type 23, handler $a262 in AMBRSTAR.68K) writes 6 bytes to the command line of its basepage
  (offset 128): per party slot -1 (empty), 1 (race 2, dwarf), 2 (race 3, gnome) or 0 (others). Then the game quits
  and the loader AMBRSTAR.PRG starts EXTRO.68K with the command line "Karsten ist doof !" and these bytes.
- EXTRO.68K is a PRG with a small stub whose data segment is LOB packed. The unpacked program image starts with the code
  (0x0-0x1552) which is the same in all versions. Only the addresses of the data differ, so they are read from the
  operands of the instructions (see `OutroData`).

## Throne room
- Picture (320x200, ST low resolution) with a palette of 17 colors: the 17th replaces color 5 from line 100 (raster interrupt).
- Fade in (7 steps of 5 vertical blanks), the music starts.
- Every 6 vertical blanks (1 step): 2 torches (3 frames each, at 115,59 and 188,59), the chancellor (7 frames at 146,66)
  and the party figures (3 figures with y offsets, positions per slot, x - 8, y + 104, drawn once).
- The chancellor waits 90 steps, stands up (frames 1-6, one per step) and then shows his 3 texts in a text box
  ((20,150)-(300,192) color 15, inside (23,153)-(297,189) color 3, text at (25,155), 6 pixels per character,
  7 pixels per line): the first after 20 steps, the next ones after 101 steps. 100 steps after the last text the room fades out.
- Fade out.

## Balcony
- Picture with 3 palettes: the sky (lines 0-67, later replaced by the red moon palette), the landscape (from line 68,
  color 5 replaced from line 82).
- Script (1 step = 6 vertical blanks): 0 n = wait n steps, 1-3 n = frame of a person (64 pixels wide, 4 frames, positions
  (112,94), (176,102), (256,94)), 4 = next text, 5 = red moon palette and the moon (at 94,20) for 2 steps, 0xff = end.
- The dragon (12 frames with y offsets, starting at x 350, 1 pixel per step) and an eagle (8 frames, x 620,
  2 pixels per step, drawn in front of the dragon) fly to the left. 70 steps after the dragon left the screen, the third moon
  pulses (4 frames, cycle 0,1,2,3,2,1 at 226,22).
- After the last text the space key ends the outro.

## Music
- The music player (a relocatable program at $1fbdc in the German outro) emulates the 4 sample voices of the Amiga:
  it mixes them by software (L960 in the player) and plays them with the DMA sound of the STE or the YM chip.
- It plays a COSO module with samples (TFMX variant): 4 voices with 3 bytes per division (pattern, note transpose,
  signed timbre transpose). The port can't play it yet and plays song 6 of the game instead.

## Port
- `OutroData` loads the data, `OutroScreen` shows it. The palettes fade like the original (fade in: each color channel
  is increased by 1 per step up to its value, fade out: decreased by 1 per step).
- After the outro the game starts again (savegame or new game) instead of returning to the desktop.
