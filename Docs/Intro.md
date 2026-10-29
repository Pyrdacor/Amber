# Amberstar intro (THALION.68K and INTRO.68K)

The intro programs are not part of the released source code. They were disassembled.

## Start
- The loader AMBRSTAR.PRG starts THALION.68K (the logo of Thalion), then INTRO.68K (not on "abnormal" STs),
  then the game AMBRSTAR.68K. When the game returns $2505, EXTRO.68K is started.
- INTRO.68K checks its command line ("Karsten ist doof !", like the outro). The data segment is LOB packed: the
  unpacked program has the code (0-$822), the data and the relocation table at the end. The addresses are from 0
  and the code is the same in all versions, so the data addresses are read from the operands (see `IntroData`).

## Intro (INTRO.68K)
- The title picture ($87c, 32000 bytes) is followed by the palettes of its 199 lines (3 x 16 colors per line).
  The VBL routine ($57c or $68e for 60 Hz) synchronises with the video counter and writes the palette 3 times
  in each line (like Spectrum 512). The color of a pixel is one of the 48 colors of its line, depending on its
  color and x position: x1 = 10 * color + (odd: -5, even: +1), the first palette before x1, the second until
  x1 + 160, then the third. The first line is black.
- 6 darker copies of the palettes are made (each channel decreased by 1 per copy). The title fades in over 24 vertical
  blanks (a step every 4) and waits for the space key, then it fades out the same way.
- The background ($d09c, a NEOchrome picture with its palette at $d020) fades in from black (7 steps of 3 vertical
  blanks, each channel is increased by 1 per step). The music starts (a COSO module with samples, played by the
  same emulation of the Amiga voices as in the outro). The logo ($14d9c, 320x36, masked) is drawn line by line at
  line 17 (one line per vertical blank).
- The credits ($4a1ba): pages which end with 0, lines end with 13, the list ends with an empty page. A page is drawn
  into the background with the font ($1641c, 16x20 pixels per character from $25, masked) at (32,75) with 22 pixels
  per line. Then a transition shows it (chosen by a random number, bit 5): 5 bands of 22 lines which show one more
  line per vertical blank, or rows of 4 lines where the upper 2 lines are shown from the left and the lower 2 lines
  from the right (16 pixels per vertical blank). Each page stays for 251 vertical blanks, then the next one follows
  (endless). The space key ends the intro: the palette fades out (7 steps of 3 vertical blanks).

## Port
- `IntroData` loads the data (the title pictures as RGBA for each fading step, the pages with their text),
  `IntroScreen` shows it. The intro and the outro use their own layer with their own palettes.
- The intro is not shown for scenario scripts.
