namespace Amberstar.GameData.Legacy;

/// <summary>
/// Plays COSO modules with the sound chip of the Atari ST (YM2149).
///
/// This was taken from the music player of the game program (AMBRSTAR.68K, $1a88e to $1b6xx in the German version):
/// the sequencer which writes the sound chip registers 50 times per second (L1aaac), the patterns (L1ac3a), the song
/// start (L1ad8a) and the frequency and volume sequences of the 3 voices (L1af2e). MMME modules (like the music of
/// the game) use another vibrato and portamento than TFMX modules. The sound effect voice and the digitized drums of
/// the player are not used by the game.
/// </summary>
internal sealed class HippelCosoYmSong : ISong
{
	const int SampleRate = 44100;
	const int TicksPerSecond = 50;
	const int BufferSize = SampleRate / 4;
	const double BufferTime = BufferSize * 1000.0 / SampleRate;
	const double ChipClock = 2000000.0;
	const int VoiceCount = 3;
	const int BytesPerDivision = 4 * VoiceCount;
	// the periods of the notes (L1b53a)
	static readonly int[] Periods =
	[
		3822, 3607, 3405, 3214, 3033, 2863, 2702, 2551, 2407, 2272, 2145, 2024,
		1911, 1803, 1702, 1607, 1516, 1431, 1351, 1275, 1203, 1136, 1072, 1012,
		955, 901, 851, 803, 758, 715, 675, 637, 601, 568, 536, 506,
		477, 450, 425, 401, 379, 357, 337, 318, 300, 284, 268, 253,
		238, 225, 212, 200, 189, 178, 168, 159, 150, 142, 134, 126,
		119, 112, 106, 100, 94, 89, 84, 79, 75, 71, 67, 63,
		59, 56, 53, 50, 47, 44, 42, 39, 37, 35, 33, 31,
		29, 28, 26, 25, 23, 22, 21, 19, 18, 17, 16, 15
	];
	// the dummy sequences of the voices before the first note (L1b3e2)
	static readonly byte[] DummySequence = [1, 0, 0, 0, 0, 0, 0, 0xe1];
	// the output levels of the volumes 0 to 15 of the sound chip (logarithmic)
	static readonly double[] Levels =
	[
		0.0, 0.0137, 0.0205, 0.0291, 0.0423, 0.0618, 0.0847, 0.1369,
		0.1691, 0.2647, 0.3527, 0.4499, 0.5704, 0.6873, 0.8482, 1.0
	];

	/// <summary>
	/// The state of a voice (the 52 byte structures L1b43c etc. in the player).
	/// </summary>
	sealed class Voice
	{
		public int Channel; // 46(a0)
		public int DivisionStart; // 0(a0)
		public byte[] VolumeSequenceData = DummySequence;
		public int VolumeSequence; // 4(a0)
		public byte[] FrequencySequenceData = DummySequence;
		public int FrequencySequence; // 8(a0)
		public int Pattern; // 12(a0)
		public int PortamentoValue; // 16(a0)
		public int DivisionOffset; // 20(a0)
		public int VolumePosition; // 22(a0)
		public int FrequencyPosition; // 24(a0)
		public int PatternCounter; // 26(a0)
		public int PatternSpeed; // 27(a0)
		public int Loops; // 28(a0)
		public int Note; // 30(a0)
		public int NoteInfo; // 31(a0)
		public int TimbreTranspose; // 32(a0)
		public int Transpose; // 33(a0)
		public int VolumeCounter = 1; // 34(a0)
		public int VolumeSpeed = 1; // 35(a0)
		public int VolumeDelay; // 36(a0)
		public int FrequencyDelay; // 37(a0)
		public int FrequencyValue; // 38(a0)
		public int VibratoSpeed; // 39(a0)
		public int VibratoDepth; // 40(a0)
		public int VibratoValue; // 41(a0)
		public int VibratoDelay; // 42(a0)
		public int VibratoFlags; // 43(a0)
		public int NoteParameter; // 44(a0)
		public int Noise; // 45(a0)
		public int VolumeValue; // 47(a0)
		public int VolumeReduction; // 48(a0)
		public int MixerMode = 2; // 50(a0)
		public int EnvelopeMode; // 51(a0)
	}

