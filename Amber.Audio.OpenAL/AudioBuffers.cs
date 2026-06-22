using Amber.Audio.Common;
using Silk.NET.OpenAL;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Amber.Audio.OpenAL;

internal class AudioBuffers(AL al, uint source, int channels, int sampleRate, ChannelDataFormat dataFormat, IAudioStream audioStream) : IDisposable
{
    static AudioBuffers CurrentBuffers { get; set; } = null;
    readonly Queue<AudioBuffer> queuedBuffers = [];
    readonly AL al = al;
    readonly uint source = source;
    readonly int channels = channels;
    readonly int sampleRate = sampleRate;
    readonly ChannelDataFormat dataFormat = dataFormat;
    readonly IAudioStream audioStream = audioStream;
    int bufferPosition = 0;
    Task playbackTask;
    readonly Mutex positionMutex = new();

    public void Dispose()
    {
        Stop();
    }

    private void DataStreamed(short[] pcmData, bool endOfStream)
    {
        lock (queuedBuffers)
        {
            var nextBuffer = new AudioBuffer(al, channels, sampleRate, dataFormat, bufferPosition);

            nextBuffer.Stream(pcmData);

            if (endOfStream)
            {
                bufferPosition = 0;
            }
            else
            {
                bufferPosition += nextBuffer.Size;
            }

            queuedBuffers.Enqueue(nextBuffer);
            al.SourceQueueBuffers(source, [nextBuffer.Index]);

            al.GetSourceProperty(source, GetSourceInteger.SourceState, out var state);

            if (state != (int)SourceState.Playing)
                al.SourcePlay(source);
        }
    }

    public void Play(CancellationToken cancellationToken)
    {
        CurrentBuffers?.Stop();

        if (CurrentBuffers != this)
        {
            CurrentBuffers = this;

            audioStream.DataStreamed -= DataStreamed;
            audioStream.Reset();

            lock (positionMutex)
            {
                bufferPosition = 0;
            }
        }

        playbackTask = PlaybackLoopAsync(cancellationToken);
    }

    public void Stop()
    {
        lock (positionMutex)
        {
            if (playbackTask != null && !playbackTask.IsCompleted)
                playbackTask.Wait();
            else
            {
                al.SourceStop(source);
                al.SetSourceProperty(source, SourceInteger.Buffer, 0);
            }
            CurrentBuffers = null;
        }
    }

    private static async Task WaitAsync(int delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            // ignore
        }
    }

    async Task PlaybackLoopAsync(CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
        {
            void ReleaseBuffers(int count)
            {
                for (int i = 0; i < Math.Min(count, queuedBuffers.Count); ++i)
                {
                    var buffer = queuedBuffers.Dequeue();
                    al.SourceUnqueueBuffers(source, [buffer.Index]);
                    buffer.Dispose();
                }
            }

            if (cancellationToken.IsCancellationRequested)
                return;

            audioStream.DataStreamed += DataStreamed;

            if (cancellationToken.IsCancellationRequested)
                return;

            while (!cancellationToken.IsCancellationRequested)
            {
                lock (queuedBuffers)
                {
                    if (queuedBuffers.Count != 0)
                        break;
                }

                await WaitAsync(10, cancellationToken); // Wait for data
            }

            if (cancellationToken.IsCancellationRequested)
                return;

            // Start playing the source
            al.SourcePlay(source);

            while (!cancellationToken.IsCancellationRequested)
            {
                // Wait for a buffer to finish playing
                int buffersProcessed;
                do
                {
                    await WaitAsync(10, cancellationToken);
                    al.GetSourceProperty(source, GetSourceInteger.BuffersProcessed, out buffersProcessed);
                } while (buffersProcessed == 0 && !cancellationToken.IsCancellationRequested);

                if (!cancellationToken.IsCancellationRequested)
                {
                    lock (queuedBuffers)
                    {
                        ReleaseBuffers(buffersProcessed);
                    }
                }
            }

            // Stop playing the source
            al.SourceStop(source);

            lock (queuedBuffers)
            {
                while (queuedBuffers.Count != 0)
                {
                    var buffer = queuedBuffers.Dequeue();
                    al.SourceUnqueueBuffers(source, [buffer.Index]);
                    buffer.Dispose();
                }
            }

            al.SetSourceProperty(source, SourceInteger.Buffer, 0);
        }, cancellationToken);
    }
}
