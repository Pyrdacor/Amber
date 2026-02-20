namespace Amberstar.GameData.Legacy;

internal class HippelCosoSong : ISong
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

        public record Command(CommandType Type, params int[] Params);

        private int currentCommandIndex = 0;
        private int tickCounter = 1;

        // Set pitch, then $e0 .. $ef
        private readonly int[] CommandSizes = [1, 2, 1, 1, 3, 2, 1, 1, 2, 2, 1, 2, 2, 4, 2, 1, 1];

        public void Reset()
        {
            currentCommandIndex = 0;
            tickCounter = 1;
        }

        public void ProcessNextCommand(HippelCosoSong player)
        {
            if (--tickCounter > 0)
                return;

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
                        // Note: The param contains the byte offset instead of the command offset.
                        int byteOffset = command.Params[0];
                        currentCommandIndex = 0;

                        while (byteOffset != 0)
                        {
                            var commandType = Commands[currentCommandIndex++].Type;
                            int size = CommandSizes[(int)commandType];
                            byteOffset -= size;
                        }
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
                        player.channels[player.currentVoice].SetTimbre(command.Params[0]);
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

        public record Command(CommandType Type, params int[] Params);

        private readonly int[] CommandSizes = [1, 1, 2, 2];

        private int currentCommandIndex = 0;
        private int tickCounter = 1;
        private int delayCounter = 0;

        public void Reset()
        {
            currentCommandIndex = 0;
            tickCounter = 1;
            //delayCounter = 0;
        }

        public void ProcessNextCommand(HippelCosoSong player)
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
                        // Note: The param contains the byte offset instead of the command offset.
                        int byteOffset = command.Params[0];
                        currentCommandIndex = 0;

                        while (byteOffset != 0)
                        {
                            int currentSize = CommandSizes[(int)Commands[currentCommandIndex++].Type];
                            byteOffset -= currentSize;
                        }
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
        private int speed = 4;

        public void Reset(int initialSpeed)
        {
            currentCommandIndex = 0;
            tickCounter = 0;
            speed = initialSpeed;
        }

        public void ResetSpeed(int speed)
        {
            this.speed = speed;
        }

        public void ProcessNextCommand(HippelCosoSong player)
        {
            if (--tickCounter > 0)
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
                        player.SetNote(command.Params[0]);
                        if (command.Params.Length > 1 && command.Params[1] != -1)
                        {
                            // Set timbre
                            player.SetTimbre(player.GetTimbre() + command.Params[1]);
                        }
                        if (player.channels[player.currentVoice].AllowInstrumentOverride &&
                            command.Params.Length > 2 && command.Params[2] != -1)
                        {
                            // Set instrument
                            player.SetInstrument(command.Params[2]);
                        }
                        player.channels[player.currentVoice].CurrentPortandoDelta = 0;
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

    internal class ChannelPlayer
    {
        private record NoteInfo(double Time, int Note, int Volume);
        private record NoiseInfo(double Time, int Period);

        private readonly Queue<NoteInfo> notePeriods = [];
        private readonly Queue<NoiseInfo> noisePeriods = [];

        private int volume = 64;
        private int notePeriod = -1;
        private int noisePeriod = -1;
        private int playedVolume = 64;
        private int playedNotePeriod = 0;
        private bool useTone = true;
        private bool useNoise = false;
        private double noteTime = 0.0;

        public void Reset()
        {
            volume = 64;
            notePeriod = -1;
            noisePeriod = 1;
            playedVolume = 64;
            playedNotePeriod = 0;
            useTone = true;
            useNoise = false;
            noteTime = 0.0;
        }

        // This happens every tick as long as the channel is active.
        public void PlayNote(double time, int period, int volume)
        {
            //if (notePeriod != -1 && period == notePeriod && volume == this.volume)
            //    return;

            this.notePeriod = period;
            this.volume = volume;
            notePeriods.Enqueue(new(time, period, volume));
        }

        public void ChangeNoise(double time, int period)
        {
            if (period <= 0)
            {
                if (this.noisePeriod == -1)
                    return;

                this.noisePeriod = -1;
                noisePeriods.Enqueue(new(time, -1));
                return;
            }

            period = ~(byte)period;
            period &= 0x1f;

            if (this.noisePeriod == period)
                return;

            this.noisePeriod = period;
            noisePeriods.Enqueue(new(time, period));
        }

        public void SampleData(sbyte[] buffer, double time, Action<int> noisePeriodChanger,
            Func<byte> nextNoiseTick, Action<int, bool> enableChannel)
        {
            const double frequencyFactor = 2_000_000.0 / 16.0;
            const double timePerSample = 1000.0 / SampleRate;
            var noteFrequency = frequencyFactor / playedNotePeriod;
            var noteVolume = playedVolume / 64.0;
            double noteDuration = 1000.0 / noteFrequency;
            bool wasEnabled = useTone || useNoise;

            for (int i = 0; i < buffer.Length; i++)
            {
                if (notePeriods.Count != 0 && notePeriods.Peek().Time <= time)
                {
                    var noteInfo = notePeriods.Dequeue();
                    playedNotePeriod = noteInfo.Note;
                    playedVolume = noteInfo.Volume;

                    noteFrequency = frequencyFactor / playedNotePeriod;
                    noteVolume = playedVolume / 64.0;
                    useTone = playedNotePeriod > 0;
                    noteTime = time;
                    noteDuration = 1000.0 / noteFrequency;
                }

                if (noisePeriods.Count != 0 && noisePeriods.Peek().Time <= time)
                {
                    var noiseInfo = noisePeriods.Dequeue();
                    noisePeriodChanger(noiseInfo.Period);
                    useNoise = noisePeriod != -1;
                }

                bool isEnabled = useTone || useNoise;

                if (wasEnabled != isEnabled)
                    enableChannel(i, isEnabled);

                int div = useTone ? 1 : 0;
                div += useNoise ? 1 : 0;

                if (div == 0)
                {
                    buffer[i] = 0;
                }
                else
                {
                    double currentNoteTime = (time - noteTime) % noteDuration;
                    double tone = useTone ? (currentNoteTime < noteDuration / 2 ? 1.0 : -1.0) : 0.0; // rectangle wave
                    /*double tone = useTone ? Math.Sin(2 * Math.PI * noteFrequency * (0.001 * time)) : 0.0;
                    if (useTone)
                    {
                        if (tone > 0.0)
                            tone = 1.0;
                        else
                            tone = -1.0;
                    }*/

                    double noise = useNoise ? (nextNoiseTick() != 0 ? 1.0 : -1.0) : 0.0;

                    double sample = ((tone + noise) / div) * noteVolume;

                    buffer[i] = (sbyte)(sample * 127); // Convert to signed byte
                }

                time += timePerSample;
            }
        }
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
    const int TickTime = 120; // ms
    const int SampleRate = 44100; // Hz
    const int BufferSize = SampleRate / 4; // 0.25 second of audio
    const double BufferTime = BufferSize * 1000.0 / SampleRate; // in ms
    double elapsedTime = 0.0;
    double totalTime = 0.0;
    double lastSampleTime = 0.0;
    bool paused = false;
    bool playing = false;
    readonly int voiceCount = 0;
    int currentVoice = 0;
    readonly Instrument[] instruments;
    readonly Timbre[] timbres;
    readonly Division[] divisions;    
    readonly Pattern[] patterns;
    readonly Channel[] channels;
    readonly SongInfo songInfo;
    readonly ChannelPlayer[] channelPlayers;
    readonly sbyte[][] channelPcmData;
    readonly short[] mixedData;
    readonly byte[] mixedDataCounts;
    readonly byte[] pcmData;
    readonly byte[] noiseData;
    readonly YmNoiseGenerator noiseGenerator = new(1);
    bool prebuffered = false;
    double endOfStreamTime = 0.0;
    bool resetDivisions = false;

    private class Channel(HippelCosoSong player, ChannelPlayer channelPlayer, int channelIndex)
    {
        private int currentDivisionIndex = -1;
        private int currentInstrumentIndex = -1;
        private int currentTimbreIndex = -1;
        private Division.Channel? currentDivision;
        private Pattern? currentPattern;        
        private Instrument? currentInstrument;
        private Timbre? currentTimbre;

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
        public bool AllowInstrumentOverride { get; private set; } = true;
        public int CurrentInstrument
        {
            get => currentInstrumentIndex;
            set
            {
                currentInstrumentIndex = value;
                currentInstrument = player.instruments[value];
                currentInstrument!.Reset();
                ResetTimbre();

                // TODO: A new instrument should reset sustain counter, instead of timbre change...
            }
        }
        public int CurrentTimbre
        {
            get => currentTimbreIndex;
            set
            {
                currentTimbreIndex = value;
                currentTimbre = player.timbres[value];
                currentTimbre.VolumeEnvelop.Reset();
            }
        }
        public int CurrentTimbreAdjust => currentDivision?.TimbreAdjust ?? 0;

        public void Reset()
        {
            Volume = 64;
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

        public void ResetTimbre()
        {
            currentTimbre!.VolumeEnvelop.Reset();
        }

        public void SetTimbre(int index)
        {
            if (CurrentTimbre == index)
                return;

            CurrentTimbre = index;

            if (currentTimbre!.Instrument == 0x80)
            {
                AllowInstrumentOverride = false;
            }
            else
            {
                CurrentInstrument = currentTimbre.Instrument;
                AllowInstrumentOverride = true;
            }

            CurrentVibratoDelay = currentTimbre.Vibrato.Delay;
            CurrentVibratoSlope = currentTimbre.Vibrato.Slope;
            CurrentVibratoDepth = currentTimbre.Vibrato.Depth;
            CurrentVibratoDirection = -1;
        }

        public void NextDivision(bool processFirstPattern)
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

        private void InitDivision()
        {
            CurrentTimbre = currentDivision!.TimbreIndex + currentDivision.TimbreAdjust;

            if (currentTimbre!.Instrument == 0x80)
            {
                AllowInstrumentOverride = false;
            }
            else
            {
                CurrentInstrument = currentTimbre.Instrument;
                AllowInstrumentOverride = true;
            }

            int speed = currentDivision!.SongSpeed == -1
                ? 4//player.songInfo.InitialSpeed
                : currentDivision.SongSpeed;

            currentPattern = player.patterns[currentDivision!.PatternIndex];
            currentPattern!.Reset(speed);

            CurrentVibratoDelay = currentTimbre.Vibrato.Delay;
            CurrentVibratoSlope = currentTimbre.Vibrato.Slope;
            CurrentVibratoDepth = currentTimbre.Vibrato.Depth;
            CurrentVibratoDirection = -1;

            bool isChannelC = player.channels[2] == this;

            Noise = false;
            Tone = isChannelC; // Channel C is active, rest not.
            Volume = isChannelC ? 16 : 0;

            // Channel C has the period reset to 0x400.
            // We can only set the note where 23 is almost 0x400.
            if (isChannelC)
            {
                Note = 0;
                Pitch = 23;
            }
        }

        public void Update(double totalTime)
        {
            if (currentDivision == null) // start
            {
                IsPlaying = true;
                currentDivisionIndex = player.songInfo.StartDivision;
                currentDivision = player.divisions[currentDivisionIndex].Channels[channelIndex];
                InitDivision();
            }
            else if (!IsPlaying)
            {
                return;
            }

            bool wasUsingNoise = Noise;
            bool wasUsingTone = Tone;

            currentPattern!.ProcessNextCommand(player);
            currentInstrument!.ProcessNextCommand(player);
            currentTimbre!.VolumeEnvelop.ProcessNextCommand(player);

            int note = Pitch;

            if ((note & 0x80) == 0)
                note += Note + currentDivision.Transpose;
            
            note &= 0x7f;

            int volume = Math.Max(0, Volume - currentDivision.VolumeReduction);
            int period = NotePeriods[note];

            // Vibrato
            if (CurrentVibratoDelay == 0)
            {
                // It looks like this happen only every two calls
                CurrentVibratoDepth += CurrentVibratoDirection * CurrentVibratoSlope;

                if (CurrentVibratoDepth < 0 || CurrentVibratoDepth > 2 * currentTimbre.Vibrato.Depth)
                    CurrentVibratoDirection = -CurrentVibratoDirection;

                CurrentVibratoDepth = Math.Clamp(CurrentVibratoDepth, 0, 2 * currentTimbre.Vibrato.Depth);

                int diff = CurrentVibratoDepth - currentTimbre.Vibrato.Depth;

                period += (period * diff) / 1024;
            }
            else
            {
                --CurrentVibratoDelay;
            }

            // Portando
            if (Portando)
            {
                CurrentPortandoDelta += PortandoSlope;

                period *= (1 - (CurrentPortandoDelta * period) / 1024);
            }

            if (volume != 64 && volume > 0)
            {
                byte[] volumeTable = [0, 1, 2, 3, 4, 6, 8, 10, 13, 16, 20, 24, 30, 38, 48, 64];

                volume = volumeTable[volume & 0xf];
            }

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
                    NoisePeriod = period;
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
        channelPcmData = new sbyte[voiceCount][];
        mixedData = new short[BufferSize];
        mixedDataCounts = new byte[BufferSize];
        pcmData = new byte[BufferSize];
        noiseData = new byte[BufferSize];

        for (int i = 0; i < voiceCount; ++i)
        {
            channelPcmData[i] = new sbyte[BufferSize]; // 0.25 second of audio
            var channelPlayer = channelPlayers[i] = new();
            var channel = channels[i] = new Channel(this, channelPlayer, i);
            channel.Reset();
            channelPlayer.Reset();
        }

        PreBuffer();
    }

    public bool Paused
    {
        get => paused;
        set
        {
            if (paused == value)
                return;

            paused = value;
        }
    }

    public bool EndOfStream { get; private set; }

    private void PreBuffer()
    {
        bool wasPlaying = playing;
        playing = true;
        Update(BufferTime, null);
        playing = wasPlaying;
    }

    public void Play()
    {
        Stop();

        currentVoice = 0;
        playing = true;
    }
    
    public void Stop(bool reset = true)
    {
        if (!playing)
            return;

        playing = false;
        Paused = false;
        currentVoice = 0;

        if (reset)
            Reset();     
    }

    public void Reset()
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

    public void Update(double elapsed, IMusicPlayer? musicPlayer)
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
            currentVoice = 0;

            foreach (var channel in channels)
            {
                if (resetDivisions)
                    channel.NextDivision(false);

                channel.Update(time);
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
            Array.Clear(noiseData);

            int noiseTickIndex = 0;

            byte GetNextNoiseTick()
            {
                if (noiseTickIndex < noiseData.Length)
                    return noiseData[noiseTickIndex++] = noiseGenerator.Tick();

                return noiseData[noiseTickIndex++ % noiseData.Length];
            }

            void ChangeNoisePeriod(int period)
            {
                noiseGenerator.Period = 0x280 - period * 0x10;
                Console.WriteLine("Change noise period to " + period);
            }

            var enableSwitches = new Dictionary<int, bool>[voiceCount];

            for (int i = 0; i < voiceCount; i++)
            {
                var enableStateChanges = enableSwitches[i] = [];

                void EnableChannel(int index, bool enable)
                {
                    enableStateChanges.Add(index, enable);
                }

                channelPlayers[i].SampleData(channelPcmData[i], lastSampleTime,
                    ChangeNoisePeriod, GetNextNoiseTick, EnableChannel);

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
                    pcmData[b] = (byte)(128 + (mixedData[b] / mixedDataCounts[b]));
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

    public void SetPitch(int pitch)
    {
        channels[currentVoice].Pitch = pitch;
    }

    public void SetNote(int note)
    {
        channels[currentVoice].Note = note;
    }

    public void SetVolume(int volume)
    {
        channels[currentVoice].Volume = volume;
    }

    public void ResetVolume()
    {
        channels[currentVoice].Volume = 64; // TODO: default?
    }

    public void ResetTimbre()
    {
        channels[currentVoice].ResetTimbre();
    }

    public void SetTimbre(int index)
    {
        channels[currentVoice].SetTimbre(index < timbres.Length ? index : 0);
    }

    public void SetInstrument(int index)
    {
        channels[currentVoice].CurrentInstrument = index < instruments.Length ? index : 0;
    }

    public void SetSample(int index)
    {
        channels[currentVoice].Sample = index;
    }

    public void NextDivision()
    {
        if (currentVoice != 0)
            return;

        channels[currentVoice].NextDivision(true);
        resetDivisions = true;
    }

    public int GetTimbre() => channels[currentVoice].CurrentTimbre;

    public int GetTimbreAdjust() => channels[currentVoice].CurrentTimbreAdjust;

    public int GetPitch() => channels[currentVoice].Pitch;

    public class YmNoiseGenerator
    {
        private static readonly byte[] noiseData;

        static YmNoiseGenerator()
        {
            //noiseData = new byte[1024]
        }

        private uint lfsr = 0x1FFFF; // 17-bit LFSR initialized with all 1s
        private int period = 0; // Adjust based on YM noise period register (0-31)
        private byte currentOutput = 0;
        private int counter = 0;
        private int sampleTicksPerNoiseStep = 1;
        private readonly int sampleRate;

        public YmNoiseGenerator(int noisePeriod, int sampleRate = 44100)
        {
            Period = noisePeriod;
            this.sampleRate = sampleRate;

            counter = sampleTicksPerNoiseStep;
        }

        public int Period
        {
            get => period;
            set
            {
                //value = 1 + Math.Clamp(value, 0, 31) * 7;

                if (period == value)
                    return;

                period = value;

                // YM2149 noise frequency: 2 MHz / (16 * (period + 1))
                double noiseFreq = 2_000_000.0 / (16.0 * (period + 1));
                sampleTicksPerNoiseStep = Math.Max(1, (int)(sampleRate / noiseFreq));
            }
        }

        // Simulate YM noise clock tick (should be called at the correct tick rate)
        public byte Tick()
        {
            counter++;

            if (counter >= sampleTicksPerNoiseStep)
            {
                counter = 0;

                // XOR taps: bit 0 and bit 3 (based on real YM behavior)
                bool bit0 = (lfsr & 1) != 0;
                bool bit3 = (lfsr & (1 << 3)) != 0;
                bool feedback = bit0 ^ bit3;

                // Shift right and insert feedback at bit 16
                lfsr >>= 1;

                if (feedback)
                    lfsr |= (1u << 16);

                currentOutput = (byte)(lfsr & 1);
            }

            return currentOutput;
        }
    }
}
