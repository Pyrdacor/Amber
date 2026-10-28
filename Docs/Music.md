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

## Amiga (the music player of EXTRO.68K, `HippelPaulaSong`)
- The outro emulates the 4 sample voices of the Amiga by software (it mixes them for the DMA sound of the STE).
- COSO modules with samples: 4 voices with 3 bytes per division row (pattern, note transpose, sound transpose or effect
  when above $7f: Fx volume 6 * (16 - x), 8x stop, Ex speed).
- The modules of the Amiga version of the game have no samples. They all use SAMPLEDA.IMG (8 bit signed), the sample
  offsets are relative to the end of the module.
- Frequency sequence commands: E0 loop, E1 hold, E2 sample with restart, E3 vibrato, E4 sample without restart, E5 sample
  with a moving loop (sweep), E6 change the sweep, E7 sample with restart if it changed, E8 wait, E9 sample of a sample bank.
- Volume sequence commands: E0 loop, E1-E7 hold, E8 wait. Volumes 0-63 (scaled by the division volume 0-100).
- A started sample plays once, then its loop is played (the voice registers are set to the loop after the start, like
  on the Amiga). The emulation of the outro does not play samples shorter than 220 words, the Amiga plays all samples
  (the leading voices of the game's music are short waveforms). Periods: 3546895 Hz / period.

## Amiga outro (EXTRO.UDO, "Player by Jochen Hippel 1990", `HippelPaulaSong`)
- TFMX modules with 7 voices. Header: "TFMX", the numbers (minus 1) of the frequency sequences, volume sequences,
  patterns and divisions, the pattern size, a word, the songs and the samples. Then the sequences (64 bytes each),
  the patterns (fixed size), the divisions (4 bytes per voice: pattern, note transpose, sound transpose, effect with
  Fx volume, 8x stop, Ex speed and Dx the mixing rate), the songs (first and last division, a word and the speed),
  the sample headers (30 bytes: the name, offset, length, a word, loop offset and loop length) and the samples.
- The patterns have rows of 2 bytes (note and info). A note of 0 is a rest, 1 ends the pattern. The parameter of a note
  is the byte in front of it. Every row takes one speed step.
- The sequences work like in the COSO player. The frequency sequences have the command EA (the volume of the division is
  reduced by a random part of the value for one tick).
- The first division has the settings of the mixer. The voices 1-3 use the sound chip, the voices 4-7 are mixed by
  software (volume tables, the sum is clipped) and played by the 4th voice. The mixer plays samples with up to 16 words
  as silence. The patterns of the mixed voices are read before their sequences, the others after them.
