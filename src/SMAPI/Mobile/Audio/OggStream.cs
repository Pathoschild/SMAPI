using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Audio;
using NVorbis;

namespace StardewModdingAPI.Mobile.Facade;

public class OggStream : IDisposable
{
    private readonly string oggFileName;

    protected DynamicSoundEffectInstance _instance;

    private const int DefaultBufferSize = 44100;

    private const int BytesPerSample = 2;

    private readonly float[] readSampleBuffer;

    private readonly short[] castBuffer;

    private readonly byte[] xnaBuffer;

    protected int bufferSize = 44100;

    private float volume;

    internal VorbisReader Reader { get; private set; }

    internal bool Ready { get; private set; }

    internal bool Preparing { get; private set; }

    public Action FinishedAction { get; private set; }

    public float Volume
    {
        get
        {
            return this.volume;
        }
        set
        {
            this._instance.Volume = value;
        }
    }

    public uint LoopCount
    {
        get
        {
            return this._instance.LoopCount;
        }
        set
        {
            this._instance.LoopCount = value;
        }
    }

    public OggStream(string filename, Action finishedAction = null, int buffer_size = 44100)
    {
        this.oggFileName = filename;
        this.FinishedAction = finishedAction;
        this.bufferSize = buffer_size;
        this.readSampleBuffer = new float[this.bufferSize];
        this.castBuffer = new short[this.bufferSize];
        this.xnaBuffer = new byte[this.bufferSize * 2];
    }

    public void Prepare()
    {
        if (!this.Preparing && !this.Ready)
        {
            this.Preparing = true;
            this.Open(precache: true);
        }
    }

    public void Play()
    {
        if (this._instance != null)
        {
            if (this._instance.PendingBufferCount == 0)
            {
                this.SubmitBuffer();
            }
            this._instance.Play();
        }
    }

    public void Pause()
    {
        this._instance.Pause();
    }

    public void Resume()
    {
        this._instance.Resume();
    }

    public void Stop()
    {
        this.SeekToPosition(new TimeSpan(0L));
        this._instance.Stop();
    }

    public void SeekToPosition(TimeSpan pos)
    {
        this.Reader.DecodedTime = pos;
    }

    public TimeSpan GetPosition()
    {
        if (this.Reader == null)
        {
            return TimeSpan.Zero;
        }
        return this.Reader.DecodedTime;
    }

    public TimeSpan GetLength()
    {
        return this.Reader.TotalTime;
    }

    public void Dispose()
    {
        if (this._instance != null)
        {
            this._instance.Dispose();
            this._instance = null;
        }
    }

    internal void Open(bool precache = false)
    {
        this.Reader = new VorbisReader(this.oggFileName);
        this._instance = new OggStreamSoundEffectInstance(this.Reader.SampleRate, (this.Reader.Channels == 1) ? AudioChannels.Mono : AudioChannels.Stereo);
        this._instance.BufferNeeded += delegate
        {
            this.SubmitBuffer();
        };
        if (precache)
        {
            this.SubmitBuffer();
        }
        this.Ready = true;
        this.Preparing = false;
    }

    public virtual void SubmitBuffer()
    {
        if (this.Reader.DecodedPosition >= this.Reader.TotalSamples)
        {
            if (this.LoopCount == 0)
            {
                if (this._instance.PendingBufferCount == 0)
                {
                    if (this.FinishedAction != null)
                    {
                        this.FinishedAction();
                    }
                    this._instance.FinishedQueueing();
                }
                return;
            }
            this.Reader.DecodedPosition = 0L;
        }
        int read_samples = this.Reader.ReadSamples(this.readSampleBuffer, 0, this.bufferSize);
        CastBuffer(this.readSampleBuffer, this.castBuffer, read_samples);
        Buffer.BlockCopy(this.castBuffer, 0, this.xnaBuffer, 0, read_samples * 2);
        this._instance.SubmitBuffer(this.xnaBuffer, 0, read_samples * 2);
    }

    public static void CastBuffer(float[] inBuffer, short[] outBuffer, int length)
    {
        for (int i = 0; i < length; i++)
        {
            int temp = (int)(32767f * inBuffer[i]);
            if (temp > 32767)
            {
                temp = 32767;
            }
            else if (temp < -32768)
            {
                temp = -32768;
            }
            outBuffer[i] = (short)temp;
        }
    }

    public DynamicSoundEffectInstance GetSoundEffectInstance()
    {
        return this._instance;
    }

    internal void Close()
    {
        if (this.Reader != null)
        {
            this.Reader.Dispose();
            this.Reader = null;
        }
        if (this._instance != null)
        {
            this._instance.Dispose();
            this._instance = null;
        }
        this.Ready = false;
    }
}

