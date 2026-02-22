namespace Amberstar.GameData.Legacy;

internal abstract class HippelCosoSong : ISong
{
    internal record SongInfo(int StartDivision, int EndDivision, int InitialSpeed);

    internal record Instrument(params Instrument.Command[] Commands)
    {
        public enum CommandType
        {
            SetPitch,
            Loop,
            Complete,
            ResetTimbre,
            Vibrato,
            EnableToneAndNoise,
            DisableToneEnableNoise,
            EnableToneDisableNoise,
            SetTimbre,
            Delay,
            NextCommand,
            Portando,
            SetInstrumentFlags,
            //SetSample,
            //ResetVolume,
        }

        public record Command(int DataIndex, CommandType Type, params int[] Params);

        private int currentCommandIndex = 0;
        private int tickCounter = 1;

        public virtual void Reset()
        {
            currentCommandIndex = 0;
            tickCounter = 1;
        }

        public virtual void ProcessNextCommand(HippelCosoSong player)
        {
            if (tickCounter > 0)
            {
                --tickCounter;
                return;
            }

            bool processCommands = true;

            while (processCommands)
            {
                var command = Commands[currentCommandIndex];

                switch (command.Type)
                {
                    case CommandType.SetPitch:
                        player.SetPitch(command.Params[0]);
                        ++currentCommandIndex;
                        processCommands = false;
                        break;
                    case CommandType.Loop:
                        int byteOffset = command.Params[0];
                        currentCommandIndex = Commands.ToList().FindIndex(cmd => cmd.DataIndex == byteOffset);
                        break;
                    case CommandType.Complete:
                        // Do not increase the index.
                        player.SetPitch(Commands[currentCommandIndex - 1].Params[0]);
                        processCommands = false;
                        break;
                    /*case CommandType.SetSample:
                        throw new NotImplementedException(); // TODO
                        player.SetSample(command.Params[0]);
                        ++currentCommandIndex;
                        ProcessNextCommand(player);
                        break;
                    case CommandType.ResetVolume:
                        player.ResetVolume();
                        ++currentCommandIndex;
                        ProcessNextCommand(player);
                        break;*/
                    case CommandType.ResetTimbre:
                        player.ResetTimbre();
                        ++currentCommandIndex;
                        break;
                    case CommandType.Vibrato:
                        player.channels[player.currentVoice].CurrentVibratoSlope = command.Params[0];
                        player.channels[player.currentVoice].CurrentVibratoDepth = command.Params[1];
                        ++currentCommandIndex;
                        break;
                    case CommandType.EnableToneAndNoise:
                        player.channels[player.currentVoice].NoisePeriod = command.Params[0];
                        player.channels[player.currentVoice].Tone = true;
                        player.channels[player.currentVoice].Noise = true;
                        ++currentCommandIndex;
                        break;
                    case CommandType.DisableToneEnableNoise:
                        player.channels[player.currentVoice].NoisePeriod = -1; // to trigger init logic
                        player.channels[player.currentVoice].Tone = false;
                        player.channels[player.currentVoice].Noise = true;
                        ++currentCommandIndex;
                        break;
                    case CommandType.EnableToneDisableNoise:
                        player.channels[player.currentVoice].Tone = true;
                        player.channels[player.currentVoice].Noise = false;
                        ++currentCommandIndex;
                        break;
                    case CommandType.SetTimbre:
                        player.channels[player.currentVoice].SetTimbre(command.Params[0], null);
                        Reset();
                        var instrument = player.instruments[player.channels[player.currentVoice].CurrentInstrument];
                        instrument.tickCounter = 1;
                        instrument.ProcessNextCommand(player);
                        return;
                    case CommandType.Delay:
                        tickCounter = command.Params[0];
                        processCommands = false;
                        ProcessNextCommand(player);
                        break;
                    case CommandType.NextCommand:
                        // Just keep going.
                        ++currentCommandIndex;
                        break;
                    case CommandType.Portando:
                        player.channels[player.currentVoice].Portando = true;
                        player.channels[player.currentVoice].PortandoSlope = unchecked((sbyte)command.Params[0]);
                        ++currentCommandIndex;
                        break;                   
                    case CommandType.SetInstrumentFlags:
                        // TODO
                        // player.channels[player.currentVoice].SetInstrumentFlags(command.Params[0]);
                        break;
                }
            }
        }
    }

