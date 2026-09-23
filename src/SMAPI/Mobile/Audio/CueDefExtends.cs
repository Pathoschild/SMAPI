using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Microsoft.Xna.Framework.Audio;
using StardewValley;
using StardewValley.Audio;
using static StardewValley.Minigames.MineCart;

namespace StardewModdingAPI.Mobile.Audio;

internal static class CueDefExtends
{
    internal static void SetSoundEx1(this CueDefinition cue, SoundEffect[] sound_effects,
         int category_id, bool loop = false, bool use_reverb = false)
    {
        var sounds = cue.sounds;

        foreach (XactSoundBankSound xact_sound in sounds)
        {
            if (xact_sound.soundClips == null)
            {
                xact_sound.soundBank.GetSoundEffect(xact_sound.waveBankIndex, xact_sound.trackIndex).RemoveDependency();
                continue;
            }

            foreach (var soundClip in xact_sound.soundClips)
            {
                foreach (ClipEvent clipEvent in soundClip.clipEvents)
                {
                    var playWaveEvent = clipEvent as PlayWaveEvent;
                    if (playWaveEvent is null)
                        continue;

                    Console.WriteLine("play event var type: " + playWaveEvent.variationType);
                    foreach (PlayWaveVariant variant in playWaveEvent.GetVariants())
                    {
                        //debug
                        var effect = variant.GetSoundEffectEx1();
                        variant.GetSoundEffect().RemoveDependency();
                    }
                }
            }
        }
        sounds.Clear();
        sounds.Add(new XactSoundBankSound(sound_effects, category_id, loop, use_reverb));
        for (int i = 0; i < sound_effects.Length; i++)
        {
            sound_effects[i].AddDependency();
        }
        foreach (XactSoundBankSound sound in sounds)
        {
            if (sound.soundClips == null)
            {
                continue;
            }
            XactClip[] soundClips = sound.soundClips;
            for (int i = 0; i < soundClips.Length; i++)
            {
                ClipEvent[] clipEvents = soundClips[i].clipEvents;
                foreach (ClipEvent clip_event in clipEvents)
                {
                    _ = clip_event is PlayWaveEvent;
                }
            }
        }
    }
    static SoundEffect GetSoundEffectEx1(this PlayWaveVariant waveVariant)
    {
        if (waveVariant.overrideSoundEffect != null)
        {
            return waveVariant.overrideSoundEffect;
        }
        if (waveVariant.soundBank != null)
        {
            var soundBank = waveVariant.soundBank;
            return soundBank.GetSoundEffectEx1(waveVariant.waveBank, waveVariant.track);
        }
        return null;
    }

    public static SoundEffect GetSoundEffectEx1(this SoundBank soundBank, int waveBankIndex, int trackIndex)
    {
        var _waveBanks = (WaveBank[])AccessTools.Field(
            typeof(SoundBank), "_waveBanks").GetValue(soundBank);

        WaveBank waveBank = _waveBanks[waveBankIndex];
        var _sounds = (SoundEffect[])AccessTools.Field(typeof(WaveBank), "_sounds").GetValue(waveBank);
        //for (int i = 0; i < _sounds.Length; i++)
        //{
        //    Console.WriteLine("sound i: " + i + ", effect: " + _sounds[i]);
        //}

        var trackEffect = _sounds[trackIndex];
        var fileName = waveBank.ReadField<string>("_waveBankFileName");
        var bankName = waveBank.ReadField<string>("_bankName");
        Console.WriteLine("bank name: " + bankName);
        Console.WriteLine("file name: " + fileName);

        return waveBank.GetSoundEffect(trackIndex);
    }
    static T ReadField<T>(this object obj, string fieldName)
    {
        return (T)AccessTools.Field(obj.GetType(), fieldName).GetValue(obj);
    }
}