	readonly byte[] module;
	readonly bool mmme;
	readonly int frequencySequences;
	readonly int volumeSequences;
	readonly int patterns;
	readonly int divisions;
	readonly int startDivision;
	readonly int endDivision;
	readonly int speed;
	readonly Voice[] voices = new Voice[VoiceCount];
	int speedCounter;
	int currentSpeed;
	int divisionCount;
	bool playing = false;
	double totalTime = 0.0;
	double sentTime = 0.0;
	double tickSamples = 0.0;
	readonly short[] buffer = new short[BufferSize];

	// the registers of the sound chip
	readonly int[] tonePeriods = new int[VoiceCount];
	readonly int[] volumes = new int[VoiceCount];
	int mixer;
	int noisePeriod;
	int envelopePeriod;
	int envelopeShape;
	// the state of the sound chip
	readonly double[] tonePhases = new double[VoiceCount];
	double noisePhase;
	int noiseShift = 1;
	bool noiseOutput;
	double envelopePhase;
	int envelopeStep;
	bool envelopeHolding;
	double lastInput;
	double lastOutput;

	/// <summary>
	/// The module data starts with "COSO". Songs are 1-based.
	/// </summary>
	public HippelCosoYmSong(byte[] module, int song = 1)
	{
		this.module = module;
		mmme = module.Length > 0x24 && module[0x20] == 'M' && module[0x21] == 'M' && module[0x22] == 'M' && module[0x23] == 'E';
		frequencySequences = Long(4);
		volumeSequences = Long(8);
		patterns = Long(12);
		divisions = Long(16);
		int songEntry = Long(20) + (song - 1) * 6;
		startDivision = Word(songEntry);
		endDivision = Word(songEntry + 2);
		speed = Word(songEntry + 4);
		Reset();
	}

	public bool Paused { get; private set; }

	int Word(int offset) => (module[offset] << 8) | module[offset + 1];

	int Long(int offset) => (Word(offset) << 16) | Word(offset + 2);

	int TableEntry(int table, int index) => Word(table + index * 2);

	public void Play()
	{
		Reset();
		playing = true;
	}

	public void Stop(bool reset = true)
	{
		playing = false;

		if (reset)
			Reset();
	}

	/// <summary>
	/// Starts the song (L1ad8a in the player). The first division is read at once.
	/// </summary>
	public void Reset()
	{
		divisionCount = endDivision - startDivision;

		for (int i = 0; i < VoiceCount; i++)
		{
			var voice = voices[i] = new Voice
			{
				Channel = i,
				DivisionStart = divisions + startDivision * BytesPerDivision + i * 4,
				DivisionOffset = BytesPerDivision,
				Loops = divisionCount
			};
			int division = voice.DivisionStart;
			voice.Pattern = TableEntry(patterns, module[division]);
			voice.Transpose = (sbyte)module[division + 1];
			voice.TimbreTranspose = module[division + 2];
			int effect = module[division + 3];

			if ((effect & 0xf0) == 0xf0)
				voice.VolumeReduction = effect & 0xf;
		}

		speedCounter = 1;
		currentSpeed = speed;
		mixer = 0;
		noisePeriod = 0;
		Array.Clear(tonePeriods);
		Array.Clear(volumes);
		totalTime = 0.0;
		sentTime = 0.0;
		tickSamples = 0.0;
	}

	public void Update(double elapsed, IMusicPlayer musicPlayer)
	{
		if (!playing || Paused)
			return;

		totalTime += elapsed;

		// The samples are generated 1 buffer ahead.
		while (sentTime <= totalTime + BufferTime)
		{
			Render(buffer);
			musicPlayer.SampleData(buffer, false);
			sentTime += BufferTime;
		}
	}

	#region Sequencer

