namespace Amberstar.GameData.Legacy;

/// <summary>
/// Plays the music modules of Jochen Hippel with samples like the Amiga does with its sound chip Paula.
/// There are 2 formats:
///
/// - COSO modules (a TFMX variant with 4 voices and tables of offsets): the music of the Amiga version of the game and
///   the outro of the Atari ST versions. This was taken from the music player of the outro (EXTRO.68K) which emulates
///   the 4 sample voices of the Amiga on the Atari ST: the sequencer (L9e88 in the player), the patterns (L9fbc),
///   the frequency and volume sequences of each voice (La22e), the DMA of the voices (L958c) and the mixing
///   (L94f4 and L9c6).
/// - TFMX modules with 7 voices: the outro of the Amiga versions (EXTRO.UDO, "Player by Jochen Hippel 1990").
///   The sequences have 64 bytes, the patterns have a fixed size with rows of 2 bytes (note and info, the parameter
///   is the byte in front of the note) and the divisions have 4 bytes per voice (pattern, transpose, timbre transpose
///   and effect). The sequences are handled like in the COSO player (L201b8). The voices 1 to 3 use the sound chip,
///   the voices 4 to 7 are mixed by software into the 4th voice of the sound chip (L27122 and L270ec, with volume
///   tables and the sum is clipped).
/// </summary>
internal sealed class HippelPaulaSong : ISong
{
	const int SampleRate = 44100;
	const int TicksPerSecond = 50;
	const int BufferSize = SampleRate / 4;
	const double BufferTime = BufferSize * 1000.0 / SampleRate;
	const double PaulaClock = 3546895.0;
	const int CosoVoiceCount = 4;
	const int TfmxVoiceCount = 7;
	// the voices from this one are mixed by software (TFMX)
	const int TfmxFirstMixedVoice = 3;
	const int TfmxSequenceSize = 64;
	const int TfmxBytesPerVoice = 4;
	const int TfmxSampleHeaderSize = 30;
	// the name of the sample is in front of its data (offset, length, ?, loop offset, loop length)
	const int TfmxSampleHeaderDataOffset = 18;
	// the periods of the TFMX player end after these notes (the next ones are 0)
	const int TfmxPeriodCount = 72;
	// The emulation of the outro does not play samples shorter than this (in words, L95fa).
	const int EmulatedMinSampleLength = 0xdc;
	// The software mixer plays samples of up to 16 words as silence (L27188).
	const int MixedMinSampleLength = 0x11;
	const int MaxVolume = 63;
	// the periods of the notes (La90a), the original reads the module header for higher notes
	static readonly int[] Periods =
	[
		1712, 1616, 1524, 1440, 1356, 1280, 1208, 1140, 1076, 1016, 960, 906,
		856, 808, 762, 720, 678, 640, 604, 570, 538, 508, 480, 453,
		428, 404, 381, 360, 339, 320, 302, 285, 269, 254, 240, 226,
		214, 202, 190, 180, 170, 160, 151, 143, 135, 127, 120, 113,
		113, 113, 113, 113, 113, 113, 113, 113, 113, 113, 113, 113,
		3424, 3232, 3048, 2880, 2712, 2560, 2416, 2280, 2152, 2032, 1920, 1812
	];
	// the dummy sequences of the voices before the first note (La6f4)
	static readonly byte[] DummySequence = [1, 0, 0, 0, 0, 0, 0, 0xe1];

	/// <summary>
	/// The registers of a voice of the sound chip (pointer and length in words) and what is played.
	/// </summary>
	sealed class PaulaVoice
	{
		public byte[] Data = [];
		public int Pointer;
		public int Length;
		public int Period;
		public int Volume;
		// the buffer which is played
		public byte[] PlayData = [];
		public int PlayPointer;
		public int PlayLength;
		public double Position;
		public bool NewStart = true;
		public double Increment;
		public int PlayVolume;
		public int MinSampleLength = 1;
	}