    internal record Vibrato(int Slope, int Depth, int Delay);

    internal record Timbre(int Speed, int Instrument, Vibrato Vibrato, VolumeEnvelop VolumeEnvelop);

    internal record VolumeEnvelop(int Speed, params VolumeEnvelop.Command[] Commands)
    {
        public enum CommandType
        {
            SetVolume,
            Hold,
            Sustain,
            Loop,
        }

        public record Command(int DataIndex, CommandType Type, params int[] Params);

        private int currentCommandIndex = 0;
        private int tickCounter = 1;
        private int delayCounter = 0;

        public virtual void Reset(int? newTickCounter)
        {
            currentCommandIndex = 0;
            tickCounter = newTickCounter ?? Speed;
            //delayCounter = 0;
        }

        public void ResetSustain()
        {
            delayCounter = 0;
        }

        public virtual void ProcessNextCommand(HippelCosoSong player)
        {
            if (delayCounter > 0)
            {
                --delayCounter;
                return;
            }

            if (--tickCounter > 0)
            {
                return;
            }

            tickCounter = Speed;

            bool processCommands = true;

            while (processCommands)
            {
                var command = Commands[currentCommandIndex];

                switch (command.Type)
                {
                    case CommandType.SetVolume:
                        player.SetVolume(command.Params[0]);
                        ++currentCommandIndex;
                        processCommands = false;
                        break;
                    case CommandType.Sustain:
                        ++currentCommandIndex;
                        delayCounter = command.Params[0];
                        processCommands = false;
                        ProcessNextCommand(player);
                        break;
                    case CommandType.Loop:
                        int byteOffset = command.Params[0];
                        currentCommandIndex = Commands.ToList().FindIndex(cmd => cmd.DataIndex == byteOffset);
                        break;
                    case CommandType.Hold:
                        player.SetVolume(Commands[currentCommandIndex - 1].Params[0]);
                        // Do not increase the index.
                        processCommands = false;
                        break;
                }
            }
        }
    }

    internal record Pattern(params Pattern.Command[] Commands)
    {
        public enum CommandType
        {
            SetNote, // and optionally timbre and/or instrument
            EndPattern,
            SetSpeed,
            SetSpeedWithDelay
        }

        public record Command(CommandType Type, params int[] Params);

        private int currentCommandIndex = 0;
        private int tickCounter = 0;
        private int speed = 0;

        public virtual void Reset()
        {
            currentCommandIndex = 0;
            tickCounter = 0;
        }

        public virtual void ProcessNextCommand(HippelCosoSong player)
        {
            if (--tickCounter >= 0)
                return;

            tickCounter = speed;
            bool processCommands = true;

            while (processCommands)
            {
                var command = Commands[currentCommandIndex];

                switch (command.Type)
                {
                    case CommandType.SetNote:
                        // Param 0: Note
                        // Param 1: Timbre
                        // Param 2: Instrument
                        // Param 3: Portando (0 or 1)
                        player.SetNote(command.Params[0]);
                        if (command.Params.Length > 1 && command.Params[1] != -1)
                        {
                            int? customInstrument = null;

                            if (command.Params.Length > 2 && command.Params[2] != -1)
                                customInstrument = command.Params[2];

                            // Set timbre
                            player.SetTimbre(player.GetTimbre() + command.Params[1], customInstrument);
                        }
                        // Technically this is not zeroed if no note param flag was set, but it should be fine.
                        player.channels[player.currentVoice].CurrentPortandoDelta = 0;
                        player.channels[player.currentVoice].Portando = (command.Params.Length > 3 && command.Params[3] != 0);
                        ++currentCommandIndex;
                        processCommands = false;
                        break;
                    case CommandType.SetSpeed:
                        speed = command.Params[0];
                        tickCounter = speed;
                        ++currentCommandIndex;
                        break;
                    case CommandType.SetSpeedWithDelay:
                        speed = command.Params[0];
                        tickCounter = speed;
                        ++currentCommandIndex;
                        processCommands = false;
                        break;
                    case CommandType.EndPattern:
                        processCommands = false;
                        player.NextDivision();
                        break;
                }
            }
        }
    }

    internal record Division(Division.Channel[] Channels)
    {
        public record Channel(int PatternIndex, int Transpose, int TimbreIndex, int VolumeReduction, int SongSpeed, int TimbreAdjust);
    }