	/// <summary>
	/// One tick (50 times per second): the sequences give the registers, then the patterns advance (L1aafa).
	/// </summary>
	void Tick()
	{
		for (int i = 0; i < VoiceCount; i++)
		{
			var (period, volume) = UpdateSequences(voices[i]);
			tonePeriods[i] = period & 0xfff;
			volumes[i] = volume & 0x1f;
		}

		if (--speedCounter == 0)
		{
			speedCounter = currentSpeed;

			// Only the first voice counts the divisions until the end of the song.
			for (int i = 0; i < VoiceCount; i++)
				UpdatePattern(voices[i], i == 0);
		}
	}

	/// <summary>
	/// The patterns give the notes and the sounds (L1ac3a).
	/// </summary>
	void UpdatePattern(Voice voice, bool countDivisions)
	{
		if (--voice.PatternCounter >= 0)
			return;

		voice.PatternCounter = voice.PatternSpeed;

		while (true)
		{
			int command = module[voice.Pattern++];

			if (command == 0xff)
			{
				// At the end of the song all voices start again.
				if (countDivisions && --voice.Loops < 0)
				{
					voice.Loops = divisionCount;

					foreach (var other in voices)
						other.DivisionOffset = 0;
				}

				int division = voice.DivisionStart + voice.DivisionOffset;
				int pattern = module[division];
				voice.Transpose = (sbyte)module[division + 1];
				voice.TimbreTranspose = module[division + 2];
				int effect = module[division + 3];

				if ((effect & 0xf0) == 0xf0)
					voice.VolumeReduction = effect & 0xf;
				else if ((effect & 0xf0) == 0xe0)
					currentSpeed = effect & 0xf;

				voice.Pattern = TableEntry(patterns, pattern);
				voice.DivisionOffset += BytesPerDivision;
				continue;
			}

			if (command == 0xfe)
			{
				voice.PatternSpeed = voice.PatternCounter = module[voice.Pattern++];
				continue;
			}

			if (command == 0xfd)
			{
				voice.PatternSpeed = voice.PatternCounter = module[voice.Pattern++];
				return;
			}

			// note
			voice.Note = command;
			voice.NoteInfo = module[voice.Pattern++];

			if ((voice.NoteInfo & 0xe0) != 0)
				voice.NoteParameter = module[voice.Pattern++];

			voice.PortamentoValue = 0;

			// Notes with bit 7 only change the note.
			if ((command & 0x80) != 0)
				return;

			int timbre = ((voice.NoteInfo & 0x1f) + voice.TimbreTranspose) & 0xff;
			int volumeSequence = TableEntry(volumeSequences, timbre);
			voice.VolumePosition = 0;
			voice.VolumeCounter = voice.VolumeSpeed = module[volumeSequence];
			int frequencySequence = module[volumeSequence + 1];
			voice.VibratoSpeed = module[volumeSequence + 2];
			voice.VibratoFlags = 0x40;
			voice.VibratoDepth = voice.VibratoValue = module[volumeSequence + 3];
			voice.VibratoDelay = module[volumeSequence + 4];
			voice.VolumeSequenceData = module;
			voice.VolumeSequence = volumeSequence + 5;

			if ((voice.NoteInfo & 0x40) != 0)
				frequencySequence = voice.NoteParameter;

			voice.FrequencySequenceData = module;
			voice.FrequencySequence = TableEntry(frequencySequences, frequencySequence);
			voice.FrequencyPosition = 0;
			voice.FrequencyDelay = 0;
			voice.VolumeDelay = 0;
			return;
		}
	}

	/// <summary>
	/// The frequency and volume sequences of a voice (L1af2e). Returns the period and the volume.
	/// </summary>
	(int Period, int Volume) UpdateSequences(Voice voice)
	{
		voice.Noise = 0;
		UpdateFrequencySequence(voice);
		UpdateVolumeSequence(voice);

		return CalculatePeriodAndVolume(voice);
	}

