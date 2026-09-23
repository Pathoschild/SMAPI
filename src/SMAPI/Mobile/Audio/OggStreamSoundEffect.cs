using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Xna.Framework.Audio;
using NVorbis;
using HarmonyLib;

namespace StardewModdingAPI.Mobile.Facade;


public class OggStreamSoundEffect : SoundEffect
{
    private const int BufferSize = 16384;

    private const int BytesPerSample = 2;

    private const int BufferSamples = 8192;

    private string OggFileName;

    private long TotalSamplesPerChannel;

    private int SampleRate;

    private AudioChannels Channels;

    public OggStreamSoundEffect(byte[] buffer, int sampleRate, AudioChannels channels) : base(buffer, sampleRate, channels)
    {
    }

    public OggStreamSoundEffect(byte[] buffer, int offset, int count, int sampleRate, AudioChannels channels, int loopStart, int loopLength) : base(buffer, offset, count, sampleRate, channels, loopStart, loopLength)
    {
    }

    public static OggStreamSoundEffect CreateOggStreamFromFileName(string oggFileName)
    {
        var ogg = AccessTools.CreateInstance<OggStreamSoundEffect>();
        using (VorbisReader reader = new VorbisReader(oggFileName))
        {
            ogg.OggFileName = oggFileName;
            ogg.TotalSamplesPerChannel = reader.TotalSamples;
            ogg.SampleRate = reader.SampleRate;
            ogg.Channels = ((reader.Channels != 2) ? AudioChannels.Mono : AudioChannels.Stereo);
        }

        return ogg;
    }

    public override SoundEffectInstance GetPooledInstance(bool forXAct)
    {
        DynamicSoundEffectInstance sound = new DynamicSoundEffectInstance(this.SampleRate, this.Channels);
        sound._isXAct = forXAct;
        ConcurrentQueue<byte[]> queue = new ConcurrentQueue<byte[]>();
        AutoResetEvent signal = new AutoResetEvent(initialState: false);
        AutoResetEvent stop = new AutoResetEvent(initialState: false);
        sound.BufferNeeded += delegate
        {
            byte[] result = null;
            try
            {
                do
                {
                    int num5 = 0;
                    while (queue.Count > 0)
                    {
                        if (queue.TryDequeue(out result))
                        {
                            sound.SubmitBuffer(result);
                            num5++;
                        }
                    }
                    signal.Set();
                    if (num5 > 0)
                    {
                        return;
                    }
                }
                while (!stop.WaitOne(0));
                sound.Stop();
            }
            catch (Exception)
            {
            }
        };

        Thread streamThread = new Thread((ThreadStart)delegate
        {
            // คำนวณขนาดของบัฟเฟอร์ที่เหลือจากตัวเลขคำนวณ
            int remainingBufferSize = (int)(this.TotalSamplesPerChannel * (long)this.Channels * 2 % 16384);

            int loopIterationCount = 0; // จำนวนการวนซ้ำสำหรับ loop เล่นเสียง
            float[] floatBuffer = new float[8192]; // บัฟเฟอร์สำหรับเก็บข้อมูลเสียง float
            short[] shortBuffer = new short[8192]; // บัฟเฟอร์สำหรับแปลง float เป็น short
            int rotatingBufferIndex = 0; // ตัวชี้สำหรับ buffer วนซ้ำ

            // สร้าง buffer หมุนวนสำหรับจัดเก็บข้อมูล byte
            byte[][] rotatingByteBuffers = new byte[4][]
            {
                new byte[16384], new byte[16384],
                new byte[16384], new byte[16384]
            };
            byte[] finalSmallBuffer = new byte[remainingBufferSize]; // บัฟเฟอร์สำหรับข้อมูลสุดท้ายที่ไม่เต็มขนาด

            // สร้าง VorbisReader สำหรับอ่านข้อมูล Ogg
            VorbisReader vorbisReader = new(this.OggFileName);

            //fixme
            //this works, fix bug:
            //System.ArgumentOutOfRangeException: Index was out of range.
            //Must be non - negative and less than the size of the collection. (Parameter 'index')
            //NVorbis.Ogg.StreamPageReader.GetPageRaw(Int32 pageIndex, Int64& pageGranulePos)
            long totalSamples = vorbisReader.TotalSamples;

            try
            {
                // เริ่มการอ่านจากตำแหน่งเริ่มต้น
                do
                {
                    vorbisReader.SeekTo(0); // รีเซ็ตตำแหน่งการอ่าน
                    while (!sound.IsDisposed)
                    {
                        while (queue.Count < 3 && vorbisReader.SamplePosition < totalSamples)
                        {
                            // เลือกบัฟเฟอร์ที่จะใช้
                            byte[] currentBuffer = finalSmallBuffer;
                            int samplesToRead = Math.Min(8192, (int)((totalSamples - vorbisReader.SamplePosition) * (long)this.Channels));

                            if (samplesToRead == 8192)
                            {
                                currentBuffer = rotatingByteBuffers[rotatingBufferIndex];
                                rotatingBufferIndex = (rotatingBufferIndex + 1) % 4; // หมุน index ไปยัง buffer ถัดไป
                            }

                            // อ่าน samples จาก VorbisReader
                            samplesToRead = vorbisReader.ReadSamples(floatBuffer, 0, samplesToRead);

                            // แปลงข้อมูลจาก float เป็น short และคัดลอกลงบัฟเฟอร์ byte
                            OggStream.CastBuffer(floatBuffer, shortBuffer, samplesToRead);
                            Buffer.BlockCopy(shortBuffer, 0, currentBuffer, 0, samplesToRead * 2);

                            // ใส่ข้อมูลที่อ่านได้ลงในคิว
                            queue.Enqueue(currentBuffer);

                            if (vorbisReader.SamplePosition >= totalSamples)
                            {
                                goto endReadingLoop;
                            }
                        }

                        // รอ 1 วินาที หากไม่มีข้อมูลเพิ่ม
                        signal.WaitOne(1000);
                        continue;

                    endReadingLoop:
                        break;
                    }
                }
                while (!sound.IsDisposed && (sound.LoopCount >= 255 || loopIterationCount++ < sound.LoopCount));
            }
            finally
            {
                // Dispose VorbisReader หลังจากเสร็จสิ้นการอ่าน
                vorbisReader.Dispose();
                stop.Set(); // แจ้งสัญญาณว่าการอ่านหยุดแล้ว
            }
        });

        streamThread.Priority = ThreadPriority.Highest;
        streamThread.Start();

        return sound;
    }
}