	/// <summary>
	/// The state of a voice of the sequencer (the 84 byte structures La718 etc. in the player).
	/// </summary>
	sealed class Voice
	{
		public int DmaBit;
		// voices which are mixed by software (TFMX)
		public bool Mixed;
		public int PatternPosition; // 60(a0) in the TFMX player
		public int RandomVolume; // 83(a0) in the TFMX player
		public int RandomVolumeValue; // 84(a0)
		public PaulaVoice Paula = new();
		public int DivisionPosition; // 4(a0) + 58(a0)
		public int DivisionStart;
		public int DivisionEnd;
		public byte[] VolumeSequenceData = DummySequence;
		public int VolumeSequence; // 16(a0)
		public byte[] FrequencySequenceData = DummySequence;
		public int FrequencySequence; // 20(a0)
		public int Pattern; // 24(a0)
		public bool PatternEnd = true;
		public int PortamentoValue; // 28(a0)
		public int SweepStart; // 30(a0)
		public int SweepEnd; // 34(a0)
		public int SweepPosition; // 38(a0)
		public int SweepStep; // 40(a0)
		public int SweepLength; // 42(a0)
		public bool SweepStarted; // 44(a0)
		public int Period; // 46(a0)
		public int SweepCounter; // 48(a0)
		public int SweepSpeed; // 49(a0)
		public bool Sweep; // 50(a0)
		public bool SweepEnded; // 51(a0)
		public int LoopLength; // 52(a0)
		public int LoopPointer; // 12(a0)
		public int FrequencyPosition; // 54(a0)
		public int VolumePosition; // 56(a0)
		public int PatternCounter = -1; // 60(a0)
		public int PatternSpeed; // 61(a0)
		public int VibratoValue; // 64(a0)
		public int Note; // 65(a0)
		public int NoteInfo; // 66(a0)
		public int TimbreTranspose; // 67(a0)
		public int VolumeCounter = 1; // 68(a0)
		public int VolumeSpeed = 1; // 69(a0)
		public int VolumeDelay; // 70(a0)
		public int FrequencyDelay; // 71(a0)
		public int VibratoSpeed; // 72(a0)
		public int VibratoDepth; // 73(a0)
		public int VibratoDelay; // 74(a0)
		public int NoteParameter; // 75(a0)
		public int Sample = 0xff; // 76(a0)
		public int FrequencyValue; // 77(a0)
		public int Transpose; // 78(a0)
		public int VolumeValue; // 79(a0)
		public int VibratoFlags; // 80(a0)
		public int DivisionVolume = 100; // 81(a0)
	}

	readonly byte[] module;
	readonly bool tfmx;
	readonly int minSampleLength;
	readonly int bytesPerDivision;
	readonly int bytesPerVoice;
	readonly int patternSize;
	readonly bool clipMixedVoices;
	readonly int frequencySequences;
	readonly int volumeSequences;
	readonly int patterns;
	readonly int divisions;
	readonly int sampleHeaders;
	readonly int sampleData;
	readonly int startDivision;
	readonly int endDivision;
	readonly int speed;
	readonly Voice[] voices;
	int randomSeed = 0;
	int dmaMask = 0;
	int restartMask = 0;
	int speedCounter = 1;
	int currentSpeed;
	bool stopped = false;
	bool playing = false;
	double totalTime = 0.0;
	double sentTime = 0.0;
	double tickSamples = 0.0;
	readonly short[] buffer = new short[BufferSize];

