using Amber.Audio;

namespace Amberstar.Frontend;

/// <summary>
/// Audio output which plays nothing. Used when running headless.
/// </summary>
internal sealed class NullAudioOutput : IAudioOutput
{
	public bool Available => false;
	public bool Enabled { get; set; } = false;
	public bool Streaming => false;
	public float Volume { get; set; } = 1.0f;

	public void Reset()
	{
	}

	public void Start()
	{
	}

	public void Stop()
	{
	}

	public void StreamData(IAudioStream audioStream, int channels = 1, int sampleRate = 44100, ChannelDataFormat dataFormat = ChannelDataFormat.Unsigned8Bit)
	{
	}
}
