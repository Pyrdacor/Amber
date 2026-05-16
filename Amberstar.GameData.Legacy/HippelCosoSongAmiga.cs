namespace Amberstar.GameData.Legacy;

internal class HippelCosoSongAmiga : HippelCosoSong
{
    private protected override int MaxVolume => 64;

    private protected override void InitializeChannel(int voiceIndex)
    {
        var channelPlayer = new ChannelPlayerAmiga();
        channelPlayers[voiceIndex] = channelPlayer;

        var channel = new ChannelAmiga(this, channelPlayer, voiceIndex);
        channels[voiceIndex] = channel;

        channel.SongSpeedChanged += speed => songSpeed = speed;
        channel.Reset();
        channelPlayer.Reset();
    }

    internal class ChannelPlayerAmiga : ChannelPlayer
    {
        bool useTone = true;
        bool useNoise = false;

        public ChannelPlayerAmiga()
            : base(ConvertNoisePeriod)
        {

        }

        static int ConvertNoisePeriod(int period)
        {
            /*period = ~(byte)period;
            period &= 0x1f;*/

            return period;
        }

        public override void Reset()
        {
            base.Reset();

            useTone = true;
            useNoise = false;
        }

        public override void SampleData(short[] buffer, double time,
            Action<int, bool> enableChannel)
        {
            // TODO
            throw new NotImplementedException();
        }
    }

    private class ChannelAmiga(HippelCosoSongAmiga player, ChannelPlayerAmiga channelPlayer, int channelIndex)
        : Channel(player, channelPlayer, channelIndex)
    {
        private protected override int CalculateVolume()
        {
            return base.CalculateVolume();
        }
    }

    internal HippelCosoSongAmiga(SongInfo songInfo, Instrument[] instruments, Timbre[] timbres, Division[] divisions, Pattern[] patterns)
        : base(songInfo, instruments, timbres, divisions, patterns)
    {
        
    }
}