	/// <summary>
	/// The module data starts with "COSO" or "TFMX". Songs are 1-based.
	///
	/// The Amiga plays samples of any length. The emulation of the outro on the Atari ST does not play short
	/// samples (like the waveforms of the leading voices of the Amiga version), so this is only used for it.
	/// </summary>
	public HippelPaulaSong(byte[] module, int song = 1, bool emulated = true)
	{
		this.module = module;
		tfmx = module[0] == 'T';
		minSampleLength = emulated ? EmulatedMinSampleLength : 1;
		voices = new Voice[tfmx ? TfmxVoiceCount : CosoVoiceCount];

		if (tfmx)
		{
			// the numbers of the sequences, patterns and divisions (minus 1), the pattern size, songs and samples (L1fd0c)
			patternSize = Word(12);
			bytesPerVoice = TfmxBytesPerVoice;
			bytesPerDivision = TfmxBytesPerVoice * TfmxVoiceCount;
			frequencySequences = 0x20;
			volumeSequences = frequencySequences + (Word(4) + 1) * TfmxSequenceSize;
			patterns = volumeSequences + (Word(6) + 1) * TfmxSequenceSize;
			divisions = patterns + (Word(8) + 1) * patternSize;
			int songs = divisions + (Word(10) + 1) * bytesPerDivision;
			sampleHeaders = songs + (Word(16) + 1) * 8;
			sampleData = sampleHeaders + Word(18) * TfmxSampleHeaderSize;
			// the songs have the first and the last division and the speed
			int songEntry = songs + (song - 1) * 8;
			startDivision = Word(songEntry);
			endDivision = Word(songEntry + 2);
			speed = Word(songEntry + 6);
			// The first division has the settings of the mixer, its last byte decides if the sum of the mixed voices
			// is clipped or divided by 4 (L278c0 and L271e4).
			clipMixedVoices = module[divisions + 3] != 0;
		}
		else
		{
			bytesPerVoice = 3;
			bytesPerDivision = 3 * CosoVoiceCount;
			frequencySequences = Long(4);
			volumeSequences = Long(8);
			patterns = Long(12);
			divisions = Long(16);
			int songs = Long(20);
			sampleHeaders = Long(24);
			sampleData = Long(28);
			int songEntry = songs + (song - 1) * 6;
			startDivision = Word(songEntry);
			endDivision = Word(songEntry + 2);
			speed = Word(songEntry + 4);
		}

		Reset();
	}

	public bool Paused { get; private set; }

	int Word(int offset) => (module[offset] << 8) | module[offset + 1];

	int Long(int offset) => (Word(offset) << 16) | Word(offset + 2);

	int TableEntry(int table, int index) => Word(table + index * 2);

	int FrequencySequence(int index) => tfmx ? frequencySequences + index * TfmxSequenceSize : TableEntry(frequencySequences, index);

	int VolumeSequence(int index) => tfmx ? volumeSequences + index * TfmxSequenceSize : TableEntry(volumeSequences, index);

