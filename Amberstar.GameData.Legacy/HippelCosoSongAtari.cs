namespace Amberstar.GameData.Legacy;

internal class HippelCosoSongAtari : HippelCosoSong
{
    private protected override int MaxVolume => 15;

    private protected override void InitializeChannel(int voiceIndex)
    {
        var channelPlayer = new ChannelPlayerAtari();
        channelPlayers[voiceIndex] = channelPlayer;

        var channel = new ChannelAtari(this, channelPlayer, voiceIndex);
        channels[voiceIndex] = channel;

        channel.SongSpeedChanged += speed => songSpeed = speed;
        channel.Reset();
        channelPlayer.Reset();
    }

    internal class ChannelPlayerAtari : ChannelPlayer
    {
        bool useTone = true;
        bool useNoise = false;        
        double tickAccumulator = 0.0;
        double blepPhase = 0.0;
        readonly VoiceHelper voiceHelper = new();

        public ChannelPlayerAtari()
            : base(ConvertNoisePeriod)
        {

        }

        static int ConvertNoisePeriod(int period)
        {
            period = ~(byte)period;
            period &= 0x1f;

            return period;
        }

        public override void Reset()
        {
            base.Reset();

            useTone = true;
            useNoise = false;
        }

        class VoiceHelper
        {
            double lpState = 0; // low pass
            int tonePeriod = 0;
            static bool noiseEnabled = false;
            static int noisePeriod;
            static int noiseCounter;
            static uint lfsr = 0x1FFFF;
            static int noiseBit;

            public static bool NoiseEnabled
            {
                get => noiseEnabled;
                set
                {
                    noiseEnabled = value;

                    if (noiseEnabled && noiseCounter <= 0)
                        noiseCounter = noisePeriod;
                }
            }

            public int TonePeriod
            {
                get => tonePeriod;
                set
                {
                    tonePeriod = Math.Max(1, value);
                }
            }

            public static int NoisePeriod
            {
                get => noisePeriod;
                set
                {
                    noisePeriod = Math.Clamp(value, 1, 31);
                    noiseBit = 0;

                    if (noiseCounter <= 0)
                        noiseCounter = noisePeriod;
                }
            }

            public int Volume { get; set; } = 64;

            public static void TickChip()
            {
                if (noiseEnabled)
                {
                    if (--noiseCounter <= 0)
                    {
                        noiseCounter += noisePeriod;

                        // 17-bit LFSR (x^17 + x^14 + 1) => taps bit0 xor bit3, shift right
                        uint newBit = (lfsr ^ (lfsr >> 3)) & 1u;
                        lfsr = (lfsr >> 1) | (newBit << 16);
                        noiseBit = (int)(lfsr & 1u);
                    }
                }
            }

            public static int GetNoiseBit() => noiseEnabled ? noiseBit : 1;

            public double LowPass(double s, double a)
            {
                lpState += a * (s - lpState);
                return lpState;
            }
        }

        public override void SampleData(short[] buffer, double time,
            Action<int, bool> enableChannel, bool firstChannel)
        {
            const double psgTicksPerSecond = 2_000_000.0 / 8.0;
            double psgTicksPerSample = psgTicksPerSecond / SampleRate;

            const double timePerSample = 1000.0 / SampleRate;

            bool wasEnabled = useTone || useNoise;
            bool noiseWasActive = useNoise;

            for (int i = 0; i < buffer.Length; i++)
            {
                while (notePeriods.Count != 0 && notePeriods.Peek().Time <= time)
                {
                    var noteInfo = notePeriods.Dequeue();

                    useTone = noteInfo.Period != -1;

                    if (useTone)
                    {
                        voiceHelper.TonePeriod = noteInfo.Period;
                        voiceHelper.Volume = noteInfo.Volume;
                    }
                }

                while (noisePeriods.Count != 0 && noisePeriods.Peek().Time <= time)
                {
                    var noiseInfo = noisePeriods.Dequeue();

                    useNoise = noiseInfo.Period != -1;

                    if (useNoise)
                        VoiceHelper.NoisePeriod = noiseInfo.Period;
                }

                if (useNoise != noiseWasActive)
                    VoiceHelper.NoiseEnabled = useNoise;

                bool isEnabled = useTone || useNoise;

                if (wasEnabled != isEnabled)
                    enableChannel(i, isEnabled);

                if (!isEnabled)
                {
                    buffer[i] = 0;
                }
                else
                {
                    if (firstChannel)
                    {
                        tickAccumulator += psgTicksPerSample;

                        while (tickAccumulator >= 1.0)
                        {
                            VoiceHelper.TickChip();
                            tickAccumulator -= 1.0;
                        }
                    }

                    double s = GetSampleBlep(SampleRate, voiceHelper.TonePeriod,
                        voiceHelper.Volume, VoiceHelper.GetNoiseBit());

                    // Low-pass
                    s = voiceHelper.LowPass(s, 0.15);

                    int v = (int)Math.Round(s * short.MaxValue);
                    buffer[i] = (short)Math.Clamp(v, short.MinValue, short.MaxValue);
                }

                time += timePerSample;
            }
        }

        static double PolyBlep(double t, double dt)
        {
            if (t < dt)
            {
                t /= dt;
                return t + t - t * t - 1.0;
            }

            if (t > 1.0 - dt)
            {
                t = (t - 1.0) / dt;
                return t * t + t + t + 1.0;
            }

            return 0.0;
        }

        static double PolyBlepSquare(ref double phase, double dt)
        {
            // phase: 0..1
            phase += dt;
            phase -= Math.Floor(phase);

            double s = (phase < 0.5) ? 1.0 : -1.0;

            // band-limit both edges
            s += PolyBlep(phase, dt);
            double t2 = phase + 0.5;
            if (t2 >= 1.0) t2 -= 1.0;
            s -= PolyBlep(t2, dt);

            return s;
        }

        public double GetSampleBlep(int sampleRate, int tonePeriod, int volume, int noiseBit)
        {
            double vol = volume / 64.0;

            // Tone dt (freq/sampleRate). YM tone freq = YMClock / (16 * period)
            // YMClock on ST = 2_000_000 Hz
            double dt = 0.0;

            if (useTone)
            {
                int p = Math.Max(1, tonePeriod);
                double freq = 2_000_000.0 / (16.0 * p);
                dt = freq / sampleRate;
                if (dt > 0.5) dt = 0.5; // safety, avoid nonsense at extreme highs
            }

            // Generate band-limited square (-1..+1)
            double tone = useTone ? PolyBlepSquare(ref blepPhase, dt) : 1.0;

            int toneBit = useTone ? (tone >= 0 ? 1 : 0) : 1;

            int outBit = toneBit & noiseBit;

            return (outBit == 0 ? -1.0 : 1.0) * vol;
        }
    }

    // TODO: REMOVE here if same for Amiga and Atari
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

    private class ChannelAtari(HippelCosoSongAtari player, ChannelPlayerAtari channelPlayer, int channelIndex)
        : Channel(player, channelPlayer, channelIndex)
    {
        private protected override int CalculateVolume()
        {
            int volume = base.CalculateVolume();

            if (volume > 0)
            {
                byte[] volumeTable = [0, 1, 2, 3, 4, 6, 8, 10, 13, 16, 20, 24, 30, 38, 48, 64];

                volume = volumeTable[volume & 0xf];
            }

            return volume;
        }
    }

    internal HippelCosoSongAtari(SongInfo songInfo, Instrument[] instruments, Timbre[] timbres, Division[] divisions, Pattern[] patterns)
        : base(songInfo, instruments, timbres, divisions, patterns)
    {
        
    }
}