    internal abstract class ChannelPlayer(Func<int, int>? noisePeriodConverter)
    {
        private protected record NoteInfo(double Time, int Period, int Volume);
        private protected record NoiseInfo(double Time, int Period);

        private protected readonly Queue<NoteInfo> notePeriods = [];
        private protected readonly Queue<NoiseInfo> noisePeriods = [];

        public virtual void Reset()
        {
            // empty for now
        }

        // This happens every tick as long as the channel is active.
        public void PlayNote(double time, int period, int volume)
        {
            notePeriods.Enqueue(new(time, period, volume));
        }

        public void ChangeNoise(double time, int period)
        {
            if (period <= 0)
            {
                noisePeriods.Enqueue(new(time, -1));
                return;
            }

            if (noisePeriodConverter != null)
                period = noisePeriodConverter(period);

            noisePeriods.Enqueue(new(time, period));
        }

        public abstract void SampleData(short[] buffer, double time,
            Action<int, bool> enableChannel, bool firstChannel);
    }

    private static readonly int[] NotePeriods =
    [
        0x0eee, 0x0e17, 0x0d4d, 0x0c8e, 0x0bd9, 0x0b2f, 0x0a8e, 0x09f7,
        0x0967, 0x08e0, 0x0861, 0x07e8, 0x0777, 0x070b, 0x06a6, 0x0647,
        0x05ec, 0x0597, 0x0547, 0x04fb, 0x04b3, 0x0470, 0x0430, 0x03f4,
        0x03bb, 0x0385, 0x0353, 0x0323, 0x02f6, 0x02cb, 0x02a3, 0x027d,
        0x0259, 0x0238, 0x0218, 0x01fa, 0x01dd, 0x01c2, 0x01a9, 0x0191,
        0x017b, 0x0165, 0x0151, 0x013e, 0x012c, 0x011c, 0x010c, 0x00fd,
        0x00ee, 0x00e1, 0x00d4, 0x00c8, 0x00bd, 0x00b2, 0x00a8, 0x009f,
        0x0096, 0x008e, 0x0086, 0x007e, 0x0077, 0x0070, 0x006a, 0x0064,
        0x005e, 0x0059, 0x0054, 0x004f, 0x004b, 0x0047, 0x0043, 0x003f,
        0x003b, 0x0038, 0x0035, 0x0032, 0x002f, 0x002c, 0x002a, 0x0027,
        0x0025, 0x0023, 0x0021, 0x001f, 0x001d, 0x001c, 0x001a, 0x0019,
        0x0017, 0x0016, 0x0015, 0x0013, 0x0012, 0x0011, 0x0010, 0x000f,
        0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
        0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
        0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
        0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000
    ];

    // TODO: This is also used for something. Also indexed by total note/pitch but limited to 96.
    /*0x00000400, 0x0000043c, 0x0000047d, 0x000004c1, 0x0000050a, 0x00000556, 0x000005a8, 0x000005fe,
    0x00000659, 0x000006ba, 0x00000720, 0x0000078d, 0x00000800, 0x00000879, 0x000008fa, 0x00000983,
    0x00000a14, 0x00000aad, 0x00000b50, 0x00000bfc, 0x00000cb2, 0x00000d74, 0x00000e41, 0x00000f1a,
    0x00001000, 0x000010f3, 0x000011f5, 0x00001306, 0x00001428, 0x0000155b, 0x000016a0, 0x000017f9,
    0x00001965, 0x00001ae8, 0x00001c82, 0x00001e34, 0x00002000, 0x000021e7, 0x000023eb, 0x0000260d,
    0x00002851, 0x00002ab7, 0x00002d41, 0x00002ff2, 0x000032cb, 0x000035d1, 0x00003904, 0x00003c68,
    0x00004000, 0x000043ce, 0x000047d6, 0x00004c1b, 0x000050a2, 0x0000556e, 0x00005a82, 0x00005fe4,
    0x00006597, 0x00006ba2, 0x00007208, 0x000078d0, 0x00008000, 0x0000879c, 0x00008fac, 0x00009837,
    0x0000a145, 0x0000aadc, 0x0000b504, 0x0000bfc8, 0x0000cb2f, 0x0000d744, 0x0000e411, 0x0000f1a1,
    0x00010000, 0x00010f38, 0x00011f59, 0x0001306f, 0x0001428a, 0x000155b8, 0x00016a09, 0x00017f91,
    0x0001965f, 0x0001ae89, 0x0001c823, 0x0001e343, 0x00020000, 0x00021e71, 0x00023eb3, 0x000260df,
    0x00028514, 0x0002ab70, 0x0002d413, 0x0002ff22, 0x00032cbf, 0x00035d13, 0x00039047, 0x0003c686, // 96
    0x0003c686, 0x0003c686, 0x0003c686, 0x0003c686, 0x0003c686, 0x0003c686, 0x0003c686, 0x0003c686,
    0x0003c686, 0x0003c686, 0x0003c686, 0x0003c686, 0x0003c686, 0x0003c686, 0x0003c686, 0x0003c686,
    0x0003c686, 0x0003c686, 0x0003c686, 0x0003c686, 0x0003c686, 0x0003c686, 0x0003c686, 0x0003c686,
    0x0003c686, 0x0003c686, 0x0003c686, 0x0003c686, 0x0003c686, 0x0003c686, 0x0003c686, 0x0003c686,*/