	int Pattern(int index) => tfmx ? patterns + index * patternSize : TableEntry(patterns, index);

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
	/// Initializes the song (La12e in the player).
	/// </summary>
	public void Reset()
	{
		for (int i = 0; i < voices.Length; i++)
		{
			bool mixed = tfmx && i >= TfmxFirstMixedVoice;
			voices[i] = new Voice
			{
				DmaBit = 1 << i,
				Mixed = mixed,
				DivisionStart = divisions + startDivision * bytesPerDivision + i * bytesPerVoice,
				DivisionEnd = divisions + (endDivision + 1) * bytesPerDivision + i * bytesPerVoice,
				// TFMX: the end of the pattern reads the first division
				PatternPosition = patternSize,
			};
			voices[i].DivisionPosition = voices[i].DivisionStart;
			voices[i].Paula.MinSampleLength = mixed ? MixedMinSampleLength : minSampleLength;
		}

		randomSeed = 0;
		dmaMask = 0;
		speedCounter = 1;
		currentSpeed = speed;
		stopped = false;
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

	#region Mixing

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
			int sum = 0;
			int mixedSum = 0;

			foreach (var voice in voices)
			{
				if (voice.Mixed)
					mixedSum += NextSample(voice.Paula);
				else
					sum += NextSample(voice.Paula);
			}

			// TFMX: the mixed voices are played by the 4th voice of the sound chip
			if (tfmx)
				sum += clipMixedVoices ? Math.Clamp(mixedSum, sbyte.MinValue, sbyte.MaxValue) : mixedSum / 4;

			// 4 voices with up to +-128 each
			output[i] = (short)Math.Clamp(sum * 48, short.MinValue, short.MaxValue);
		}
	}

	int NextSample(PaulaVoice paula)
	{
		if (paula.PlayLength == 0 || paula.Increment == 0.0)
			return 0;

		int index = paula.PlayPointer + (int)paula.Position;
		int sample = index < paula.PlayData.Length ? (sbyte)paula.PlayData[index] : 0;
		paula.Position += paula.Increment;

		// At the end of the buffer the registers are read again (L8c2).
		while (paula.Position >= paula.PlayLength)
		{
			paula.Position -= paula.PlayLength;
			SetPlayBuffer(paula);

			if (paula.PlayLength == 0)
				break;
		}

		return sample * paula.PlayVolume / MaxVolume;
	}

	void SetPlayBuffer(PaulaVoice paula)
	{
		int length = paula.Length & 0x3fff;

		if (length < paula.MinSampleLength)
		{
			paula.PlayLength = 0;
			return;
		}

		paula.PlayData = paula.Data;
		paula.PlayPointer = paula.Pointer;
		paula.PlayLength = length * 2;
	}

	/// <summary>
	/// Voices without DMA are silent. When the DMA is switched on again, the voice starts with its registers (L958c).
	/// </summary>
	void ApplyDma()
	{
		foreach (var voice in voices)
		{
			var paula = voice.Paula;

			if ((dmaMask & voice.DmaBit) == 0)
			{
				paula.PlayLength = 0;
				paula.Position = 0.0;
				paula.NewStart = true;
			}
			else if (paula.NewStart)
			{
				paula.NewStart = false;
				paula.Position = 0.0;
				SetPlayBuffer(paula);
			}
		}
	}

	/// <summary>
	/// The period and the volume give the step through the sample data (L94f4).
	/// </summary>
	void UpdateIncrements()
	{
		foreach (var voice in voices)
		{
			var paula = voice.Paula;

			if (paula.Period == 0)
				continue;

			paula.Increment = PaulaClock / paula.Period / SampleRate;
			paula.PlayVolume = Math.Min(paula.Volume & 0xff, MaxVolume);
		}
	}

	#endregion

	#region Sequencer

	/// <summary>
	/// One tick of the sequencer (50 times per second, L9e88).
	/// </summary>
	void Tick()
	{
		if (stopped)
		{
			foreach (var voice in voices)
				voice.Paula.PlayLength = 0;

			return;
		}

		bool nextRow = --speedCounter == 0;

		if (nextRow)
			speedCounter = currentSpeed;

		// TFMX: the patterns of the mixed voices are read before the sequences (L1fdc6)
		if (nextRow && tfmx)
		{
			foreach (var voice in voices.Where(voice => voice.Mixed))
				UpdateTfmxPattern(voice);
		}

		restartMask = 0;
		var results = voices.Select(UpdateSequences).ToArray();
		ApplyDma();

		if (restartMask != 0)
		{
			dmaMask |= restartMask;
			ApplyDma();

			// After the start the registers point to the loop of the sample.
			foreach (var voice in voices)
			{
				voice.Paula.Pointer = voice.LoopPointer;
				voice.Paula.Length = voice.LoopLength;
			}
		}

		for (int i = 0; i < voices.Length; i++)
		{
			voices[i].Paula.Period = results[i].Period;
			voices[i].Paula.Volume = results[i].Volume;
		}

		if (restartMask != 0)
			ApplyDma();

		if (nextRow)
		{
			foreach (var voice in voices.Where(voice => !voice.Mixed))
			{
				if (tfmx)
					UpdateTfmxPattern(voice);
				else
					UpdatePattern(voice);
			}
		}

		UpdateIncrements();
	}

	/// <summary>
	/// The patterns give the notes and the sounds (L9fbc).
	/// </summary>
	void UpdatePattern(Voice voice)
	{
		if (--voice.PatternCounter >= 0)
			return;

		voice.PatternCounter = voice.PatternSpeed;

		while (true)
		{
			int command = voice.PatternEnd ? 0xff : module[voice.Pattern++];

			if (command == 0xff)
			{
				voice.PatternEnd = false;

				// the next division (or the first one after the last)
				if (voice.DivisionPosition == voice.DivisionEnd)
					voice.DivisionPosition = voice.DivisionStart;

				int pattern = module[voice.DivisionPosition];
				voice.Transpose = (sbyte)module[voice.DivisionPosition + 1];
				int effect = module[voice.DivisionPosition + 2];

				if (effect > 0x7f)
				{
					int type = effect >> 4;
					int value = effect & 0xf;

					if (type == 0xf)
						voice.DivisionVolume = value == 0 ? 100 : (16 - value) * 6;
					else if (type == 0x8)
						stopped = true;
					else if (type == 0xe)
						currentSpeed = value;
				}
				else
				{
					voice.TimbreTranspose = effect;
				}

				voice.Pattern = Pattern(pattern);
				voice.DivisionPosition += bytesPerDivision;
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
			int volumeSequence = VolumeSequence(timbre);
			voice.VolumePosition = 0;
			voice.VolumeCounter = voice.VolumeSpeed = module[volumeSequence];
			int frequencySequence = module[volumeSequence + 1];
			voice.VibratoSpeed = module[volumeSequence + 2];
			voice.VibratoFlags = 0x40;
			voice.VibratoDepth = voice.VibratoValue = module[volumeSequence + 3];
			voice.VibratoDelay = module[volumeSequence + 4];
			voice.VolumeSequenceData = module;
			voice.VolumeSequence = volumeSequence + 5;
			voice.VolumeDelay = 0;

			if (frequencySequence == 0x80)
				return;

			if ((voice.NoteInfo & 0x40) != 0)
				frequencySequence = voice.NoteParameter;

			voice.FrequencySequenceData = module;
			voice.FrequencySequence = FrequencySequence(frequencySequence);
			voice.FrequencyPosition = 0;
			voice.FrequencyDelay = 0;
			return;
		}
	}

	/// <summary>
	/// The patterns of TFMX modules have rows of 2 bytes, a note of 1 or the end of the pattern read the next division
	/// (L1ff14 in the player of the Amiga outro).
	/// </summary>
	void UpdateTfmxPattern(Voice voice)
	{
		int note = module[voice.Pattern + (voice.PatternPosition % patternSize)] & 0x7f;

		if (note == 1 || voice.PatternPosition == patternSize)
		{
			if (voice.DivisionPosition == voice.DivisionEnd)
				voice.DivisionPosition = voice.DivisionStart;

			int pattern = module[voice.DivisionPosition];
			voice.Transpose = (sbyte)module[voice.DivisionPosition + 1];
			voice.TimbreTranspose = module[voice.DivisionPosition + 2];
			int effect = module[voice.DivisionPosition + 3];
			int value = effect & 0xf;

			// d changes the mixing rate (only the quality of the mixed voices)
			switch (effect >> 4)
			{
				case 0xf:
					voice.DivisionVolume = value == 0 ? 100 : (16 - value) * 6;
					break;
				case 0x8:
					stopped = true;
					break;
				case 0xe:
					currentSpeed = value;
					break;
			}

			voice.PatternPosition = 0;
			voice.Pattern = Pattern(pattern);
			voice.DivisionPosition += bytesPerDivision;
		}

		int position = voice.Pattern + voice.PatternPosition;
		int command = module[position];
		note = command & 0x7f;

		if (note != 0)
		{
			voice.PortamentoValue = 0;
			voice.Note = note;
			// the parameter is the byte in front of the note (the last byte of the pattern for the first row)
			voice.NoteParameter = module[voice.Pattern + (voice.PatternPosition + patternSize - 1) % patternSize];
			voice.NoteInfo = module[position + 1];

			// Notes with bit 7 only change the note.
			if ((command & 0x80) == 0)
			{
				int timbre = ((voice.NoteInfo & 0x1f) + voice.TimbreTranspose) & 0xff;
				int volumeSequence = VolumeSequence(timbre);
				voice.VolumePosition = 0;
				voice.VolumeCounter = voice.VolumeSpeed = module[volumeSequence];
				int frequencySequence = module[volumeSequence + 1];
				voice.VibratoSpeed = module[volumeSequence + 2];
				voice.VibratoFlags = 0x40;
				voice.VibratoDepth = voice.VibratoValue = module[volumeSequence + 3];
				voice.VibratoDelay = module[volumeSequence + 4];
				voice.VolumeSequenceData = module;
				voice.VolumeSequence = volumeSequence + 5;
				voice.VolumeDelay = 0;

				if (frequencySequence != 0x80)
				{
					if ((voice.NoteInfo & 0x40) != 0)
						frequencySequence = voice.NoteParameter;

					voice.FrequencySequenceData = module;
					voice.FrequencySequence = FrequencySequence(frequencySequence);
					voice.FrequencyPosition = 0;
					voice.FrequencyDelay = 0;
				}
			}
		}

		voice.PatternPosition += 2;
	}

	/// <summary>
	/// The sample header has the offset (long), the length (words), the loop offset (bytes) and the loop length (words).
	/// In TFMX modules the name is in front and there is another word in front of the loop offset.
	/// </summary>
	(int Offset, int Length, int LoopOffset, int LoopLength) SampleHeader(int sample)
	{
		if (tfmx)
		{
			int tfmxHeader = sampleHeaders + sample * TfmxSampleHeaderSize + TfmxSampleHeaderDataOffset;

			return (Long(tfmxHeader), Word(tfmxHeader + 4), Word(tfmxHeader + 8), Word(tfmxHeader + 10));
		}

		int header = sampleHeaders + sample * 10;

		return (Long(header), Word(header + 4), Word(header + 6), Word(header + 8));
	}

	(int Pointer, int Length, int LoopPointer, int LoopLength) SampleInfo(int sample)
	{
		var (offset, length, loopOffset, loopLength) = SampleHeader(sample);
		int pointer = sampleData + offset;

		return (pointer, length, pointer + loopOffset, loopLength);
	}

	void RestartDma(Voice voice)
	{
		restartMask |= voice.DmaBit;
		dmaMask &= ~voice.DmaBit;
	}

	void SetSample(Voice voice, int sample)
	{
		var (pointer, length, loopPointer, loopLength) = SampleInfo(sample);
		voice.Paula.Data = module;
		voice.Paula.Pointer = pointer;
		voice.Paula.Length = length;
		voice.LoopPointer = loopPointer;
		voice.LoopLength = loopLength;
	}

	/// <summary>
	/// The frequency and volume sequences of a voice (La22e). Returns the period and the volume.
	/// </summary>
	(int Period, int Volume) UpdateSequences(Voice voice)
	{
		UpdateFrequencySequence(voice);
		UpdateSweep(voice);
		UpdateVolumeSequence(voice);

		return (CalculatePeriod(voice), CalculateVolume(voice));
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
				case 0xe1: // hold
					return;
				case 0xe2: // sample with restart
					voice.Sample = 0xff;
					voice.RandomVolume = 0;
					RestartDma(voice);
					SetSample(voice, data[position + 1]);
					voice.VolumePosition = 0;
					voice.VolumeCounter = 1;
					voice.FrequencyPosition += 2;
					voice.Sweep = false;
					continue;
				case 0xe3: // vibrato
					voice.VibratoSpeed = data[position + 1];
					voice.VibratoDepth = data[position + 2];
					voice.FrequencyPosition += 3;
					continue;
				case 0xe4: // sample without restart
				{
					var (pointer, length, loopPointer, loopLength) = SampleInfo(data[position + 1]);
					voice.Paula.Data = module;
					voice.Paula.Pointer = pointer;
					voice.LoopPointer = loopPointer;
					voice.Paula.Length = length;
					voice.LoopLength = loopLength;
					voice.FrequencyPosition += 2;
					voice.Sweep = false;
					continue;
				}
				case 0xe5: // sample with a sweep through it
				{
					RestartDma(voice);
					voice.RandomVolume = 0;
					var (sweepStart, length, _, _) = SampleHeader(data[position + 1]);
					voice.SweepStart = sweepStart;
					voice.SweepEnd = voice.SweepStart + length * 2;
					int start = (data[position + 2] << 8) | data[position + 3];
					voice.SweepPosition = start == 0xffff ? length : start;
					voice.SweepLength = (data[position + 4] << 8) | data[position + 5];
					voice.SweepStep = (data[position + 6] << 8) | data[position + 7];
					voice.SweepCounter = 0;
					voice.SweepSpeed = data[position + 8];
					voice.SweepEnded = false;
					voice.SweepStarted = false;
					voice.Sweep = true;
					voice.VolumePosition = 0;
					voice.VolumeCounter = 1;
					voice.FrequencyPosition += 9;
					continue;
				}
				case 0xe6: // change the sweep
					voice.SweepLength = (data[position + 1] << 8) | data[position + 2];
					voice.SweepStep = (data[position + 3] << 8) | data[position + 4];
					voice.SweepCounter = 0;
					voice.SweepSpeed = data[position + 5];
					voice.SweepStarted = false;
					voice.SweepEnded = false;
					voice.FrequencyPosition += 6;
					continue;
				case 0xe7: // sample with restart if it changed
				{
					int sample = data[position + 1];

					if (sample != voice.Sample)
					{
						voice.Sample = sample;
						voice.RandomVolume = 0;
						RestartDma(voice);
						SetSample(voice, sample);
					}

					voice.VolumePosition = 0;
					voice.VolumeCounter = 1;
					voice.FrequencyPosition += 2;
					voice.Sweep = false;
					continue;
				}
				case 0xe8: // wait
					voice.FrequencyDelay = data[position + 1];
					voice.FrequencyPosition += 2;
					continue;
				case 0xe9: // sample of a sample bank
				{
					voice.Sample = 0xff;
					voice.RandomVolume = 0;
					RestartDma(voice);
					int bank = sampleData + SampleHeader(data[position + 1]).Offset;
					int entries = bank + 8;
					int waves = entries + Word(bank + 4) * 0x18 + Word(bank + 6) * 4;
					int entry = entries + data[position + 2] * 0x18;
					int start = Long(entry) & ~1;
					int end = Long(entry + 4) & ~1;
					voice.Paula.Data = module;
					voice.Paula.Pointer = waves + start;
					voice.Paula.Length = (end - start) / 2;
					voice.LoopPointer = waves + start;
					voice.LoopLength = 1;
					voice.VolumePosition = 0;
					voice.VolumeCounter = 1;
					voice.FrequencyPosition += 3;
					voice.Sweep = false;
					continue;
				}
				case 0xea when tfmx: // the volume is reduced by a random part of the value once
					voice.RandomVolume = data[position + 1];
					voice.RandomVolumeValue = 0;
					voice.FrequencyPosition += 2;
					continue;
				default:
					// unknown commands are skipped
					voice.FrequencyPosition++;
					continue;
			}
		}
	}

	/// <summary>
	/// The loop of the sample moves through the sample (La522).
	/// </summary>
	void UpdateSweep(Voice voice)
	{
		if (!voice.Sweep || voice.SweepEnded)
			return;

		if (--voice.SweepCounter >= 0)
			return;

		voice.SweepCounter = voice.SweepSpeed;
		int position = voice.SweepPosition;
		int step = (short)voice.SweepStep;

		if (!voice.SweepStarted)
		{
			voice.SweepStarted = true;
		}
		else
		{
			position += step;

			if (position < 0 || voice.SweepStart + position * 2 + voice.SweepLength * 2 > voice.SweepEnd)
			{
				voice.SweepEnded = true;
				position -= step;
			}
		}

		voice.SweepPosition = position;
		int pointer = sampleData + voice.SweepStart + position * 2;
		voice.LoopLength = voice.SweepLength;
		voice.LoopPointer = pointer;
		voice.Paula.Data = module;
		voice.Paula.Pointer = pointer;
		voice.Paula.Length = voice.SweepLength;
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

				if (command == 0xe0) // loop (relative to the 5 bytes header)
				{
					voice.VolumePosition = data[position + 1] - 5;
					continue;
				}

				if (command == 0xe8) // wait
				{
					voice.VolumeDelay = data[position + 1];
					voice.VolumePosition += 2;
					break;
				}

				// hold
				return;
			}
		}
	}

	/// <summary>
	/// The note, the vibrato and the portamento give the period (La610).
	/// </summary>
	int CalculatePeriod(Voice voice)
	{
		int note = voice.FrequencyValue;

		// Values with bit 7 are fixed notes.
		if ((note & 0x80) == 0)
			note += voice.Note + voice.Transpose;

		note &= 0x7f;
		int period = tfmx ? (note < TfmxPeriodCount ? Periods[note] : 0)
			: note < Periods.Length ? Periods[note] : Word(note * 2 - Periods.Length * 2);

		if (voice.VibratoDelay != 0)
		{
			voice.VibratoDelay--;
		}
		else
		{
			int value = voice.VibratoValue;
			int flags = voice.VibratoFlags;

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

				if (value > voice.VibratoDepth)
				{
					flags &= ~0x20;
					value = voice.VibratoDepth;
				}
			}

			voice.VibratoValue = value & 0xff;
			voice.VibratoFlags = flags;
			period += ((value - (voice.VibratoDepth >> 1)) * period) >> 10;
		}

		// portamento
		if ((voice.NoteInfo & 0x20) != 0)
		{
			int parameter = voice.NoteParameter;

			if ((parameter & 0x80) == 0)
			{
				voice.PortamentoValue = (voice.PortamentoValue + parameter) & 0xffff;
				period -= (voice.PortamentoValue * (period & 0xffff)) >> 10;
			}
			else
			{
				voice.PortamentoValue = (voice.PortamentoValue + ((-parameter) & 0xff)) & 0xffff;
				period += (voice.PortamentoValue * (period & 0xffff)) >> 10;
			}
		}

		voice.Period = period & 0xffff;

		return voice.Period;
	}

	/// <summary>
	/// The volume of the sequence is scaled by the volume of the division (La6cc). In the TFMX player a random part
	/// of a value can be subtracted from the volume of the division (L206b6).
	/// </summary>
	int CalculateVolume(Voice voice)
	{
		int volume = voice.DivisionVolume;

		if (voice.RandomVolume != 0)
		{
			if (voice.RandomVolumeValue == 0)
			{
				voice.RandomVolumeValue = voice.RandomVolume * (NextRandom() & 0xff) / 0xff;
				voice.RandomVolume = 0;
			}

			volume -= voice.RandomVolumeValue;
		}

		return voice.VolumeValue * Math.Max(0, volume) / 100;
	}

	/// <summary>
	/// The random numbers of the TFMX player (L2070e).
	/// </summary>
	int NextRandom()
	{
		int value = (randomSeed + 0x4793) & 0xffff;
		randomSeed = (((value >> 6) | (value << 10)) & 0xffff) ^ value;

		return randomSeed;
	}

	#endregion
}
