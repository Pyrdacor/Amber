namespace Amberstar.GameData.Legacy;

/// <summary>
/// Plays COSO modules with samples (TFMX variant with 4 voices) like the Amiga does with its sound chip Paula.
///
/// This was taken from the music player of the outro (EXTRO.68K) which emulates the 4 sample voices of the Amiga
/// on the Atari ST: the sequencer (L9e88 in the player), the patterns (L9fbc), the frequency and volume sequences
/// of each voice (La22e), the DMA of the voices (L958c) and the mixing (L94f4 and L9c6).
/// </summary>
internal sealed class HippelCosoPaulaSong : ISong
{
	const int SampleRate = 44100;
	const int TicksPerSecond = 50;
	const int BufferSize = SampleRate / 4;
	const double BufferTime = BufferSize * 1000.0 / SampleRate;
	const double PaulaClock = 3546895.0;
	const int VoiceCount = 4;
	const int BytesPerDivision = 3 * VoiceCount;
	// The emulation of the outro does not play samples shorter than this (in words, L95fa).
	const int EmulatedMinSampleLength = 0xdc;
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
	}

	/// <summary>
	/// The state of a voice of the sequencer (the 84 byte structures La718 etc. in the player).
	/// </summary>
	sealed class Voice
	{
		public int DmaBit;
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
	readonly int minSampleLength;
	readonly int frequencySequences;
	readonly int volumeSequences;
	readonly int patterns;
	readonly int divisions;
	readonly int sampleHeaders;
	readonly int sampleData;
	readonly int startDivision;
	readonly int endDivision;
	readonly int speed;
	readonly Voice[] voices = new Voice[VoiceCount];
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
	/// The module data starts with "COSO". Songs are 1-based.
	///
	/// The Amiga plays samples of any length. The emulation of the outro on the Atari ST does not play short
	/// samples (like the waveforms of the leading voices of the Amiga version), so this is only used for it.
	/// </summary>
	public HippelCosoPaulaSong(byte[] module, int song = 1, bool emulated = true)
	{
		this.module = module;
		minSampleLength = emulated ? EmulatedMinSampleLength : 1;
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
	/// Initializes the song (La12e in the player).
	/// </summary>
	public void Reset()
	{
		for (int i = 0; i < VoiceCount; i++)
		{
			voices[i] = new Voice
			{
				DmaBit = 1 << i,
				DivisionStart = divisions + startDivision * BytesPerDivision + i * 3,
				DivisionEnd = divisions + (endDivision + 1) * BytesPerDivision + i * 3,
			};
			voices[i].DivisionPosition = voices[i].DivisionStart;
		}

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

			foreach (var voice in voices)
				sum += NextSample(voice.Paula);

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

		if (length < minSampleLength)
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

		for (int i = 0; i < VoiceCount; i++)
		{
			voices[i].Paula.Period = results[i].Period;
			voices[i].Paula.Volume = results[i].Volume;
		}

		if (restartMask != 0)
			ApplyDma();

		if (--speedCounter == 0)
		{
			speedCounter = currentSpeed;

			foreach (var voice in voices)
				UpdatePattern(voice);
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

				voice.Pattern = TableEntry(patterns, pattern);
				voice.DivisionPosition += BytesPerDivision;
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
			voice.VolumeDelay = 0;

			if (frequencySequence == 0x80)
				return;

			if ((voice.NoteInfo & 0x40) != 0)
				frequencySequence = voice.NoteParameter;

			voice.FrequencySequenceData = module;
			voice.FrequencySequence = TableEntry(frequencySequences, frequencySequence);
			voice.FrequencyPosition = 0;
			voice.FrequencyDelay = 0;
			return;
		}
	}

	/// <summary>
	/// The sample header has the offset (long), the length (words), the loop offset (bytes) and the loop length (words).
	/// </summary>
	(int Pointer, int Length, int LoopPointer, int LoopLength) SampleInfo(int sample)
	{
		int header = sampleHeaders + sample * 10;
		int pointer = sampleData + Long(header);

		return (pointer, Word(header + 4), pointer + Word(header + 6), Word(header + 8));
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
					int header = sampleHeaders + data[position + 1] * 10;
					int length = Word(header + 4);
					voice.SweepStart = Long(header);
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
					RestartDma(voice);
					int bank = sampleData + Long(sampleHeaders + data[position + 1] * 10);
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
		int period = note < Periods.Length ? Periods[note] : Word(note * 2 - Periods.Length * 2);

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
	/// The volume of the sequence is scaled by the volume of the division (La6cc).
	/// </summary>
	static int CalculateVolume(Voice voice) => voice.VolumeValue * Math.Max(0, voice.DivisionVolume) / 100;

	#endregion
}