    // TODO: Normally it should be 20ms per tick, but 120 works much better for some reason...
    private protected const int TickTime = 20; // ms
    private protected const int SampleRate = 44100; // Hz
    private protected const int BufferSize = SampleRate / 4; // 0.25 second of audio
    private protected const double BufferTime = BufferSize * 1000.0 / SampleRate; // in ms
    private protected double elapsedTime = 0.0;
    int songSpeedCounter = 1;
    private protected int songSpeed = 0;
    double totalTime = 0.0;
    double lastSampleTime = 0.0;
    bool paused = false;
    bool playing = false;
    private protected readonly int voiceCount = 0;
    int currentVoice = 0;
    readonly Instrument[] instruments;
    readonly Timbre[] timbres;
    readonly Division[] divisions;    
    readonly Pattern[] patterns;
    private protected readonly Channel[] channels;
    readonly SongInfo songInfo;
    private protected readonly ChannelPlayer[] channelPlayers;
    readonly short[][] channelPcmData;
    readonly int[] mixedData;
    readonly byte[] mixedDataCounts;
    readonly short[] pcmData;
    bool prebuffered = false;
    double endOfStreamTime = 0.0;
    bool resetDivisions = false;

    private protected abstract int MaxVolume { get; }

    private protected abstract class Channel(HippelCosoSong player, ChannelPlayer channelPlayer, int channelIndex)
    {
        private int currentDivisionIndex = -1;
        private int currentInstrumentIndex = -1;
        private int currentTimbreIndex = -1;
        private Division.Channel? currentDivision;
        private Pattern? currentPattern;        
        private Instrument? currentInstrument;
        private Timbre? currentTimbre;

        public bool HasDivision => currentTimbre != null;

        public event Action<int>? SongSpeedChanged;

        public int Volume { get; set; }
        public int Pitch { get; set; }
        public int Note { get; set; }
        public int Sample { get; set; }
        public bool IsPlaying { get; set; }
        public int CurrentVibratoDelay { get; set; }
        public int CurrentVibratoSlope { get; set; }
        public int CurrentVibratoDepth { get; set; }
        public int CurrentVibratoDirection { get; set; }
        public int PortandoSlope { get; set; }
        public int CurrentPortandoDelta { get; set; }
        public bool Portando { get; set; }
        public bool Noise { get; set; } = true;
        public bool Tone { get; set; } = true;
        public int NoisePeriod { get; set; } = -1;
        public int CurrentInstrument
        {
            get => currentInstrumentIndex;
            set
            {
                currentInstrumentIndex = value;
                currentInstrument = player.instruments[value];
                InstumentChanged();
            }
        }
        public int CurrentTimbre
        {
            get => currentTimbreIndex;
            set
            {
                currentTimbreIndex = value;
                currentTimbre = player.timbres[value];
                TimbreChanged();
            }
        }
        public int CurrentTimbreAdjust => currentDivision?.TimbreAdjust ?? 0;

        private protected virtual void InstumentChanged()
        {
            currentInstrument?.Reset();
            currentTimbre?.VolumeEnvelop.ResetSustain();
        }

        private protected virtual void TimbreChanged()
        {
            currentTimbre?.VolumeEnvelop.Reset(null);
        }

