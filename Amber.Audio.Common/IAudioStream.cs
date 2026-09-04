namespace Amber.Audio;

public delegate void DataStreamEventHandler(short[] data, bool endOfStream);

public interface IAudioStream
{
    /// <summary>
    /// Raised whenever new audio data is available to stream.
    /// </summary>
    event DataStreamEventHandler? DataStreamed;

    /// <summary>
    /// Resets the stream back to its start.
    /// </summary>
    void Reset();
}
