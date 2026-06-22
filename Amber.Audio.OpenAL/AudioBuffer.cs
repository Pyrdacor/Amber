using Amber.Audio.Common;
using Silk.NET.OpenAL;
using System;

namespace Amber.Audio.OpenAL;

internal class AudioBuffer(AL al, int channels, int sampleRate, ChannelDataFormat dataFormat, int position) : IDisposable
{
    readonly uint bufferIndex = al.GenBuffer();
    readonly BufferFormat format = dataFormat == ChannelDataFormat.Unsigned8Bit
            ? (channels == 1 ? BufferFormat.Mono8 : BufferFormat.Stereo8)
            : (channels == 1 ? BufferFormat.Mono16 : BufferFormat.Stereo16);
    bool disposed = false;

    public uint Index => bufferIndex;
    public int Position { get; } = position;
    public int Size { get; private set; } = 0;

    public void Stream(short[] data)
    {
        if (disposed)
            throw new InvalidOperationException("Tried to fill a disposed audio buffer.");

        Size = data.Length;
        al.BufferData(bufferIndex, format, data, sampleRate);
    }

    public void Stream(byte[] data)
    {
        if (disposed)
            throw new InvalidOperationException("Tried to fill a disposed audio buffer.");

        Size = data.Length;
        al.BufferData(bufferIndex, format, data, sampleRate);
    }

    public void Dispose()
    {
        if (!disposed)
        {
            if (al.IsBuffer(bufferIndex))
            {
                al.DeleteBuffer(bufferIndex);
                var error = al.GetError();
                if (error != AudioError.NoError)
                {
                    Console.WriteLine($"OpenAL error while deleting buffer with index {bufferIndex}: " + error);
                }
            }
            Size = 0;
            disposed = true;
        }
    }
}