        private protected virtual int CalculateNotePeriod()
        {
            int note = Pitch;

            if ((note & 0x80) == 0)
                note += Note + (currentDivision?.Transpose ?? 0);

            note &= 0x7f;

            return NotePeriods[note];
        }

        private protected virtual int CalculateVolume()
        {
            return Math.Max(0, Volume - (currentDivision?.VolumeReduction ?? 0));
        }

        private protected virtual void ProcessVibrato(ref int period)
        {
            if (CurrentVibratoDelay == 0)
            {
                // It looks like this happen only every two calls in original
                CurrentVibratoDepth += CurrentVibratoDirection * CurrentVibratoSlope;

                if (CurrentVibratoDepth < 0 || CurrentVibratoDepth > 2 * currentTimbre!.Vibrato.Depth)
                    CurrentVibratoDirection = -CurrentVibratoDirection;

                CurrentVibratoDepth = Math.Clamp(CurrentVibratoDepth, 0, 2 * currentTimbre!.Vibrato.Depth);

                int diff = CurrentVibratoDepth - currentTimbre.Vibrato.Depth;

                period += (period * diff) / 1024;
            }
            else
            {
                --CurrentVibratoDelay;
            }
        }

        private protected virtual void ProcessPortando(ref int period)
        {
            CurrentPortandoDelta += PortandoSlope;

            // This code would be for the "MARC" format
            // period -= CurrentPortandoDelta / 65536;

            period -= (CurrentPortandoDelta * period / 1024);
        }

        public virtual void Reset()
        {
            Volume = player.MaxVolume;
            Pitch = 0;
            Note = 0;
            Sample = 0;
            Tone = true;
            Noise = false;
            CurrentVibratoDelay = 0;
            CurrentVibratoDepth = 0;
            CurrentVibratoSlope = 0;
            CurrentVibratoDirection = -1;
            Portando = false;
            PortandoSlope = 0;
            CurrentPortandoDelta = 0;
            currentInstrumentIndex = -1;
            currentTimbreIndex = -1;
            currentInstrument = null;
            currentTimbre = null;
            currentDivision = null;
            currentPattern = null;
            IsPlaying = false;
        }

        public virtual void ResetTimbre()
        {
            currentTimbre?.VolumeEnvelop.Reset(1);
        }

        public void SetTimbre(int index, int? customInstrument)
        {
            CurrentTimbre = index;

            CurrentInstrument = customInstrument ?? currentTimbre!.Instrument;

            CurrentVibratoDelay = currentTimbre!.Vibrato.Delay;
            CurrentVibratoSlope = currentTimbre.Vibrato.Slope;
            CurrentVibratoDepth = currentTimbre.Vibrato.Depth;
            CurrentVibratoDirection = -1;
        }

        public virtual void NextDivision(bool processFirstPattern)
        {
            currentDivisionIndex++;

            if (currentDivisionIndex >= player.songInfo.EndDivision)
            {
                currentDivisionIndex = player.songInfo.StartDivision;
                IsPlaying = false;
                return;
            }

            currentDivision = player.divisions[currentDivisionIndex].Channels[channelIndex];
            InitDivision();

            if (processFirstPattern)
                currentPattern!.ProcessNextCommand(player); // Directly process the next pattern in this case.
        }

        private protected virtual void InitDivision()
        {
            CurrentTimbre = currentDivision!.TimbreIndex + currentDivision.TimbreAdjust;
            CurrentInstrument = currentTimbre!.Instrument;

            currentPattern = player.patterns[currentDivision!.PatternIndex];
            currentPattern!.Reset();

            CurrentVibratoDelay = currentTimbre.Vibrato.Delay;
            CurrentVibratoSlope = currentTimbre.Vibrato.Slope;
            CurrentVibratoDepth = currentTimbre.Vibrato.Depth;
            CurrentVibratoDirection = -1;

            Noise = false;
            Tone = true;
            Volume = 0;

            if (currentDivision.SongSpeed != -1)
                SongSpeedChanged?.Invoke(currentDivision.SongSpeed);
        }