	void UpdateFrequencySequence(Voice voice)
	{
		while (true)
		{
			if (voice.FrequencyDelay != 0)
			{
				voice.FrequencyDelay--;
				return;
			}

			var data = voice.FrequencySequenceData;
			int position = voice.FrequencySequence + voice.FrequencyPosition;
			int command = data[position];

			if (command < 0xe0)
			{
				voice.FrequencyValue = command;
				voice.FrequencyPosition++;
				return;
			}

			switch (command)
			{
				case 0xe0: // loop
					voice.FrequencyPosition = data[position + 1];
					continue;
				case 0xe1: // hold the last value
					voice.FrequencyValue = data[position - 1];
					return;
				case 0xe2: // restart the volume sequence
					voice.VolumePosition = 0;
					voice.VolumeCounter = 1;
					voice.FrequencyPosition++;
					continue;
				case 0xe3: // vibrato
					voice.VibratoSpeed = data[position + 1];
					voice.VibratoDepth = data[position + 2];
					voice.FrequencyPosition += 3;
					continue;
				case 0xe4: // tone and noise with a noise period
					voice.MixerMode = 0;
					voice.Noise = data[position + 1];
					voice.FrequencyPosition += 2;
					continue;
				case 0xe5: // noise only
					voice.MixerMode = 1;
					voice.FrequencyPosition++;
					continue;
				case 0xe6: // tone only
					voice.MixerMode = 2;
					voice.FrequencyPosition++;
					continue;
				case 0xe7: // another frequency sequence
					// Note: The original uses a wrong register for the table here.
					voice.FrequencySequence = TableEntry(frequencySequences, data[position + 1]);
					voice.FrequencyPosition = 0;
					continue;
				case 0xe8: // wait
					voice.FrequencyDelay = data[position + 1];
					voice.FrequencyPosition += 2;
					continue;
				case 0xe9: // envelope (shape 10) with a period
					envelopePeriod = data[position + 1];
					SetEnvelopeShape(0x0a);
					voice.FrequencyPosition += 2;
					continue;
				case 0xea: // portamento
					voice.NoteInfo = 0x20;
					voice.NoteParameter = data[position + 1];
					voice.FrequencyPosition += 2;
					continue;
				case 0xeb: // envelope with the note period (buzzer)
					voice.EnvelopeMode = data[position + 1];
					voice.FrequencyPosition += 2;
					continue;
				case 0xec: // digitized drum (not used)
					voice.FrequencyPosition += 4;
					continue;
				case 0xed: // stop the digitized drum (not used)
					voice.FrequencyPosition++;
					continue;
				default:
					voice.FrequencyPosition++;
					continue;
			}
		}
	}

	void UpdateVolumeSequence(Voice voice)
	{
		while (true)
		{
			if (voice.VolumeDelay != 0)
			{
				voice.VolumeDelay--;
				return;
			}

			if (--voice.VolumeCounter != 0)
				return;

			voice.VolumeCounter = voice.VolumeSpeed;

			while (true)
			{
				var data = voice.VolumeSequenceData;
				int position = voice.VolumeSequence + voice.VolumePosition;
				int command = data[position];

				if (command < 0xe0)
				{
					voice.VolumeValue = command;
					voice.VolumePosition++;
					return;
				}

				if (command == 0xe1) // hold the last value
				{
					voice.VolumeValue = data[position - 1];
					return;
				}

				if (command == 0xe8) // wait
				{
					voice.VolumeDelay = data[position + 1];
					voice.VolumePosition += 2;
					break;
				}

				// E0 and E2-E7: loop (relative to the 5 bytes header)
				voice.VolumePosition = data[position + 1] - 5;
			}
		}
	}

