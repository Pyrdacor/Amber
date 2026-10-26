# Music (COSO modules by Jochen Hippel)

The players were taken from the disassembled original programs. Both use the same engine: divisions (one row per
pattern step of all voices), patterns (notes and sounds), sounds (a volume sequence with a header for the frequency
sequence and the vibrato) and frequency sequences. They differ in the module variant and in the sound output.

## Atari ST (AMBRSTAR.68K, $1a88e, `HippelCosoYmSong`)
- 3 voices with 4 bytes per division row: pattern, note transpose, sound transpose, effect (Fx = volume reduction x,
  Ex = speed x). The first voice counts the rows and restarts all voices after the last row of the song.
- Frequency sequence commands: E0 loop, E1 hold, E2 restart the volume sequence, E3 vibrato, E4 tone and noise with a noise
  period, E5 noise only, E6 tone only, E7 another sequence, E8 wait, E9 envelope (shape 10) with a period, EA portamento,
  EB envelope with the tone period (buzzer), EC/ED digitized drums (not used by the game).
- Volume sequence commands: E0 and E2-E7 loop, E1 hold, E8 wait. Volumes with bit 4 use the envelope.
- MMME modules (the game's music) use another vibrato and portamento than TFMX modules.
- 50 ticks per second, the speed counter advances the patterns. The YM periods: 2 MHz / (16 * period).
- The songs 1-19 of the game map to 17 modules by a table ($316dc, see `ProgramData.FindSongTable`).

## Amiga (the music player of EXTRO.68K, `HippelCosoPaulaSong`)
- The outro emulates the 4 sample voices of the Amiga by software (it mixes them for the DMA sound of the STE).
- TFMX modules with samples: 4 voices with 3 bytes per division row (pattern, note transpose, sound transpose or effect
  when above $7f: Fx volume 6 * (16 - x), 8x stop, Ex speed).
- Frequency sequence commands: E0 loop, E1 hold, E2 sample with restart, E3 vibrato, E4 sample without restart, E5 sample
  with a moving loop (sweep), E6 change the sweep, E7 sample with restart if it changed, E8 wait, E9 sample of a sample bank.
- Volume sequence commands: E0 loop, E1-E7 hold, E8 wait. Volumes 0-63 (scaled by the division volume 0-100).
- A started sample plays once, then its loop is played (the voice registers are set to the loop after the start, like
  on the Amiga). Samples shorter than 220 words are silent. Periods: 3546895 Hz / period.