        public virtual void Update(double totalTime, bool updatePatterns)
        {
            if (currentDivision == null) // start
            {
                IsPlaying = true;
                currentDivisionIndex = player.songInfo.StartDivision;
                currentDivision = player.divisions[currentDivisionIndex].Channels[channelIndex];
                CurrentPortandoDelta = 0;
                InitDivision();
            }
            else if (!IsPlaying)
            {
                return;
            }

            bool wasUsingNoise = Noise;
            bool wasUsingTone = Tone;

            // Note: In Atari ST player there is a default instrument which
            // is selected at start. It sets pitch to 1, then to 0 (6 times)
            // and then a complete command. So basically 1, 0, 0, 0, 0, 0, 0, 0xe1.
            // The same data is also pre-selected for timbre. Here it means:
            // Speed 1, Instrument 0, Vibrato slope, depth and delay 0.
            // Then set volume to 0 twice and hold that volume.
            
            currentInstrument?.ProcessNextCommand(player);
            currentTimbre?.VolumeEnvelop.ProcessNextCommand(player);

            if (updatePatterns)
                currentPattern?.ProcessNextCommand(player);

            int period = CalculateNotePeriod();

            // Vibrato
            ProcessVibrato(ref period);

            // Portando
            if (Portando)
            {
                ProcessPortando(ref period);
            }

            int volume = CalculateVolume();

            if (Tone)
                channelPlayer.PlayNote(totalTime, period, volume);
            else if (wasUsingTone)
                channelPlayer.PlayNote(totalTime, -1, 0);

            if (NoisePeriod == -1 && Noise) // Init
            {
                NoisePeriod = Tone ? period : Note;
                wasUsingNoise = false;
            }

            if (!wasUsingNoise && Noise)
            {
                if (Tone) // If both (tone and noise) are active, e4 was used which sets the NoisePeriod property.
                {
                    channelPlayer.ChangeNoise(totalTime, NoisePeriod);
                }
                else if ((Pitch & 0x80) == 0) // Otherwise, e5 was used, so use the pitch logic.
                {
                    NoisePeriod = (byte)(Note + Pitch);
                    channelPlayer.ChangeNoise(totalTime, NoisePeriod);
                }
                else
                {
                    NoisePeriod = (byte)(Pitch & 0x7f);
                    channelPlayer.ChangeNoise(totalTime, NoisePeriod);
                }
            }
            else if (wasUsingNoise && !Noise)
            {
                channelPlayer.ChangeNoise(totalTime, -1);
            }
        }
    }

    internal HippelCosoSong(SongInfo songInfo, Instrument[] instruments, Timbre[] timbres, Division[] divisions, Pattern[] patterns)
    {
        voiceCount = divisions.Length == 0 ? 0 : divisions[0].Channels.Length;
        this.songInfo = songInfo;
        this.instruments = instruments;
        this.timbres = timbres;
        this.divisions = divisions;
        this.patterns = patterns;
        channels = new Channel[voiceCount];
        channelPlayers = new ChannelPlayer[voiceCount];
        songSpeed = songInfo.InitialSpeed;
        channelPcmData = new short[voiceCount][];
        mixedData = new int[BufferSize];
        mixedDataCounts = new byte[BufferSize];
        pcmData = new short[BufferSize];

        for (int i = 0; i < voiceCount; ++i)
        {
            channelPcmData[i] = new short[BufferSize];
            InitializeChannel(i);
        }

        PreBuffer();
    }

    /// <summary>
    /// Initialize channel and channel player for the given voice index.
    /// </summary>
    /// <param name="voiceIndex"></param>
    private protected abstract void InitializeChannel(int voiceIndex);

    public virtual bool Paused
    {
        get => paused;
        set
        {
            if (paused == value)
                return;

            paused = value;
        }
    }

    public virtual bool EndOfStream { get; private protected set; }

    private protected virtual void PreBuffer()
    {
        bool wasPlaying = playing;
        playing = true;
        Update(BufferTime, null);
        playing = wasPlaying;
    }

    public virtual void Play()
    {
        Stop();

        currentVoice = 0;
        playing = true;
    }
    
    public virtual void Stop(bool reset = true)
    {
        if (!playing)
            return;

        playing = false;
        Paused = false;
        currentVoice = 0;

        if (reset)
            Reset();     
    }

    public virtual void Reset()
    {
        elapsedTime = 0.0;
        totalTime = 0.0;
        EndOfStream = false;
        prebuffered = false;

        foreach (var channel in channels)
        {
            channel.Reset();
        }

        foreach (var channelPlayer in channelPlayers)
        {
            channelPlayer.Reset();
        }

        PreBuffer();
    }