	/// <summary>
	/// The note, the mixer, the vibrato, the portamento and the volume (L1b1ae).
	/// </summary>
	(int Period, int Volume) CalculatePeriodAndVolume(Voice voice)
	{
		int note = voice.FrequencyValue;

		// Values with bit 7 are fixed notes.
		if ((note & 0x80) == 0)
			note += voice.Note + voice.Transpose;

		note &= 0x7f;
		int noteIndex = note * 2;
		int period = note < Periods.Length ? Periods[note] : 0;
		int toneBit = 1 << voice.Channel;
		int noiseBit = 8 << voice.Channel;

		switch (voice.MixerMode)
		{
			case 0: // tone and noise
				mixer &= ~(toneBit | noiseBit);
				break;
			case 1: // noise with the note
				mixer = (mixer | toneBit) & ~noiseBit;
				voice.Noise = voice.Note;

				if ((voice.FrequencyValue & 0x80) != 0)
					voice.Noise = voice.FrequencyValue & 0x7f;
				else
					voice.Noise = (voice.Noise + voice.FrequencyValue) & 0xff;
				break;
			default: // tone
				mixer = (mixer & ~toneBit) | noiseBit;
				break;
		}

		if (voice.Noise != 0)
			noisePeriod = ~voice.Noise & 0x1f;

		if (mmme)
			period = MmmeVibratoAndPortamento(voice, period);
		else
			period = TfmxVibratoAndPortamento(voice, period, noteIndex);

		int volume = (sbyte)(voice.VolumeValue - voice.VolumeReduction);

		if (volume < 0)
			volume = 0;

		if (mmme && voice.EnvelopeMode != 0)
		{
			int mode = voice.EnvelopeMode;
			int tonePeriod = period & 0xffff;

			if ((mode & 0x80) != 0)
			{
				envelopePeriod &= 0xff;
				SetEnvelopeShape(0x0a);
			}

			if ((mode & 0x02) != 0)
				period = 0;

			int envelope = (mode & 0x08) != 0 ? tonePeriod >> 3 : tonePeriod >> 4;
			envelopePeriod = (envelopePeriod & 0xff00) | (envelope & 0xff);
		}

		return (period, volume);
	}

	static int MmmeVibratoAndPortamento(Voice voice, int period)
	{
		if (voice.VibratoDelay != 0)
		{
			voice.VibratoDelay--;
		}
		else
		{
			int value = voice.VibratoValue;
			int maximum = voice.VibratoDepth * 2;
			int flags = voice.VibratoFlags;

			if ((flags & 0x20) != 0)
			{
				value -= voice.VibratoSpeed;

				if (value < 0)
				{
					value = 0;
					flags ^= 0x20;
				}
			}
			else
			{
				value += voice.VibratoSpeed;

				if (value > maximum)
				{
					value = maximum;
					flags ^= 0x20;
				}
			}

			voice.VibratoValue = value & 0xff;
			voice.VibratoFlags = flags;
			period += (short)(((value - voice.VibratoDepth) * (short)period) >> 10);
		}

		if ((voice.NoteInfo & 0x20) != 0)
		{
			voice.PortamentoValue += (sbyte)voice.NoteParameter;
			period -= (short)((((short)voice.PortamentoValue * (short)period) >> 8) >> 2);
		}

		return period;
	}

	static int TfmxVibratoAndPortamento(Voice voice, int period, int noteIndex)
	{
		if (voice.VibratoDelay != 0)
		{
			voice.VibratoDelay--;
		}
		else
		{
			int flags = voice.VibratoFlags;
			int depth = voice.VibratoDepth;
			depth = (depth & 0x80) != 0 ? depth & 0x7f : (depth * 2) & 0xff;
			int value = voice.VibratoValue;

			// With bit 7 of the flags the vibrato changes only every second tick.
			if ((flags & 0x80) == 0 || (flags & 0x01) == 0)
			{
				if ((flags & 0x20) == 0)
				{
					value -= voice.VibratoSpeed;

					if (value < 0)
					{
						flags |= 0x20;
						value = 0;
					}
				}
				else
				{
					value += voice.VibratoSpeed;

					if (value >= depth)
					{
						flags &= ~0x20;
						value = depth;
					}
				}

				voice.VibratoValue = value & 0xff;
			}

			int offset = voice.VibratoValue - (depth >> 1);

			// Lower notes get a bigger vibrato (doubled per octave, L1b378).
			int octave = (noteIndex & 0xff) + 0xa0;

			if (octave < 0x100)
			{
				do
				{
					offset *= 2;
					octave += 0x18;
				}
				while (octave < 0x100);
			}

			period += offset;
			voice.VibratoFlags = flags ^ 0x01;
		}

		if ((voice.NoteInfo & 0x20) != 0)
		{
			voice.PortamentoValue += (sbyte)voice.NoteParameter << 12;
			period -= voice.PortamentoValue >> 16;
		}

		return period;
	}

