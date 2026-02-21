namespace Amber.Audio;

public delegate void DataStreamEventHandler(short[] data, bool endOfStream);

public interface IAudioStream
{
    event DataStreamEventHandler? DataStreamed;

    void Reset();
}