    public virtual void Update(double elapsed, IMusicPlayer? musicPlayer)
    {
        if (!playing || paused)
            return;

        if (prebuffered)
        {
            musicPlayer?.SampleData(pcmData, EndOfStream);
            prebuffered = musicPlayer == null;
        }

        double time = totalTime;
        elapsedTime += elapsed;
        totalTime += elapsed;

        int ticks = (int)Math.Floor(elapsedTime / TickTime);
        elapsedTime -= ticks * TickTime;

        while (ticks-- > 0)
        {
            bool updatePatterns = false;

            if (!channels[0].HasDivision)
                songSpeedCounter = 1;

            if (--songSpeedCounter == 0)
            {
                updatePatterns = true;
                songSpeedCounter = songSpeed;
            }

            currentVoice = 0;

            foreach (var channel in channels)
            {
                if (resetDivisions)
                    channel.NextDivision(false);

                channel.Update(time, updatePatterns);

                currentVoice++;
            }

            resetDivisions = false;

            EndOfStream = channels.Any(channel => !channel.IsPlaying);

            if (EndOfStream)
            {
                endOfStreamTime = time;
                break;
            }

            time += TickTime;
        }

        if (totalTime - lastSampleTime >= BufferTime)
        {
            if (EndOfStream && endOfStreamTime == lastSampleTime)
            {
                musicPlayer?.SampleData([], true);
                return;
            }

            Array.Clear(mixedData);
            Array.Clear(mixedDataCounts);

            // Enable state change of voice based on buffer index.
            var enableSwitches = new Dictionary<int, bool>[voiceCount];

            for (int i = 0; i < voiceCount; i++)
            {
                var enableStateChanges = enableSwitches[i] = [];

                void EnableChannel(int index, bool enable)
                {
                    enableStateChanges.Add(index, enable);
                }

                channelPlayers[i].SampleData(channelPcmData[i], lastSampleTime,
                    EnableChannel, i == 0);

                bool enabled = true;

                for (int b = 0; b < BufferSize; b++)
                {
                    if (enableStateChanges.TryGetValue(b, out var enable))
                        enabled = enable;

                    if (enabled)
                    {
                        ++mixedDataCounts[b];
                        mixedData[b] += channelPcmData[i][b];
                    }
                }
            }

            for (int b = 0; b < BufferSize; b++)
            {
                if (mixedDataCounts[b] == 0)
                    pcmData[b] = 0;
                else
                    pcmData[b] = (short)(mixedData[b] / mixedDataCounts[b]);
            }

            if (EndOfStream)
            {
                double duration = endOfStreamTime - lastSampleTime;
                int sampleCount = (int)(duration * SampleRate / 1000.0);

                if (sampleCount < pcmData.Length)
                {
                    musicPlayer?.SampleData(pcmData[..sampleCount], true);
                }
                else
                {
                    musicPlayer?.SampleData(pcmData, true);
                }

                elapsedTime = 0.0;
                totalTime = 0.0;
                lastSampleTime = 0.0;
                EndOfStream = false;

                foreach (var channel in channels)
                {
                    channel.Reset();
                }                
            }
            else
            {
                musicPlayer?.SampleData(pcmData, false);
                lastSampleTime += BufferTime;
            }

            prebuffered = musicPlayer == null;
        }
    }

    public virtual void SetPitch(int pitch)
    {
        channels[currentVoice].Pitch = pitch;
    }

    public virtual void SetNote(int note)
    {
        channels[currentVoice].Note = note;
    }

    public virtual void SetVolume(int volume)
    {
        channels[currentVoice].Volume = volume;
    }

    public void ResetVolume() => SetVolume(MaxVolume);

    public void ResetTimbre()
    {
        channels[currentVoice].ResetTimbre();
    }

    public void SetTimbre(int index, int? customInstrument)
    {
        channels[currentVoice].SetTimbre(index < timbres.Length ? index : 0, customInstrument);
    }

    public void SetSample(int index)
    {
        channels[currentVoice].Sample = index;
    }

    public void NextDivision()
    {
        if (currentVoice != 0)
            return;

        channels[currentVoice].NextDivision(true); // TODO: Is this true right?
        resetDivisions = true;
    }

    public int GetTimbre() => channels[currentVoice].CurrentTimbre;

    public int GetTimbreAdjust() => channels[currentVoice].CurrentTimbreAdjust;

    public int GetPitch() => channels[currentVoice].Pitch;
}