	#endregion

	#region Sound chip

	/// <summary>
	/// Writing the shape register restarts the envelope.
	/// </summary>
	void SetEnvelopeShape(int shape)
	{
		envelopeShape = shape & 0xf;
		envelopeStep = 0;
		envelopePhase = 0.0;
		envelopeHolding = false;
	}

	/// <summary>
	/// The envelope level (0-15) of the 16 steps of a cycle and the 4 bits of the shape (continue, attack, alternate, hold).
	/// </summary>
	int EnvelopeLevel()
	{
		bool attack = (envelopeShape & 0x04) != 0;
		int cycle = envelopeStep / 16;
		int step = envelopeStep % 16;

		if (envelopeHolding)
		{
			bool continues = (envelopeShape & 0x08) != 0;

			if (!continues)
				return 0;

			bool alternate = (envelopeShape & 0x02) != 0;
			bool hold = (envelopeShape & 0x01) != 0;
			// the last level of the first cycle, inverted when alternating
			bool high = attack;

			if (hold && alternate)
				high = !high;

			return high ? 15 : 0;
		}

		bool rising = attack;

		if ((envelopeShape & 0x02) != 0 && (cycle & 1) != 0)
			rising = !rising;

		return rising ? step : 15 - step;
	}

	void AdvanceEnvelope(double steps)
	{
		if (envelopeHolding)
			return;

		envelopePhase += steps;

		while (envelopePhase >= 1.0)
		{
			envelopePhase -= 1.0;
			envelopeStep++;

			// After the first cycle the envelope holds unless it continues without the hold bit.
			if (envelopeStep >= 16 && ((envelopeShape & 0x08) == 0 || (envelopeShape & 0x01) != 0))
			{
				envelopeHolding = true;
				return;
			}
		}
	}

	void Render(short[] output)
	{
		double samplesPerTick = (double)SampleRate / TicksPerSecond;

		for (int i = 0; i < output.Length; i++)
		{
			if (tickSamples <= 0.0)
			{
				Tick();
				tickSamples += samplesPerTick;
			}

			tickSamples--;

			// noise (17 bit shift register)
			noisePhase += ChipClock / (16.0 * Math.Max(1, noisePeriod)) / SampleRate;

			while (noisePhase >= 1.0)
			{
				noisePhase -= 1.0;
				int bit = (noiseShift ^ (noiseShift >> 3)) & 1;
				noiseShift = (noiseShift >> 1) | (bit << 16);
				noiseOutput = (noiseShift & 1) != 0;
			}

			AdvanceEnvelope(ChipClock / (16.0 * Math.Max(1, envelopePeriod)) / SampleRate);
			int envelopeLevel = EnvelopeLevel();
			double sum = 0.0;

			for (int voice = 0; voice < VoiceCount; voice++)
			{
				tonePhases[voice] += ChipClock / (16.0 * Math.Max(1, tonePeriods[voice])) / SampleRate;
				tonePhases[voice] -= Math.Floor(tonePhases[voice]);
				bool tone = tonePhases[voice] < 0.5 || (mixer & (1 << voice)) != 0;
				bool noise = noiseOutput || (mixer & (8 << voice)) != 0;

				if (tone && noise)
				{
					int volume = volumes[voice];
					sum += Levels[(volume & 0x10) != 0 ? envelopeLevel : volume & 0xf];
				}
			}

			// remove the constant part of the output
			double filtered = sum - lastInput + 0.995 * lastOutput;
			lastInput = sum;
			lastOutput = filtered;
			output[i] = (short)Math.Clamp(filtered * 10000.0, short.MinValue, short.MaxValue);
		}
	}

	#endregion
}
