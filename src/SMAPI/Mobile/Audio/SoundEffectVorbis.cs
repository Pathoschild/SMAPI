using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Microsoft.Xna.Framework.Audio;
using NVorbis;
using NVorbis.Contracts;

namespace StardewModdingAPI.Mobile.Facade;

public class SoundEffectVorbis : SoundEffect
{
    public SoundEffectVorbis(byte[] buffer, int sampleRate, AudioChannels channels) : base(buffer, sampleRate, channels)
    {
    }

    public SoundEffectVorbis(byte[] buffer, int offset, int count, int sampleRate, AudioChannels channels, int loopStart, int loopLength) : base(buffer, offset, count, sampleRate, channels, loopStart, loopLength)
    {
    }

    static readonly FieldInfo _duration_FI = AccessTools.Field(typeof(SoundEffect), "_duration");
    static readonly MethodInfo Initialize_MI = AccessTools.Method(typeof(SoundEffect), "Initialize");

    public static SoundEffectVorbis CreateFromFilePath(string soundFilePath)
    {
        Console.WriteLine("starting load sound vorbis: " + soundFilePath);

        var st = Stopwatch.StartNew();
        using FileStream stream = new FileStream(soundFilePath, FileMode.Open);
        var soundEffect = AccessTools.CreateInstance<SoundEffectVorbis>();
        Initialize_MI.Invoke(soundEffect, null);

        using (VorbisReader vorbis_reader = new VorbisReader(stream, closeOnDispose: true))
        {

            const int bytes_per_sample = 2;

            float[] float_buffer = new float[vorbis_reader.TotalSamples * vorbis_reader.Channels];
            short[] cast_buffer = new short[float_buffer.Length];
            byte[] xna_buffer = new byte[float_buffer.Length * bytes_per_sample];
            int read_samples = vorbis_reader.ReadSamples(float_buffer, 0, float_buffer.Length);
            OggStream.CastBuffer(float_buffer, cast_buffer, read_samples);
            Buffer.BlockCopy(cast_buffer, 0, xna_buffer, 0, read_samples * bytes_per_sample);
            _duration_FI.SetValue(soundEffect, vorbis_reader.TotalTime);
            var PlatformInitializePcm = AccessTools.Method(typeof(SoundEffect), "PlatformInitializePcm");
            PlatformInitializePcm.Invoke(soundEffect, [
                xna_buffer, 0, xna_buffer.Length, 16,
                    vorbis_reader.SampleRate,
                    (AudioChannels)vorbis_reader.Channels, 0,
                    (int)vorbis_reader.TotalSamples
            ]);
        }

        Console.WriteLine($"created SoundEffect: {soundEffect} in {st.Elapsed.TotalSeconds}s");
        return soundEffect;
    }
}
